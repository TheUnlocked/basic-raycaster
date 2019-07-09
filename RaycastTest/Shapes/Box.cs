using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using XNAPlane = Microsoft.Xna.Framework.Plane;

namespace RaycastTest
{
    class Box : IRaycastObject
    {
        public Vector3 position;
        public Vector3 direction;
        public Vector3 size;
        public Material Material { get; }

        private int _rotChecksum;
        private Matrix _rotMatrix;
        private Matrix _invRotMatrix;

        public Box(Vector3 position, Vector3 direction, Vector3 size, Material material)
        {
            this.position = position;
            this.direction = direction;
            this.size = size;
            this.Material = material;

            Vector2 rotYawPitch = direction.ToYawPitch();
            _rotMatrix = Matrix.CreateFromYawPitchRoll(rotYawPitch.X, rotYawPitch.Y, 0);
            _invRotMatrix = Matrix.Invert(_rotMatrix);
            _rotChecksum = GetRotationChecksum();
        }

        public IntersectionData? Intersect(Ray ray)
        {
            if (_rotChecksum != GetRotationChecksum())
            {
                Vector2 rotYawPitch = direction.ToYawPitch();
                _rotMatrix = Matrix.CreateFromYawPitchRoll(rotYawPitch.X, rotYawPitch.Y, 0);
                _invRotMatrix = Matrix.Invert(_rotMatrix);
                _rotChecksum = GetRotationChecksum();
            }

            Ray adjustedRay = new Ray(Vector3.Transform(ray.Position - position, _invRotMatrix), Vector3.TransformNormal(ray.Direction, _invRotMatrix));

            float? dist = new BoundingBox(new Vector3(-size.X / 2, -size.Y / 2, -size.Z / 2), new Vector3(size.X / 2, size.Y / 2, size.Z / 2))
                .Intersects(adjustedRay);

            if (dist is float f)
            {
                Vector3[] verts = new Vector3[]
                {
                    new Vector3(size.X/2, -size.Y/2, size.Z/2), // 1
                    new Vector3(size.X/2, -size.Y/2, -size.Z/2), // 2
                    new Vector3(-size.X/2, -size.Y/2, size.Z/2), // 3
                    new Vector3(-size.X/2, size.Y/2, size.Z/2), // 4
                    new Vector3(size.X/2, size.Y/2, -size.Z/2), // 5
                    new Vector3(-size.X/2, size.Y/2, -size.Z/2) // 6
                };

                Vector3 bestHitNormal = -Vector3.Forward;
                float trueDist = float.PositiveInfinity;

                foreach (var vs in new[] {
                    (1, 2, 0), // Bottom
                    (3, 4, 5), // Top
                    (0, 3, 2), // Left-Front
                    (2, 5, 3), // Left-Back
                    (0, 4, 1), // Right-Front
                    (1, 5, 4), // Right-Back
                })
                {
                    Vector3 vec1 = verts[vs.Item1],
                            vec2 = verts[vs.Item2],
                            vecExtra = verts[vs.Item3];
                    var tFace = new BoundingBox(vec1, vec2);
                    adjustedRay.Intersects(ref tFace, out float? tDist);
                    if (tDist is float f2 && (f2 == f || f == 0))
                    {
                        bestHitNormal = ((vec1 + vec2) / 2).ToNormal();
                        trueDist = MathF.Min(trueDist, f2);
                        break;
                    }
                }

                if (trueDist == float.PositiveInfinity)
                {
                    return null;
                }

                return new IntersectionData
                {
                    collided = this,
                    inside = f == 0,
                    hit = ray.Position + ray.Direction * trueDist,
                    incidentDirection = ray.Direction,
                    normalDirection = (f == 0 ? -1 : 1) * Vector3.TransformNormal(bestHitNormal, _rotMatrix),
                    Distance = trueDist
                };
            }

            return null;
        }

        private int GetRotationChecksum()
        {
            return direction.GetHashCode();
        }
    }
}
