using System;
using System.Collections.Generic;
using System.Linq;
using ParkingLotManagementSystem.Enums;
using ParkingLotManagementSystem.Exceptions;
using ParkingLotManagementSystem.Factories;
using ParkingLotManagementSystem.Models;
using ParkingLotManagementSystem.Strategies.Allocation;
using ParkingLotManagementSystem.Strategies.Fee;

namespace ParkingLotManagementSystem.Services
{
    public class ParkingLotService : IParkingLotService
    {
        private ParkingLot? _parkingLot;
        private ISlotAllocationStrategy _allocationStrategy;
        private IFeeStrategy _feeStrategy;

        private readonly Dictionary<string, Ticket> _tickets = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, (string SlotId, int FloorNumber)> _activeVehicleLocationIndex = new(StringComparer.OrdinalIgnoreCase);
        private int _ticketSequence = 1000;

        public ParkingLotService(
            ISlotAllocationStrategy? allocationStrategy = null,
            IFeeStrategy? feeStrategy = null)
        {
            _allocationStrategy = allocationStrategy ?? new LowestFloorLowestSlotAllocationStrategy();
            _feeStrategy = feeStrategy ?? new FlatHourlyFeeStrategy();
        }

        public void CreateParkingLot(string id, string name)
        {
            _parkingLot = new ParkingLot(id, name);
        }

        public void AddFloor(int floorNumber)
        {
            EnsureParkingLotExists();
            var floor = new ParkingFloor(floorNumber);
            _parkingLot!.AddFloor(floor);
        }

        public void AddSlot(int floorNumber, int slotNumber, VehicleType supportedVehicleType)
        {
            EnsureParkingLotExists();
            var floor = _parkingLot!.GetFloor(floorNumber);
            if (floor == null)
            {
                floor = new ParkingFloor(floorNumber);
                _parkingLot.AddFloor(floor);
            }

            var slot = new ParkingSlot(floorNumber, slotNumber, supportedVehicleType);
            floor.AddSlot(slot);
        }

        public void SetFeeStrategy(IFeeStrategy feeStrategy)
        {
            _feeStrategy = feeStrategy ?? throw new ArgumentNullException(nameof(feeStrategy));
        }

        public void SetAllocationStrategy(ISlotAllocationStrategy allocationStrategy)
        {
            _allocationStrategy = allocationStrategy ?? throw new ArgumentNullException(nameof(allocationStrategy));
        }

        public Ticket ParkVehicle(VehicleType type, string licensePlate, string color = "White", DateTime? entryTime = null)
        {
            EnsureParkingLotExists();

            if (string.IsNullOrWhiteSpace(licensePlate))
            {
                throw new ArgumentException("License plate cannot be empty.", nameof(licensePlate));
            }

            string cleanPlate = licensePlate.Trim().ToUpperInvariant();

            // 1. Validation: Duplicate Vehicle Parking
            if (_activeVehicleLocationIndex.ContainsKey(cleanPlate))
            {
                throw new DuplicateVehicleParkingException(cleanPlate);
            }

            // 2. Factory Pattern: Create Vehicle instance
            Vehicle vehicle = VehicleFactory.CreateVehicle(type, cleanPlate, color);

            // 3. Strategy Pattern: Slot Allocation Strategy
            var targetSlot = _allocationStrategy.AllocateSlot(_parkingLot!.GetAllFloorsSorted(), type);
            if (targetSlot == null)
            {
                throw new SlotUnavailableException(type.ToString());
            }

            DateTime now = entryTime ?? DateTime.Now;

            // Park vehicle in slot
            targetSlot.Park(vehicle, now);

            // Generate ticket
            string ticketId = $"PRK-{_parkingLot.ParkingLotId}-F{targetSlot.FloorNumber}-S{targetSlot.SlotNumber}-{_ticketSequence++}";
            var ticket = new Ticket(ticketId, cleanPlate, type, targetSlot.FloorNumber, targetSlot.SlotId, now);

            _tickets[ticketId] = ticket;
            _activeVehicleLocationIndex[cleanPlate] = (targetSlot.SlotId, targetSlot.FloorNumber);

            return ticket;
        }

        public UnparkResult ProcessExit(string ticketId, DateTime? exitTime = null)
        {
            EnsureParkingLotExists();

            if (string.IsNullOrWhiteSpace(ticketId))
            {
                throw new InvalidTicketException(ticketId ?? "NULL");
            }

            // 1. Validation: Invalid Ticket
            if (!_tickets.TryGetValue(ticketId.Trim(), out var ticket))
            {
                throw new InvalidTicketException(ticketId);
            }

            // 2. Validation: Repeated Exit / Already Exited Ticket
            if (ticket.Status == TicketStatus.PAID_AND_COMPLETED)
            {
                throw new TicketAlreadyExitedException(ticketId);
            }

            var floor = _parkingLot!.GetFloor(ticket.FloorNumber);
            var slot = floor?.GetSlotById(ticket.SlotId);

            if (slot == null || slot.ParkedVehicle == null)
            {
                throw new ParkingLotException($"Inconsistent state: Slot '{ticket.SlotId}' does not contain expected vehicle for ticket '{ticketId}'.");
            }

            DateTime actualExitTime = exitTime ?? DateTime.Now;

            if (actualExitTime < ticket.EntryTime)
            {
                throw new ParkingLotException($"Exit time ({actualExitTime:yyyy-MM-dd HH:mm}) cannot be earlier than entry time ({ticket.EntryTime:yyyy-MM-dd HH:mm}).");
            }

            TimeSpan duration = actualExitTime - ticket.EntryTime;

            // 3. Strategy Pattern: Fee Calculation Strategy
            decimal fee = _feeStrategy.CalculateFee(ticket.VehicleType, duration);

            // Complete ticket
            ticket.CompleteExit(actualExitTime, fee);

            // Unpark vehicle & release slot
            Vehicle vehicle = slot.Unpark();

            // Clear vehicle from active location index
            _activeVehicleLocationIndex.Remove(ticket.LicensePlate);

            return new UnparkResult
            {
                Ticket = ticket,
                Vehicle = vehicle,
                TotalFee = fee,
                Duration = duration,
                StrategyUsed = _feeStrategy.StrategyName
            };
        }

        public List<ParkingSlot> ViewFreeSlots(VehicleType vehicleType)
        {
            EnsureParkingLotExists();
            var freeSlots = new List<ParkingSlot>();
            foreach (var floor in _parkingLot!.GetAllFloorsSorted())
            {
                freeSlots.AddRange(floor.GetFreeSlots(vehicleType));
            }
            return freeSlots;
        }

        public OccupancyReport GetOccupancyReport()
        {
            EnsureParkingLotExists();
            var report = new OccupancyReport
            {
                ParkingLotName = _parkingLot!.Name
            };

            int totalCap = 0;
            int totalOcc = 0;

            foreach (var floor in _parkingLot.GetAllFloorsSorted())
            {
                var detail = new FloorOccupancyDetail
                {
                    FloorNumber = floor.FloorNumber,
                    TotalSlots = floor.GetTotalSlotsCount(),
                    OccupiedSlots = floor.GetOccupiedSlots().Count,
                    FreeSlots = floor.GetTotalSlotsCount() - floor.GetOccupiedSlots().Count
                };

                foreach (VehicleType vt in Enum.GetValues(typeof(VehicleType)))
                {
                    detail.FreeSlotsByVehicleType[vt] = floor.GetFreeSlotsCount(vt);
                    detail.OccupiedSlotsByVehicleType[vt] = floor.Slots.Count(s => s.SupportedVehicleType == vt && s.Status == SlotStatus.OCCUPIED);
                }

                totalCap += detail.TotalSlots;
                totalOcc += detail.OccupiedSlots;
                report.FloorDetails.Add(detail);
            }

            report.TotalCapacity = totalCap;
            report.TotalOccupied = totalOcc;

            return report;
        }

        public Ticket? GetTicket(string ticketId)
        {
            if (string.IsNullOrWhiteSpace(ticketId)) return null;
            return _tickets.TryGetValue(ticketId.Trim(), out var ticket) ? ticket : null;
        }

        public ParkingLot GetParkingLotDetails()
        {
            EnsureParkingLotExists();
            return _parkingLot!;
        }

        private void EnsureParkingLotExists()
        {
            if (_parkingLot == null)
            {
                throw new InvalidOperationException("Parking lot has not been initialized yet. Call CreateParkingLot() first.");
            }
        }
    }
}
