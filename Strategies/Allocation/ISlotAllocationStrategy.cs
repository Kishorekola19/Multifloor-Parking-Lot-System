using System.Collections.Generic;
using System.Linq;
using ParkingLotManagementSystem.Enums;
using ParkingLotManagementSystem.Models;

namespace ParkingLotManagementSystem.Strategies.Allocation
{
    public interface ISlotAllocationStrategy
    {
        ParkingSlot? AllocateSlot(IEnumerable<ParkingFloor> floors, VehicleType vehicleType);
    }

    /// <summary>
    /// Allocates slot on the lowest floor available, picking the lowest slot number.
    /// </summary>
    public class LowestFloorLowestSlotAllocationStrategy : ISlotAllocationStrategy
    {
        public ParkingSlot? AllocateSlot(IEnumerable<ParkingFloor> floors, VehicleType vehicleType)
        {
            var sortedFloors = floors.OrderBy(f => f.FloorNumber);

            foreach (var floor in sortedFloors)
            {
                var availableSlots = floor.GetFreeSlots(vehicleType);
                if (availableSlots.Count > 0)
                {
                    return availableSlots.First();
                }
            }

            return null; // Full or unavailable
        }
    }

    /// <summary>
    /// Extensible Strategy: Allocates starting from top floor downwards.
    /// </summary>
    public class HighestFloorFirstAllocationStrategy : ISlotAllocationStrategy
    {
        public ParkingSlot? AllocateSlot(IEnumerable<ParkingFloor> floors, VehicleType vehicleType)
        {
            var sortedFloors = floors.OrderByDescending(f => f.FloorNumber);

            foreach (var floor in sortedFloors)
            {
                var availableSlots = floor.GetFreeSlots(vehicleType);
                if (availableSlots.Count > 0)
                {
                    return availableSlots.First();
                }
            }

            return null;
        }
    }
}
