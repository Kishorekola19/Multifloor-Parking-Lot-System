using System;
using System.Collections.Generic;
using System.Linq;
using ParkingLotManagementSystem.Enums;
using ParkingLotManagementSystem.Exceptions;
using ParkingLotManagementSystem.Factories;
using ParkingLotManagementSystem.Models;
using ParkingLotManagementSystem.Services;
using ParkingLotManagementSystem.Strategies.Allocation;
using ParkingLotManagementSystem.Strategies.Fee;

namespace ParkingLotManagementSystem
{
    class Program
    {
        static void Main(string[] args)
        {
            PrintHeader("EXTENSIBLE MULTI-FLOOR PARKING LOT SYSTEM - LLD DEMO");

            IParkingLotService parkingService = new ParkingLotService();

            // --------------------------------------------------------------------------------
            // OPERATION 1: System & Floor/Slot Initialization
            // --------------------------------------------------------------------------------
            PrintSection("OPERATION 1: Initializing Parking Lot & Configuring Floors/Slots");
            parkingService.CreateParkingLot("LOT-01", "Grand Central Multi-Floor Parking Lot");

            // Floor 1 Setup: 2 Bike Slots, 2 Car Slots, 1 Truck Slot
            parkingService.AddFloor(1);
            parkingService.AddSlot(1, 1, VehicleType.BIKE);
            parkingService.AddSlot(1, 2, VehicleType.BIKE);
            parkingService.AddSlot(1, 3, VehicleType.CAR);
            parkingService.AddSlot(1, 4, VehicleType.CAR);
            parkingService.AddSlot(1, 5, VehicleType.TRUCK);

            // Floor 2 Setup: 1 Bike Slot, 2 Car Slots, 1 Truck Slot
            parkingService.AddFloor(2);
            parkingService.AddSlot(2, 1, VehicleType.BIKE);
            parkingService.AddSlot(2, 2, VehicleType.CAR);
            parkingService.AddSlot(2, 3, VehicleType.CAR);
            parkingService.AddSlot(2, 4, VehicleType.TRUCK);

            Console.WriteLine("[INFO] Parking Lot 'LOT-01' initialized with 2 Floors and 9 Total Slots.");

            // --------------------------------------------------------------------------------
            // OPERATION 2: Normal Vehicle Parking (Bike, Car, Truck)
            // --------------------------------------------------------------------------------
            PrintSection("OPERATION 2: Parking Vehicles (Normal Operations)");
            
            DateTime baseTime = new DateTime(2026, 10, 4, 10, 0, 0);

            var ticket1 = parkingService.ParkVehicle(VehicleType.BIKE, "KA-01-BK-1001", "Black", baseTime);
            Console.WriteLine($"[PARK SUCCESS] {ticket1}");

            var ticket2 = parkingService.ParkVehicle(VehicleType.CAR, "KA-05-CR-2002", "Blue", baseTime);
            Console.WriteLine($"[PARK SUCCESS] {ticket2}");

            var ticket3 = parkingService.ParkVehicle(VehicleType.TRUCK, "KA-03-TR-3003", "Red", baseTime);
            Console.WriteLine($"[PARK SUCCESS] {ticket3}");

            // --------------------------------------------------------------------------------
            // OPERATION 3: Viewing Free Slots & Detailed Occupancy Report
            // --------------------------------------------------------------------------------
            PrintSection("OPERATION 3: Viewing Free Slots & Displaying Occupancy Report");

            Console.WriteLine("[INFO] Free Car Slots Available:");
            var freeCarSlots = parkingService.ViewFreeSlots(VehicleType.CAR);
            foreach (var slot in freeCarSlots)
            {
                Console.WriteLine($"   - Floor {slot.FloorNumber}, Slot {slot.SlotId} ({slot.SupportedVehicleType})");
            }

            DisplayOccupancyReport(parkingService.GetOccupancyReport());

            // --------------------------------------------------------------------------------
            // OPERATION 4: Edge Case 1 - Validation of Duplicate Vehicle Parking
            // --------------------------------------------------------------------------------
            PrintSection("OPERATION 4: Validation Edge Case - Attempting Duplicate Vehicle Parking");
            try
            {
                Console.WriteLine("[ATTEMPT] Attempting to park vehicle 'KA-05-CR-2002' again while already parked...");
                parkingService.ParkVehicle(VehicleType.CAR, "KA-05-CR-2002", "Blue", baseTime.AddMinutes(15));
            }
            catch (DuplicateVehicleParkingException ex)
            {
                Console.WriteLine($"[EXPECTED EXCEPTION CAUGHT] {ex.Message}");
            }

            // --------------------------------------------------------------------------------
            // OPERATION 5: Edge Case 2 - Validation of Unavailable Slots (Parking Lot Full for Type)
            // --------------------------------------------------------------------------------
            PrintSection("OPERATION 5: Validation Edge Case - Parking When Slots Are Full");
            try
            {
                // Fill up remaining Truck slot (Floor 2, Slot 4)
                var ticketTruck2 = parkingService.ParkVehicle(VehicleType.TRUCK, "KA-03-TR-4004", "Yellow", baseTime);
                Console.WriteLine($"[PARK SUCCESS] Filled second truck slot: {ticketTruck2.SlotId}");

                Console.WriteLine("[ATTEMPT] Attempting to park a 3rd Truck when capacity is 2...");
                parkingService.ParkVehicle(VehicleType.TRUCK, "KA-03-TR-9999", "Green", baseTime);
            }
            catch (SlotUnavailableException ex)
            {
                Console.WriteLine($"[EXPECTED EXCEPTION CAUGHT] {ex.Message}");
            }

            // --------------------------------------------------------------------------------
            // OPERATION 6: Normal Vehicle Unparking & Fee Processing (Flat Hourly Fee Strategy)
            // --------------------------------------------------------------------------------
            PrintSection("OPERATION 6: Unparking Vehicle & Fee Calculation (Flat Hourly Rate)");

            DateTime carExitTime = baseTime.AddHours(3).AddMinutes(15); // 3h 15m => 4 hours billed
            Console.WriteLine($"[INFO] Unparking Car '{ticket2.LicensePlate}' (Ticket: {ticket2.TicketId}). Billed Duration: 3h 15m (rounded up to 4 hrs)");

            var exitResult2 = parkingService.ProcessExit(ticket2.TicketId, carExitTime);
            Console.WriteLine($"[EXIT COMPLETED]");
            Console.WriteLine($"   Vehicle: {exitResult2.Vehicle}");
            Console.WriteLine($"   Duration: {exitResult2.Duration.Hours}h {exitResult2.Duration.Minutes}m");
            Console.WriteLine($"   Fee Strategy: {exitResult2.StrategyUsed}");
            Console.WriteLine($"   Total Fee Billed: ${exitResult2.TotalFee:F2}");

            // --------------------------------------------------------------------------------
            // OPERATION 7: Edge Case 3 - Validation of Repeated Exit Attempt
            // --------------------------------------------------------------------------------
            PrintSection("OPERATION 7: Validation Edge Case - Attempting Repeated Exit");
            try
            {
                Console.WriteLine($"[ATTEMPT] Attempting to unpark using ticket '{ticket2.TicketId}' a second time...");
                parkingService.ProcessExit(ticket2.TicketId, carExitTime.AddMinutes(10));
            }
            catch (TicketAlreadyExitedException ex)
            {
                Console.WriteLine($"[EXPECTED EXCEPTION CAUGHT] {ex.Message}");
            }

            // --------------------------------------------------------------------------------
            // OPERATION 8: Edge Case 4 - Validation of Invalid Ticket
            // --------------------------------------------------------------------------------
            PrintSection("OPERATION 8: Validation Edge Case - Processing Invalid/Non-Existent Ticket");
            try
            {
                string invalidTicketId = "PRK-LOT01-F9-S99-99999";
                Console.WriteLine($"[ATTEMPT] Attempting to exit with invalid ticket ID '{invalidTicketId}'...");
                parkingService.ProcessExit(invalidTicketId, DateTime.Now);
            }
            catch (InvalidTicketException ex)
            {
                Console.WriteLine($"[EXPECTED EXCEPTION CAUGHT] {ex.Message}");
            }

            // --------------------------------------------------------------------------------
            // OPERATION 9: Design Pattern Demonstration - Dynamic Pricing Strategy Switch
            // --------------------------------------------------------------------------------
            PrintSection("OPERATION 9: Design Pattern Demo - Switching Fee Strategy (Strategy Pattern)");

            var tieredStrategy = FeeStrategyFactory.CreateStrategy(FeeStrategyType.TIERED_RATES);
            parkingService.SetFeeStrategy(tieredStrategy);
            Console.WriteLine($"[STRATEGY SWITCH] Fee Strategy switched to: '{tieredStrategy.StrategyName}'");

            DateTime bikeParkTime = baseTime;
            DateTime bikeExitTime = baseTime.AddHours(4); // 4 Hours

            Console.WriteLine($"[INFO] Processing Bike exit ('{ticket1.LicensePlate}') after 4 hours with Tiered Strategy...");
            var bikeExitResult = parkingService.ProcessExit(ticket1.TicketId, bikeExitTime);

            Console.WriteLine($"[EXIT COMPLETED]");
            Console.WriteLine($"   Strategy Used: {bikeExitResult.StrategyUsed}");
            Console.WriteLine($"   Fee Calculation: 4 hours (First 2 hrs = $15.00 base, Next 2 hrs @ $5/hr = $10.00)");
            Console.WriteLine($"   Total Fee Charged: ${bikeExitResult.TotalFee:F2}");

            // --------------------------------------------------------------------------------
            // OPERATION 10: Design Pattern Demonstration - Dynamic Slot Allocation Strategy Switch
            // --------------------------------------------------------------------------------
            PrintSection("OPERATION 10: Design Pattern Demo - Switching Allocation Strategy (Highest Floor First)");

            parkingService.SetAllocationStrategy(new HighestFloorFirstAllocationStrategy());
            Console.WriteLine($"[STRATEGY SWITCH] Slot Allocation Strategy switched to: 'HighestFloorFirstAllocationStrategy'");

            var ticketTopFloor = parkingService.ParkVehicle(VehicleType.CAR, "KA-02-HF-7777", "Silver", baseTime);
            Console.WriteLine($"[PARK SUCCESS] Car parked at: Floor {ticketTopFloor.FloorNumber}, Slot {ticketTopFloor.SlotId}");

            PrintSection("PARKING LOT SYSTEM DEMO COMPLETED SUCCESSFULLY!");
        }

        private static void PrintHeader(string title)
        {
            Console.WriteLine(new string('=', 80));
            Console.WriteLine($"  {title}");
            Console.WriteLine(new string('=', 80));
            Console.WriteLine();
        }

        private static void PrintSection(string header)
        {
            Console.WriteLine();
            Console.WriteLine(new string('-', 80));
            Console.WriteLine($">>> {header}");
            Console.WriteLine(new string('-', 80));
        }

        private static void DisplayOccupancyReport(OccupancyReport report)
        {
            Console.WriteLine();
            Console.WriteLine($"--- PARKING LOT OCCUPANCY REPORT: '{report.ParkingLotName}' ---");
            Console.WriteLine($"   Overall Capacity: {report.TotalCapacity} slots | Occupied: {report.TotalOccupied} | Free: {report.TotalFree}");
            Console.WriteLine();
            foreach (var f in report.FloorDetails)
            {
                Console.WriteLine($"   Floor {f.FloorNumber}: Total: {f.TotalSlots} | Occupied: {f.OccupiedSlots} | Free: {f.FreeSlots}");
                Console.WriteLine($"      - Bike Free: {f.FreeSlotsByVehicleType[VehicleType.BIKE]} | Car Free: {f.FreeSlotsByVehicleType[VehicleType.CAR]} | Truck Free: {f.FreeSlotsByVehicleType[VehicleType.TRUCK]}");
            }
            Console.WriteLine();
        }
    }
}
