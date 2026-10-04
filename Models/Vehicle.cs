using System;
using ParkingLotManagementSystem.Enums;

namespace ParkingLotManagementSystem.Models
{
    public abstract class Vehicle
    {
        public string LicensePlate { get; }
        public string Color { get; }
        public VehicleType Type { get; }

        protected Vehicle(string licensePlate, string color, VehicleType type)
        {
            if (string.IsNullOrWhiteSpace(licensePlate))
                throw new ArgumentException("License plate cannot be null or empty.", nameof(licensePlate));

            LicensePlate = licensePlate.Trim().ToUpperInvariant();
            Color = string.IsNullOrWhiteSpace(color) ? "Unknown" : color.Trim();
            Type = type;
        }

        public override string ToString() => $"{Type} [{LicensePlate}] ({Color})";
    }

    public class Bike : Vehicle
    {
        public Bike(string licensePlate, string color = "Black")
            : base(licensePlate, color, VehicleType.BIKE) { }
    }

    public class Car : Vehicle
    {
        public Car(string licensePlate, string color = "White")
            : base(licensePlate, color, VehicleType.CAR) { }
    }

    public class Truck : Vehicle
    {
        public Truck(string licensePlate, string color = "Red")
            : base(licensePlate, color, VehicleType.TRUCK) { }
    }
}
