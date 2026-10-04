using System.Collections.Generic;
using ParkingLotManagementSystem.Enums;

namespace ParkingLotManagementSystem.Models
{
    public class FloorOccupancyDetail
    {
        public int FloorNumber { get; set; }
        public int TotalSlots { get; set; }
        public int OccupiedSlots { get; set; }
        public int FreeSlots { get; set; }
        public Dictionary<VehicleType, int> FreeSlotsByVehicleType { get; set; } = new();
        public Dictionary<VehicleType, int> OccupiedSlotsByVehicleType { get; set; } = new();
    }

    public class OccupancyReport
    {
        public string ParkingLotName { get; set; } = string.Empty;
        public int TotalCapacity { get; set; }
        public int TotalOccupied { get; set; }
        public int TotalFree => TotalCapacity - TotalOccupied;
        public List<FloorOccupancyDetail> FloorDetails { get; set; } = new();
    }
}
