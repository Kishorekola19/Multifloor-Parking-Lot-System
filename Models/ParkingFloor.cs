using System;
using System.Collections.Generic;
using System.Linq;
using ParkingLotManagementSystem.Enums;
using ParkingLotManagementSystem.Exceptions;

namespace ParkingLotManagementSystem.Models
{
    public class ParkingFloor
    {
        public int FloorNumber { get; }
        public string FloorId { get; }
        private readonly List<ParkingSlot> _slots;

        public IReadOnlyList<ParkingSlot> Slots => _slots.AsReadOnly();

        public ParkingFloor(int floorNumber)
        {
            if (floorNumber <= 0)
                throw new ArgumentOutOfRangeException(nameof(floorNumber), "Floor number must be > 0.");

            FloorNumber = floorNumber;
            FloorId = $"Floor-{floorNumber}";
            _slots = new List<ParkingSlot>();
        }

        public void AddSlot(ParkingSlot slot)
        {
            if (slot == null) throw new ArgumentNullException(nameof(slot));
            if (slot.FloorNumber != FloorNumber)
                throw new ParkingLotException($"Slot floor mismatch. Expected floor {FloorNumber}, got {slot.FloorNumber}.");

            if (_slots.Any(s => s.SlotNumber == slot.SlotNumber))
                throw new ParkingLotException($"Slot number {slot.SlotNumber} already exists on Floor {FloorNumber}.");

            _slots.Add(slot);
        }

        public List<ParkingSlot> GetFreeSlots(VehicleType vehicleType)
        {
            return _slots.Where(s => s.CanAccommodate(vehicleType)).OrderBy(s => s.SlotNumber).ToList();
        }

        public List<ParkingSlot> GetOccupiedSlots()
        {
            return _slots.Where(s => s.Status == SlotStatus.OCCUPIED).ToList();
        }

        public ParkingSlot? GetSlotById(string slotId)
        {
            return _slots.FirstOrDefault(s => string.Equals(s.SlotId, slotId, StringComparison.OrdinalIgnoreCase));
        }

        public int GetTotalSlotsCount() => _slots.Count;

        public int GetFreeSlotsCount(VehicleType vehicleType) => _slots.Count(s => s.CanAccommodate(vehicleType));
    }
}
