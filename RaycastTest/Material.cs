using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace RaycastTest
{
    class Material
    {
        public Color Color { get; set; } = Color.White;
        public float Transparency { get; set; } = 0;
        public float Metallicity { get; set; } = 0;
        public float IndexOfRefraction { get; set; } = 1;
        public float SpecularExponent { get; set; } = 20;
        public float DiffuseMultiplier { get; set; } = 0.8f;
        public float SpecularMultiplier { get; set; } = 0.2f;
        /**
         * <summary>Transparent materials should be made non-solid to allow transmitted light.</summary>
         */
        public bool Solid { get; set; } = true;
    }
}
