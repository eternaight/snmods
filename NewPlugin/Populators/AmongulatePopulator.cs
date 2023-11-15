using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NewPlugin.ProcGen;
using NewPlugin.WorldgenAPI;
using UnityEngine;

namespace NewPlugin
{
    public class AmongulatePopulator : IPopulator
    {
        private readonly AmongulateDensityField[] densities;

        private float maxDist = float.NegativeInfinity;
        private float minDist = float.PositiveInfinity;

        public AmongulatePopulator()
        {
            const string PATH_A = "C:\\Users\\eterna_dark\\Downloads\\among_a.stl";
            const string PATH_B = "C:\\Users\\eterna_dark\\Downloads\\among_b.stl";
            const string PATH_C = "C:\\Users\\eterna_dark\\Downloads\\among_c.stl";
            
            var tris_a = ReadSTLTris(PATH_A);
            var tris_b = ReadSTLTris(PATH_B);
            var tris_c = ReadSTLTris(PATH_C);

            densities = new AmongulateDensityField[] {
                new AmongulateDensityField(new Int3(50, 62, 74), new Vector3(128, 80, 128), tris_a, 19),
                new AmongulateDensityField(new Int3(50, 62, 74), new Vector3(128, 80, 128), tris_b, 6),
                new AmongulateDensityField(new Int3(50, 62, 74), new Vector3(128, 80, 128), tris_c, 5),
            };
        }

        public static Vector3[][] ReadSTLTris(string path)
        {
            var log = Plugin.logSource;

            using var stream = File.OpenRead(path);
            stream.Seek(80, SeekOrigin.Begin);
            using var reader = new BinaryReader(stream, Encoding.ASCII, true);
            
            var facetNumber = reader.ReadUInt32();
            log.LogInfo($"Number of facets: {facetNumber}");

            var triangles = new List<Vector3[]>();

            for (int i = 0; i < facetNumber; i++) 
            {
                triangles.Add(new Vector3[] {
                    reader.ReadVector3(),
                    reader.ReadVector3(),
                    reader.ReadVector3(),
                    reader.ReadVector3()
                });

                for (int j = 0; j < 4; j++)
                {
                    // swizzle XZY
                    triangles[i][j] = new Vector3(triangles[i][j].x, triangles[i][j].z, triangles[i][j].y);
                }

                var attributeByteCount = reader.ReadUInt16();
                if (attributeByteCount > 0) {
                    log.LogInfo("non-zero attribute byte count");
                    reader.BaseStream.Seek(attributeByteCount, SeekOrigin.Current);
                }
            }

            return triangles.ToArray();
        }

        public VoxelPayload Populate(Int3 voxel)
        {
            var pos = voxel.ToVector3() + Vector3.one * 0.5f;
            // Max() cause we're Unioning them
            var payload = new VoxelPayload(float.NegativeInfinity, 1);
            foreach (var item in densities)
            {
                payload.Union(item.Evaluate(pos), item.solidType);
            }
            
            minDist = Mathf.Min(payload.signedDistance, minDist);
            maxDist = Mathf.Max(payload.signedDistance, maxDist);

            return payload;
        }

        public void Sprinkle(BuildBlueprint_TreeEverything build)
        {
            Plugin.logSource.LogInfo($"minDist: {minDist}");
            Plugin.logSource.LogInfo($"maxDist: {maxDist}");
        }

        private class AmongulateDensityField : ISignedDistance
        {
            public readonly Int3.Bounds scaledBounds;
            private readonly Vector3 origin;
            public List<SignedDistanceThickTriangle> tris = new();
            public readonly int solidType;

            public AmongulateDensityField(Int3 voxelSize, Vector3 origin, Vector3[][] triangles, int solidType) {
                
                // bake distance or sth
                var extentsMin = triangles[0][1];
                var extentsMax = triangles[0][1];

                // calc extents
                for (int i = 0; i < triangles.Length; i++)
                {
                    for (int j = 1; j < 4; j++)
                    {
                        extentsMin = Vector3.Min(extentsMin, triangles[i][j]);
                        extentsMax = Vector3.Max(extentsMax, triangles[i][j]);
                    }
                }

                var extents = extentsMax - extentsMin;
                var mod = new Vector3(voxelSize.x / extents.x, voxelSize.y / extents.y, voxelSize.z / extents.z);
                scaledBounds = new Int3.Bounds
                (
                    Int3.Floor(Vector3.Scale(extentsMin, mod)), 
                    Int3.Ceil(Vector3.Scale(extentsMax, mod))
                );

                // add planes
                for (int i = 0; i < triangles.Length; i++)
                {
                    tris.Add(new SignedDistanceThickTriangle(
                        Vector3.Scale(triangles[i][1], mod), 
                        Vector3.Scale(triangles[i][2], mod), 
                        Vector3.Scale(triangles[i][3], mod),
                        triangles[i][0],
                        .75f
                    ));
                }

                this.origin = origin;
                this.solidType = solidType;
            }

            public float Evaluate(Vector3 p) {

                var localPoint = p - origin;

                if (!scaledBounds.Contains(Int3.Floor(localPoint)))
                {
                    return float.NegativeInfinity;
                }

                return tris.Select(tri => tri.Evaluate(localPoint)).Max();
            }
        }
    }
}