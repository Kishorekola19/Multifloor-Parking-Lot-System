using System;
using ParkingLotManagementSystem.Strategies.Fee;

namespace ParkingLotManagementSystem.Factories
{
    public enum FeeStrategyType
    {
        FLAT_HOURLY,
        TIERED_RATES
    }

    public static class FeeStrategyFactory
    {
        public static IFeeStrategy CreateStrategy(FeeStrategyType strategyType)
        {
            return strategyType switch
            {
                FeeStrategyType.FLAT_HOURLY => new FlatHourlyFeeStrategy(),
                FeeStrategyType.TIERED_RATES => new TieredVehicleTypeFeeStrategy(),
                _ => throw new NotSupportedException($"Fee strategy type '{strategyType}' is not supported.")
            };
        }
    }
}
