using System;
using ParkingLotManagementSystem.Enums;
using ParkingLotManagementSystem.Models;

namespace ParkingLotManagementSystem.Factories
{
    public static class VehicleFactory
    {
        public static Vehicle CreateVehicle(VehicleType type, string licensePlate, string color = "White")
        {
            if (string.IsNullOrWhiteSpace(licensePlate))
            {
                throw new ArgumentException("License plate cannot be null or empty.", nameof(licensePlate));
            }

            return type switch
            {
                VehicleType.BIKE => new Bike(licensePlate, color),
                VehicleType.CAR => new Car(licensePlate, color),
                VehicleType.TRUCK => new Truck(licensePlate, color),
                _ => throw new NotSupportedException($"Vehicle type '{type}' is not supported by VehicleFactory.")
            };
        }
    }
}
