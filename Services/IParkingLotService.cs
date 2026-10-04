using System;
using System.Collections.Generic;
using ParkingLotManagementSystem.Enums;
using ParkingLotManagementSystem.Models;
using ParkingLotManagementSystem.Strategies.Allocation;
using ParkingLotManagementSystem.Strategies.Fee;

namespace ParkingLotManagementSystem.Services
{
    public class UnparkResult
    {
        public Ticket Ticket { get; set; } = null!;
        public Vehicle Vehicle { get; set; } = null!;
        public decimal TotalFee { get; set; }
        public TimeSpan Duration { get; set; }
        public string StrategyUsed { get; set; } = string.Empty;
    }

    public interface IParkingLotService
    {
        void CreateParkingLot(string id, string name);
        void AddFloor(int floorNumber);
        void AddSlot(int floorNumber, int slotNumber, VehicleType supportedVehicleType);
        void SetFeeStrategy(IFeeStrategy feeStrategy);
        void SetAllocationStrategy(ISlotAllocationStrategy allocationStrategy);

        Ticket ParkVehicle(VehicleType type, string licensePlate, string color = "White", DateTime? entryTime = null);
        UnparkResult ProcessExit(string ticketId, DateTime? exitTime = null);

        List<ParkingSlot> ViewFreeSlots(VehicleType vehicleType);
        OccupancyReport GetOccupancyReport();

        Ticket? GetTicket(string ticketId);
        ParkingLot GetParkingLotDetails();
    }
}
