using Microsoft.Xna.Framework;
using System;

namespace RaycastTest
{
    struct IntersectionData
    {
        public IRaycastObject collided;
        public bool inside;
        public Vector3 hit;
        public Vector3 incidentDirection;
        public Vector3 normalDirection;
        private float _distance;
        public float Distance {
            get
            {
                if (inside && collided.Material.Solid)
                    return 0;
                return _distance;
            }
            set
            {
                _distance = value;
            }
        }
        public float DistanceSquared { get => Distance * Distance; }
        private Vector3 _reflectiveDirection;
        public Vector3 ReflectiveDirection { get
            {
                if (_reflectiveDirection == Vector3.Zero)
                {
                    _reflectiveDirection = Vector3.Reflect(incidentDirection, normalDirection);
                }
                return _reflectiveDirection;
            }
        }
        public Vector3 GetRefractiveDirection(float currentIOR = 1) {
            // A lot of this is magic that I don't understand.
            float cosi = MathHelper.Clamp(Vector3.Dot(incidentDirection, normalDirection), -1, 1);
            Vector3 normal = normalDirection;
            float eta;
            if (cosi < 0)
            {
                cosi *= -1;
                eta = currentIOR / collided.Material.IndexOfRefraction;
            }
            else
            {
                normal *= -1;
                eta = collided.Material.IndexOfRefraction / currentIOR;
            }
            float k = 1 - eta * eta * (1 - cosi * cosi);
            return k < 0
                ? /* Total internal reflection */ ReflectiveDirection
                : incidentDirection * eta + (eta * cosi - MathF.Sqrt(k)) * normal;
        }
    }
}