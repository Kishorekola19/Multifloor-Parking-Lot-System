using System;
using System.Linq;
using ParkingLotManagementSystem.Enums;
using ParkingLotManagementSystem.Exceptions;
using ParkingLotManagementSystem.Factories;
using ParkingLotManagementSystem.Models;
using ParkingLotManagementSystem.Services;
using ParkingLotManagementSystem.Strategies.Allocation;
using ParkingLotManagementSystem.Strategies.Fee;
using Xunit;

namespace ParkingLotSystem.Tests
{
    public class ParkingLotServiceTests
    {
        private IParkingLotService CreateTestParkingLotService()
        {
            var service = new ParkingLotService();
            service.CreateParkingLot("TEST-LOT", "Test Parking Building");

            // Floor 1: 1 Bike, 1 Car, 1 Truck
            service.AddSlot(1, 1, VehicleType.BIKE);
            service.AddSlot(1, 2, VehicleType.CAR);
            service.AddSlot(1, 3, VehicleType.TRUCK);

            // Floor 2: 1 Bike, 1 Car
            service.AddSlot(2, 1, VehicleType.BIKE);
            service.AddSlot(2, 2, VehicleType.CAR);

            return service;
        }

        [Fact]
        public void Test1_ParkVehicle_SuccessfulAllocation()
        {
            var service = CreateTestParkingLotService();

            var ticket = service.ParkVehicle(VehicleType.CAR, "KA-01-AA-1111", "Red");

            Assert.NotNull(ticket);
            Assert.Equal("KA-01-AA-1111", ticket.LicensePlate);
            Assert.Equal(VehicleType.CAR, ticket.VehicleType);
            Assert.Equal(1, ticket.FloorNumber);
            Assert.Equal("F1-S2", ticket.SlotId);
            Assert.Equal(TicketStatus.ACTIVE, ticket.Status);
        }

        [Fact]
        public void Test2_DuplicateVehicleParking_ThrowsDuplicateVehicleParkingException()
        {
            var service = CreateTestParkingLotService();
            service.ParkVehicle(VehicleType.CAR, "KA-01-AA-1111", "Red");

            var ex = Assert.Throws<DuplicateVehicleParkingException>(() =>
            {
                service.ParkVehicle(VehicleType.CAR, "KA-01-AA-1111", "Blue");
            });

            Assert.Equal("KA-01-AA-1111", ex.LicensePlate);
        }

        [Fact]
        public void Test3_SlotUnavailable_ThrowsSlotUnavailableException()
        {
            var service = CreateTestParkingLotService();
            // Floor 1 has only 1 truck slot
            service.ParkVehicle(VehicleType.TRUCK, "TRK-01", "Black");

            // Attempting second truck
            var ex = Assert.Throws<SlotUnavailableException>(() =>
            {
                service.ParkVehicle(VehicleType.TRUCK, "TRK-02", "White");
            });

            Assert.Equal("TRUCK", ex.VehicleType);
        }

        [Fact]
        public void Test4_ProcessExit_FlatHourlyFeeCalculation()
        {
            var service = CreateTestParkingLotService();
            service.SetFeeStrategy(new FlatHourlyFeeStrategy());

            DateTime entry = new DateTime(2026, 10, 4, 10, 0, 0);
            DateTime exit = entry.AddHours(2).AddMinutes(15); // 2h 15m => 3 hours billed

            var ticket = service.ParkVehicle(VehicleType.CAR, "KA-02-BB-2222", "Silver", entry);
            var result = service.ProcessExit(ticket.TicketId, exit);

            Assert.Equal(TicketStatus.PAID_AND_COMPLETED, result.Ticket.Status);
            Assert.Equal(60.0m, result.TotalFee); // $20/hr * 3 hrs = $60
            Assert.Equal("KA-02-BB-2222", result.Vehicle.LicensePlate);
        }

        [Fact]
        public void Test5_ProcessExit_TieredFeeCalculation()
        {
            var service = CreateTestParkingLotService();
            service.SetFeeStrategy(new TieredVehicleTypeFeeStrategy());

            DateTime entry = new DateTime(2026, 10, 4, 10, 0, 0);
            DateTime exit = entry.AddHours(4); // 4 hours

            var ticket = service.ParkVehicle(VehicleType.BIKE, "KA-03-BK-3333", "Green", entry);
            var result = service.ProcessExit(ticket.TicketId, exit);

            // Tiered Bike Rate: Base $15 for 2 hrs + $5/hr for 2 extra hrs = $25
            Assert.Equal(25.0m, result.TotalFee);
        }

        [Fact]
        public void Test6_RepeatedExit_ThrowsTicketAlreadyExitedException()
        {
            var service = CreateTestParkingLotService();
            var ticket = service.ParkVehicle(VehicleType.CAR, "KA-04-CC-4444", "Yellow");

            service.ProcessExit(ticket.TicketId);

            var ex = Assert.Throws<TicketAlreadyExitedException>(() =>
            {
                service.ProcessExit(ticket.TicketId);
            });

            Assert.Equal(ticket.TicketId, ex.TicketId);
        }

        [Fact]
        public void Test7_InvalidTicket_ThrowsInvalidTicketException()
        {
            var service = CreateTestParkingLotService();

            var ex = Assert.Throws<InvalidTicketException>(() =>
            {
                service.ProcessExit("INVALID-TICKET-ID-999");
            });

            Assert.Equal("INVALID-TICKET-ID-999", ex.TicketId);
        }

        [Fact]
        public void Test8_ViewFreeSlots_ReturnsCorrectCountAndSlots()
        {
            var service = CreateTestParkingLotService();
            // Free car slots before parking: 2 (F1-S2, F2-S2)
            var freeSlotsBefore = service.ViewFreeSlots(VehicleType.CAR);
            Assert.Equal(2, freeSlotsBefore.Count);

            service.ParkVehicle(VehicleType.CAR, "KA-05-DD-5555");

            var freeSlotsAfter = service.ViewFreeSlots(VehicleType.CAR);
            Assert.Single(freeSlotsAfter);
            Assert.Equal("F2-S2", freeSlotsAfter[0].SlotId);
        }

        [Fact]
        public void Test9_OccupancyReport_AccuratelyReflectsState()
        {
            var service = CreateTestParkingLotService();
            service.ParkVehicle(VehicleType.BIKE, "BK-01");
            service.ParkVehicle(VehicleType.CAR, "CR-01");

            var report = service.GetOccupancyReport();

            Assert.Equal(5, report.TotalCapacity);
            Assert.Equal(2, report.TotalOccupied);
            Assert.Equal(3, report.TotalFree);
            Assert.Equal(2, report.FloorDetails.Count);
        }

        [Fact]
        public void Test10_VehicleFactory_CreatesCorrectSubclasses()
        {
            var bike = VehicleFactory.CreateVehicle(VehicleType.BIKE, "BK-100", "Black");
            var car = VehicleFactory.CreateVehicle(VehicleType.CAR, "CR-200", "White");
            var truck = VehicleFactory.CreateVehicle(VehicleType.TRUCK, "TR-300", "Blue");

            Assert.IsType<Bike>(bike);
            Assert.IsType<Car>(car);
            Assert.IsType<Truck>(truck);
            Assert.Equal("BK-100", bike.LicensePlate);
        }

        [Fact]
        public void Test11_HighestFloorAllocationStrategy_AllocatesOnTopFloorFirst()
        {
            var service = CreateTestParkingLotService();
            service.SetAllocationStrategy(new HighestFloorFirstAllocationStrategy());

            var ticket = service.ParkVehicle(VehicleType.CAR, "TOP-FLOOR-CAR");

            Assert.Equal(2, ticket.FloorNumber);
            Assert.Equal("F2-S2", ticket.SlotId);
        }

        [Fact]
        public void Test12_UnparkVehicle_ReleasesSlotForNewVehicle()
        {
            var service = CreateTestParkingLotService();
            // Fill truck slot
            var t1 = service.ParkVehicle(VehicleType.TRUCK, "TRK-01");
            
            // Cannot park another truck
            Assert.Throws<SlotUnavailableException>(() => service.ParkVehicle(VehicleType.TRUCK, "TRK-02"));

            // Unpark first truck
            service.ProcessExit(t1.TicketId);

            // Now second truck can park in released slot!
            var t2 = service.ParkVehicle(VehicleType.TRUCK, "TRK-02");
            Assert.NotNull(t2);
            Assert.Equal("F1-S3", t2.SlotId);
        }
    }
}
