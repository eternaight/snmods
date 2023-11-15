using System.Collections.Generic;
using UnityEngine;

namespace WarperTravel
{
    public static class WarperSubspace
    {
        private static List<Vector3> portalDestinations = new ();

        private static List<Vector3> portalLocations = new ();

        public static Vector3 GetINLocation() {
            return Random.onUnitSphere * 2 + portalLocations[Random.Range(0, portalLocations.Count)];
        }
        public static Vector3 GetOUTLocation() => portalDestinations[0];
        public static void RegisterGate(Vector3 location) 
        {
            portalLocations.Add(location);
            portalDestinations.Add(Vector3.zero);
        }
    }
}