using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NewPlugin.ProcGen
{
    public interface ISignedDistance
    {
        public float Evaluate(Vector3 p);
    }

    public class SignedDistanceBox : ISignedDistance
    {
        private readonly SignedDistancePlane[] sides;

        public SignedDistanceBox(Vector3 center, Vector3 basis_a, Vector3 basis_b, Vector3 basis_c) 
        {
            var points = new Vector3[] 
            {
                center - basis_a - basis_b - basis_c,
                center - basis_a - basis_b + basis_c,
                center - basis_a + basis_b - basis_c,
                center - basis_a + basis_b + basis_c,
                center + basis_a - basis_b - basis_c,
                center + basis_a - basis_b + basis_c,
                center + basis_a + basis_b - basis_c,
                center + basis_a + basis_b + basis_c
            };

            sides = new SignedDistancePlane[] 
            {
                new SignedDistancePlane(points[0], points[2], points[1]),
                new SignedDistancePlane(points[4], points[5], points[6]),
                new SignedDistancePlane(points[0], points[1], points[4]),
                new SignedDistancePlane(points[2], points[6], points[3]),
                new SignedDistancePlane(points[1], points[3], points[5]),
                new SignedDistancePlane(points[0], points[4], points[2]),
            };
        }

        public float Evaluate(Vector3 p) => sides.Select(side => side.Evaluate(p)).Min();
    }
    public class SignedDistancePlane : ISignedDistance
    {
        private readonly Vector3 normal;
        private readonly float D;

        public SignedDistancePlane(Vector3 normal, float D) 
        {
            this.normal = normal;
            this.D = D;
        }
        public SignedDistancePlane(Vector3 a, Vector3 b, Vector3 c) 
        {
            var u = b - a;
            var v = c - a;
            this.normal = Vector3.Cross(u, v).normalized;
            this.D = -Vector3.Dot(normal, a);
        }

        public float Evaluate(Vector3 p) => Vector3.Dot(p, normal) + D;
    }

    public class SignedDistanceNoise : ISignedDistance
    {
        private readonly ISignedDistance baseFunction;
        private readonly FractalNoise noise;

        public SignedDistanceNoise(ISignedDistance baseFunction, FractalNoise noise)
        {
            this.baseFunction = baseFunction;
            this.noise = noise;
        }

        public float Evaluate(Vector3 p)
        {
            return baseFunction.Evaluate(p) + noise.Evaluate(p);
        }
    }

    public class SignedDistanceSphere : ISignedDistance
    {
        private readonly Vector3 origin;
        private readonly float radius;

        public SignedDistanceSphere(Vector3 origin, float radius)
        {
            this.origin = origin;
            this.radius = radius;
        }

        public float Evaluate(Vector3 p)
        {
            return radius - (p - origin).magnitude;
        }
    }

    public class SignedDistanceThickTriangle
    {
        public readonly Vector3 a;
        public readonly Vector3 b;
        public readonly Vector3 c;
        public readonly Vector3 normal;
        private readonly float thickness;
        private readonly Vector3 planeABnormal;
        private readonly Vector3 planeBCnormal;
        private readonly Vector3 planeCAnormal;

        public SignedDistanceThickTriangle(Vector3 a, Vector3 b, Vector3 c, Vector3 normal, float thickness)
        {
            var generatedNormal = Vector3.Cross(a - b, a - c).normalized;
            if (Vector3.Dot(normal, generatedNormal) > 0)
            {
                this.a = a;
                this.b = b;
                this.c = c;
            } else
            {
                this.a = a;
                this.b = c;
                this.c = b;
            }

            this.normal = normal;
            this.thickness = thickness;
            this.planeABnormal = Vector3.Cross(normal, this.b - this.a);
            this.planeBCnormal = Vector3.Cross(normal, this.c - this.b);
            this.planeCAnormal = Vector3.Cross(normal, this.a - this.c);
        }


        // thank you bradgonesurfing 
        // https://stackoverflow.com/questions/2924795/fastest-way-to-compute-point-to-triangle-distance-in-3d
        public Vector3 ClosestPointTo(Vector3 p)
        {
            // Find the projection of the point onto the edge

            var uab = ProjectOnEdge( p, a, b );
            var uca = ProjectOnEdge( p, c, a );

            if (uca > 1 && uab < 0)
                return a;

            var ubc = ProjectOnEdge( p, b, c );

            if (uab > 1 && ubc < 0)
                return b;

            if (ubc > 1 && uca < 0)
                return c;

            if ((uab >= 0 && uab <= 1) && (Vector3.Dot(planeABnormal, p - a) <= 0))
                return Vector3.Lerp( a, b, uab );

            if ((ubc >= 0 && ubc <= 1) && (Vector3.Dot(planeBCnormal, p - b) <= 0))
                return Vector3.Lerp( b, c, uab );

            if ((uca >= 0 && uca <= 1) && (Vector3.Dot(planeCAnormal, p - c) <= 0))
                return Vector3.Lerp( c, a, uab );

            // The closest point is in the triangle so 
            // project to the plane to find it
            return p - normal * Vector3.Dot(normal, p - a);
        }

        private static float ProjectOnEdge(Vector3 p, Vector3 a, Vector3 b) 
        {
            return Vector3.Dot(p - a, b - a) / (b - a).sqrMagnitude;
        }

        public float Evaluate(Vector3 p)
        {
            return thickness - Mathf.Abs(Vector3.Distance(p, ClosestPointTo(p)));
        }
    }

    public class SignedDistanceUnion : ISignedDistance
    {
        public List<ISignedDistance> members = new();

        public float Evaluate(Vector3 p)
        {
            return members.Select(m => m.Evaluate(p)).Max();
        }
    }
    public class SignedDistanceIntersection : ISignedDistance
    {
        public List<ISignedDistance> members = new();

        public float Evaluate(Vector3 p)
        {
            return members.Select(m => m.Evaluate(p)).Min();
        }
    }

    public class SignedDistanceCappedCone : ISignedDistance
    {
        private readonly Vector3 a;
        private readonly Vector3 b;
        private readonly float r_a;
        private readonly float r_b;

        public SignedDistanceCappedCone(Vector3 a, Vector3 b, float r_a, float r_b)
        {
            this.a = a;
            this.b = b;
            this.r_a = r_a;
            this.r_b = r_b;
        }

        public float Evaluate(Vector3 p)
        {
            float rba  = r_b - r_a;
            float baba = Vector3.Dot(b - a, b - a);
            float papa = Vector3.Dot(p-a,p-a);
            float paba = Vector3.Dot(p-a,b-a)/baba;
            float x = Mathf.Sqrt( papa - paba*paba*baba );
            float cax = Mathf.Max(0, x-((paba<0.5)?r_a:r_b));
            float cay = Mathf.Abs(paba-0.5f)-0.5f;
            float k = rba*rba + baba;
            float f = Mathf.Clamp01( (rba*(x-r_a)+paba*baba)/k);
            float cbx = x-r_a - f*rba;
            float cby = paba - f;
            float s = (cbx<0.0 && cay<0.0) ? 1.0f : -1.0f;
            return s * Mathf.Sqrt( Mathf.Min(cax*cax + cay*cay*baba, cbx*cbx + cby*cby*baba) );
        }
    }
}