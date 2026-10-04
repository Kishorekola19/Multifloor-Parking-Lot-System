using System;
using System.Collections.Generic;
using ParkingLotManagementSystem.Enums;

namespace ParkingLotManagementSystem.Strategies.Fee
{
    public interface IFeeStrategy
    {
        string StrategyName { get; }
        decimal CalculateFee(VehicleType vehicleType, TimeSpan duration);
    }

    /// <summary>
    /// Standard Flat Hourly Fee Strategy.
    /// Charges per hour (rounded up to nearest hour, min 1 hour).
    /// Hourly Rates: BIKE = $10/hr, CAR = $20/hr, TRUCK = $30/hr.
    /// </summary>
    public class FlatHourlyFeeStrategy : IFeeStrategy
    {
        public string StrategyName => "Flat Hourly Rate Strategy";

        private readonly Dictionary<VehicleType, decimal> _hourlyRates = new()
        {
            { VehicleType.BIKE, 10.0m },
            { VehicleType.CAR, 20.0m },
            { VehicleType.TRUCK, 30.0m }
        };

        public decimal CalculateFee(VehicleType vehicleType, TimeSpan duration)
        {
            int hours = (int)Math.Ceiling(duration.TotalHours);
            if (hours <= 0) hours = 1;

            decimal rate = _hourlyRates.TryGetValue(vehicleType, out var r) ? r : 20.0m;
            return hours * rate;
        }
    }

    /// <summary>
    /// Tiered Pricing Fee Strategy.
    /// First 2 hours flat base rate, then discounted hourly rate for additional hours.
    /// </summary>
    public class TieredVehicleTypeFeeStrategy : IFeeStrategy
    {
        public string StrategyName => "Tiered Base + Incremental Rate Strategy";

        private readonly Dictionary<VehicleType, (decimal base2Hrs, decimal extraPerHour)> _rates = new()
        {
            { VehicleType.BIKE, (15.0m, 5.0m) },
            { VehicleType.CAR, (35.0m, 15.0m) },
            { VehicleType.TRUCK, (50.0m, 25.0m) }
        };

        public decimal CalculateFee(VehicleType vehicleType, TimeSpan duration)
        {
            int hours = (int)Math.Ceiling(duration.TotalHours);
            if (hours <= 0) hours = 1;

            var (baseRate, extraRate) = _rates.TryGetValue(vehicleType, out var r) ? r : (35.0m, 15.0m);

            if (hours <= 2)
            {
                return baseRate;
            }

            int extraHours = hours - 2;
            return baseRate + (extraHours * extraRate);
        }
    }
}
