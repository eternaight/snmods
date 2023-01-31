using System.Linq;
using UnityEngine;

namespace NewPlugin.ProcGen
{
    public class FractalNoise
    {
        private readonly OpenSimplexNoise[] noises;
        private readonly Vector3 baseFrequency;
        private readonly float baseAmplitude;
        private readonly float persistence;
        private readonly Vector3 lacunarity;
        private readonly int octaves;

        public FractalNoise(long seed, int octaves, Vector3 baseFrequency, float baseAmplitude, float persistence, Vector3 lacunarity)
        {
            this.noises = Enumerable.Range(0, octaves).Select(octave => new OpenSimplexNoise(seed + octave)).ToArray();
            this.octaves = octaves;
            this.baseFrequency = baseFrequency;
            this.baseAmplitude = baseAmplitude;
            this.persistence = persistence;
            this.lacunarity = lacunarity;
        }

        public float Evaluate(Vector3 pos)
        {
            var frequency = baseFrequency;
            var amplitude = baseAmplitude;
            var noise = 0d;
            var ampSum = 0f;

            for (var o = 0; o < octaves; o++)
            {
                noise += amplitude * noises[0].Evaluate(pos.x * frequency.x, pos.y * frequency.y, pos.z * frequency.z);
                ampSum += amplitude;

                frequency.x *= lacunarity.x;
                frequency.y *= lacunarity.y;
                frequency.z *= lacunarity.z;
                amplitude *= persistence;
            }

            return (float)noise / ampSum;
        }
    }
}