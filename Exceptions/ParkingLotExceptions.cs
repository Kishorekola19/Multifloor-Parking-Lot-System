using System;

namespace ParkingLotManagementSystem.Exceptions
{
    public class ParkingLotException : Exception
    {
        public ParkingLotException(string message) : base(message) { }
    }

    public class DuplicateVehicleParkingException : ParkingLotException
    {
        public string LicensePlate { get; }

        public DuplicateVehicleParkingException(string licensePlate)
            : base($"Vehicle with license plate '{licensePlate}' is already parked in the parking lot.")
        {
            LicensePlate = licensePlate;
        }
    }

    public class SlotUnavailableException : ParkingLotException
    {
        public string VehicleType { get; }

        public SlotUnavailableException(string vehicleType)
            : base($"No available parking slot found for vehicle type '{vehicleType}'.")
        {
            VehicleType = vehicleType;
        }
    }

    public class InvalidTicketException : ParkingLotException
    {
        public string TicketId { get; }

        public InvalidTicketException(string ticketId)
            : base($"Invalid or non-existent ticket ID: '{ticketId}'.")
        {
            TicketId = ticketId;
        }
    }

    public class TicketAlreadyExitedException : ParkingLotException
    {
        public string TicketId { get; }

        public TicketAlreadyExitedException(string ticketId)
            : base($"Ticket '{ticketId}' has already been processed and exited.")
        {
            TicketId = ticketId;
        }
    }
}
