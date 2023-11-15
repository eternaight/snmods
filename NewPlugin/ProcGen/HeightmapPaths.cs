using UnityEngine;

namespace NewPlugin.ProcGen
{
    public static class HeightmapPaths
    {
        public static Vector3[] GenerateRandomPath(Vector3 min, Vector3 max)
        {
            var steps = 10;
            var points = new Vector3[steps];

            var startPosition = new Vector3(
                Mathf.Lerp(min.x, max.x, Random.value),
                Mathf.Lerp(min.y, max.y, Random.value),
                Mathf.Lerp(min.z, max.z, Random.value)
            );

            var direction = Random.onUnitSphere;

            for (int i = 0; i < steps; i++)
            {
                
            }

            return points;
        } 
    }
}