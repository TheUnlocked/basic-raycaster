using Microsoft.Xna.Framework;

namespace RaycastTest
{
    interface IRaycastObject
    {
        Material Material { get; }

        IntersectionData? Intersect(Ray ray);
    }
}