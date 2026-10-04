using System;
using ParkingLotManagementSystem.Enums;
using ParkingLotManagementSystem.Exceptions;

namespace ParkingLotManagementSystem.Models
{
    public class Ticket
    {
        public string TicketId { get; }
        public string LicensePlate { get; }
        public VehicleType VehicleType { get; }
        public int FloorNumber { get; }
        public string SlotId { get; }
        public DateTime EntryTime { get; }
        public DateTime? ExitTime { get; private set; }
        public TicketStatus Status { get; private set; }
        public decimal Fee { get; private set; }

        public Ticket(string ticketId, string licensePlate, VehicleType vehicleType, int floorNumber, string slotId, DateTime entryTime)
        {
            if (string.IsNullOrWhiteSpace(ticketId)) throw new ArgumentException("Ticket ID cannot be empty.", nameof(ticketId));
            if (string.IsNullOrWhiteSpace(licensePlate)) throw new ArgumentException("License plate cannot be empty.", nameof(licensePlate));
            if (string.IsNullOrWhiteSpace(slotId)) throw new ArgumentException("Slot ID cannot be empty.", nameof(slotId));

            TicketId = ticketId;
            LicensePlate = licensePlate.Trim().ToUpperInvariant();
            VehicleType = vehicleType;
            FloorNumber = floorNumber;
            SlotId = slotId;
            EntryTime = entryTime;
            Status = TicketStatus.ACTIVE;
            Fee = 0.0m;
        }

        public void CompleteExit(DateTime exitTime, decimal fee)
        {
            if (Status != TicketStatus.ACTIVE)
            {
                throw new TicketAlreadyExitedException(TicketId);
            }

            if (exitTime < EntryTime)
            {
                throw new ParkingLotException($"Exit time ({exitTime}) cannot be earlier than entry time ({EntryTime}).");
            }

            ExitTime = exitTime;
            Fee = fee;
            Status = TicketStatus.PAID_AND_COMPLETED;
        }

        public override string ToString() => $"Ticket[{TicketId}] | Vehicle: {LicensePlate} ({VehicleType}) | Floor: {FloorNumber} | Slot: {SlotId} | Status: {Status} | Entry: {EntryTime:yyyy-MM-dd HH:mm:ss}";
    }
}
