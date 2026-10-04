using System;
using System.Collections.Generic;
using System.Linq;

namespace ParkingLotManagementSystem.Models
{
    public class ParkingLot
    {
        public string ParkingLotId { get; }
        public string Name { get; }
        private readonly Dictionary<int, ParkingFloor> _floors;

        public IReadOnlyDictionary<int, ParkingFloor> Floors => _floors;

        public ParkingLot(string parkingLotId, string name)
        {
            if (string.IsNullOrWhiteSpace(parkingLotId)) throw new ArgumentException("Parking lot ID required.", nameof(parkingLotId));
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Parking lot name required.", nameof(name));

            ParkingLotId = parkingLotId;
            Name = name;
            _floors = new Dictionary<int, ParkingFloor>();
        }

        public void AddFloor(ParkingFloor floor)
        {
            if (floor == null) throw new ArgumentNullException(nameof(floor));
            if (_floors.ContainsKey(floor.FloorNumber))
                throw new InvalidOperationException($"Floor {floor.FloorNumber} already exists in parking lot '{Name}'.");

            _floors[floor.FloorNumber] = floor;
        }

        public ParkingFloor? GetFloor(int floorNumber)
        {
            return _floors.TryGetValue(floorNumber, out var floor) ? floor : null;
        }

        public List<ParkingFloor> GetAllFloorsSorted()
        {
            return _floors.Values.OrderBy(f => f.FloorNumber).ToList();
        }
    }
}
