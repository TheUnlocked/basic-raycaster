using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace RaycastTest
{
    public static class VectorExtensions
    {
        public static Vector3 ToNormal(this Vector3 vec)
        {
            var newVec = vec;
            newVec.Normalize();
            return newVec;
        }

        public static Vector2 ToYawPitch(this Vector3 vec)
        {
            return new Vector2(MathF.Atan2(vec.X, vec.Z), MathF.Asin(vec.Y));
        }

        public static Vector3 ToNormalDirection(this Vector2 vec)
        {
            return new Vector3(
                MathF.Sin(vec.X) * MathF.Cos(vec.Y),
                MathF.Sin(vec.Y),
                MathF.Cos(vec.X) * MathF.Cos(vec.Y));
        }

        public static float MaxComponent(this Vector3 vec)
        {
            return Math.Max(vec.X, Math.Max(vec.Y, vec.Z));
        }

        public static Vector3 Apply(this Vector3 vec, Func<float, float> f)
        {
            return new Vector3(f(vec.X), f(vec.Y), f(vec.Z));
        }

        public static Vector2 ToVector2(this Vector3 vec)
        {
            return new Vector2(vec.X, vec.Y);
        }
    }
    public static class VectorHelper
    {
        public static float FresnelFactor(Vector3 incident, Vector3 normal, float currentIOR, float materialIOR)
        {
            // This function is pure magic.

            float cosi = MathHelper.Clamp(Vector3.Dot(incident, normal), -1, 1);
            float etai;
            float etat;
            if (cosi > 0)
            {
                etai = materialIOR;
                etat = currentIOR;
            }
            else
            {
                etai = currentIOR;
                etat = materialIOR;
            }
            float sint = etai / etat * MathF.Sqrt(MathF.Max(0, 1 - cosi * cosi));

            if (sint >= 1) return 1;

            float cost = MathF.Sqrt(MathF.Max(0f, 1 - sint * sint));
            cosi = MathF.Abs(cosi);
            float Rs = ((etat * cosi) - (etai * cost)) / ((etat * cosi) + (etai * cost));
            float Rp = ((etai * cosi) - (etat * cost)) / ((etai * cosi) + (etat * cost));
            return (Rs * Rs + Rp * Rp) / 2;
        }
    }
}
