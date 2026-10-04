using System;
using ParkingLotManagementSystem.Enums;
using ParkingLotManagementSystem.Exceptions;

namespace ParkingLotManagementSystem.Models
{
    public class ParkingSlot
    {
        public string SlotId { get; }
        public int FloorNumber { get; }
        public int SlotNumber { get; }
        public VehicleType SupportedVehicleType { get; }
        public SlotStatus Status { get; private set; }
        public Vehicle? ParkedVehicle { get; private set; }
        public DateTime? OccupiedTime { get; private set; }

        public ParkingSlot(int floorNumber, int slotNumber, VehicleType supportedVehicleType)
        {
            if (floorNumber <= 0) throw new ArgumentOutOfRangeException(nameof(floorNumber), "Floor number must be positive.");
            if (slotNumber <= 0) throw new ArgumentOutOfRangeException(nameof(slotNumber), "Slot number must be positive.");

            FloorNumber = floorNumber;
            SlotNumber = slotNumber;
            SupportedVehicleType = supportedVehicleType;
            SlotId = $"F{floorNumber}-S{slotNumber}";
            Status = SlotStatus.AVAILABLE;
        }

        public bool IsAvailable => Status == SlotStatus.AVAILABLE;

        public bool CanAccommodate(VehicleType vehicleType) => SupportedVehicleType == vehicleType && IsAvailable;

        public void Park(Vehicle vehicle, DateTime entryTime)
        {
            if (!IsAvailable)
            {
                throw new ParkingLotException($"Slot '{SlotId}' is not available for parking.");
            }

            if (vehicle.Type != SupportedVehicleType)
            {
                throw new ParkingLotException($"Slot '{SlotId}' supports '{SupportedVehicleType}' but vehicle is '{vehicle.Type}'.");
            }

            ParkedVehicle = vehicle;
            Status = SlotStatus.OCCUPIED;
            OccupiedTime = entryTime;
        }

        public Vehicle Unpark()
        {
            if (Status != SlotStatus.OCCUPIED || ParkedVehicle == null)
            {
                throw new ParkingLotException($"Slot '{SlotId}' is not currently occupied.");
            }

            Vehicle vehicle = ParkedVehicle;
            ParkedVehicle = null;
            Status = SlotStatus.AVAILABLE;
            OccupiedTime = null;
            return vehicle;
        }

        public void SetOutOfService()
        {
            if (Status == SlotStatus.OCCUPIED)
            {
                throw new ParkingLotException($"Cannot mark slot '{SlotId}' out of service while occupied.");
            }
            Status = SlotStatus.OUT_OF_SERVICE;
        }

        public void RestoreToService()
        {
            if (Status == SlotStatus.OUT_OF_SERVICE)
            {
                Status = SlotStatus.AVAILABLE;
            }
        }

        public override string ToString() => $"Slot[{SlotId} | Type: {SupportedVehicleType} | Status: {Status}]";
    }
}
