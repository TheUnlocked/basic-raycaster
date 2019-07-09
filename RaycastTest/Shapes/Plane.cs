using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using XNAPlane = Microsoft.Xna.Framework.Plane;

namespace RaycastTest
{
    class Plane : IRaycastObject
    {
        public XNAPlane plane;
        public Material Material { get; }

        public Plane(XNAPlane plane, Material material)
        {
            this.plane = plane;
            this.Material = material;
        }

        public IntersectionData? Intersect(Ray ray)
        {
            float? dist = ray.Intersects(plane);

            if (!dist.HasValue) return null;

            return new IntersectionData
            {
                collided = this,
                // "Inside" meaning "behind the plane"
                inside = Vector3.Dot(ray.Direction, plane.Normal) > 0,
                hit = ray.Position + (dist.Value * ray.Direction),
                incidentDirection = ray.Direction,
                normalDirection = plane.Normal,
                Distance = dist.Value
            };
        }
    }
}
