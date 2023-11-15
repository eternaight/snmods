using UnityEngine;

namespace NewPlugin.ProcGen
{
    public static class RandomUtils
    {
        public static Vector3 RandomInsideBounds(Vector3 min, Vector3 max) => new 
        (
            Mathf.Lerp(min.x, max.x, Random.value),
            Mathf.Lerp(min.y, max.y, Random.value),
            Mathf.Lerp(min.z, max.z, Random.value)
        );
        public static Int3 RandomInsideBounds(System.Random prng, Int3 min, Int3 max) => new 
        (
            prng.Next(min.x, max.x),
            prng.Next(min.y, max.y),
            prng.Next(min.z, max.z)
        );
        public static Int2 RandomInsideBounds(System.Random prng, Int2 min, Int2 max) => new 
        (
            prng.Next(min.x, max.x),
            prng.Next(min.y, max.y)
        );

        public static Int2[] ScatterWithMinSeparation(System.Random prng, Int2 min, Int2 max, int num_positions, float separation) {
            var result = new Int2[num_positions];

            for (int i = 0; i < num_positions; i++)
            {
                var pos = Int2.zero;
                var pos_successful = false;

                for (int iterations = 0; iterations < 128 && !pos_successful; iterations++) {
                    pos = RandomInsideBounds(prng, min, max);
                    pos_successful = true;
                    for (int j = 0; i < j && pos_successful; j++)
                    {
                        var dist = result[j].GetDistance(pos);
                        pos_successful &= dist >= separation; 
                    }
                };

                result[i] = pos;
            }

            return result;
        }
    }
}