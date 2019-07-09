using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;

namespace RaycastTest
{
    class Sphere : IRaycastObject
    {
        public Vector3 position;
        public float radius;
        public Material Material { get; }

        public Sphere(Vector3 position, float radius, Material material)
        {
            this.position = position;
            this.radius = radius;
            this.Material = material;
        }

        public IntersectionData? Intersect(Ray ray)
        {
            Vector3 zeroredSpherePosition = position - ray.Position;
            float positionLengthSquared = zeroredSpherePosition.LengthSquared();
            float radiusSquared = radius * radius;
            float distanceAlongRay = Vector3.Dot(ray.Direction, zeroredSpherePosition);

            // If not inside the sphere and not pointing in the same direction as the sphere, this will never intersect.
            if (positionLengthSquared >= radiusSquared && distanceAlongRay < 0)
            {
                return null;
            }

            // Copied from the MonoGame source. ¯\_(ツ)_/¯
            float dist = radiusSquared + distanceAlongRay * distanceAlongRay - positionLengthSquared;
            if (dist < 0) return null;
            dist = distanceAlongRay - MathF.Sqrt(dist);

            // dist is the distance... unless the ray is inside the sphere!
            // Otherwise it's "backwards," so we need to flip it around.
            float trueDistance = dist < 0 ? radius * 2 + dist : dist;

            // Just follow the ray with our calculated distance to get the hit position.
            Vector3 hit = ray.Direction * trueDistance + ray.Position;

            return new IntersectionData
            {
                collided = this,
                inside = dist < 0,
                hit = hit,
                incidentDirection = ray.Direction,
                // On a sphere, the normal will just be a normalized vector from the center to the hit position.
                normalDirection = (hit - position).ToNormal() * MathF.Sign(dist),
                Distance = trueDistance
            };
        }
    }
}
