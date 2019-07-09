// Can significantly improve performance on multicore systems.
// However, this flag will likely max out your CPU and make it run much hotter.
#define MULTICORE_RENDER

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.IO;
using System.Threading.Tasks;
using XNAPlane = Microsoft.Xna.Framework.Plane;

namespace RaycastTest
{
    public class Game1 : Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        Texture2D canvas;
        Color[] canvasPixels;

        // PREFS
        Vector3 cameraPosition;
        Vector3 cameraRotation = new Vector2(0, 0).ToNormalDirection();
        float fov = MathF.PI / 2;

        // I recommend setting this lower for realtime and higher for single renders.
        const float RENDER_SCALE = 1 / 3f;

        // This is a magic number which reduces artifacts.
        const float bias = 0.0001f;

        static readonly Color BACKGROUND_COLOR = Color.CornflowerBlue;

        // Must be at least 2 for reflections and at least 3 for passing through a transparent medium.
        // The ideal value will depend on how many reflective/transmissive objects are in your scene.
        const int MAX_DEPTH = 8;

        // Render the just the first frame or attempt to render the scene in realtime.
        const bool SINGLE_RENDER = false;

        IRaycastObject[] raycastObjects;
        LightSource[] lightSources;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            canvas = new Texture2D(GraphicsDevice, (int)(GraphicsDevice.Viewport.Width * RENDER_SCALE), (int)(GraphicsDevice.Viewport.Height * RENDER_SCALE));
            canvasPixels = new Color[canvas.Width * canvas.Height];

            Material mirror = new Material() {
                Color = Color.White,
                Metallicity = 1f,
            };

            Material water = new Material()
            {
                Transparency = 1f,
                IndexOfRefraction = 1.33f,
                DiffuseMultiplier = 0,
                SpecularMultiplier = 0,
                Metallicity = 0,
                Solid = false
            };

            Random r = new Random();

            raycastObjects = new IRaycastObject[] {
                new Sphere(new Vector3(2, -0.5f, 10), 3, new Material { Color = Color.Magenta, SpecularExponent = 30, SpecularMultiplier = 0.3f }),
                new Sphere(new Vector3(-1f, 0.5f, 7), 1.5f, new Material { Color = Color.White, SpecularMultiplier = 0 }),

                new Box(new Vector3(1f, 2.5f, 9), new Vector2(4 * MathF.PI / 3, MathF.PI / 6).ToNormalDirection(), Vector3.One * 1.5f, mirror),
                new Box(new Vector3(0.3f, 0.3f, 5), new Vector2(4 * MathF.PI / 3, MathF.PI / 3).ToNormalDirection(), Vector3.One * 0.75f, water),
                new Box(new Vector3(1.5f, -6f, 9f), new Vector2(MathF.PI / 5, -0.1f).ToNormalDirection(), new Vector3(6, 10, 6),
                    new Material() { DiffuseMultiplier = 0.2f, Solid = false, Transparency = 1, IndexOfRefraction = 1.03f }),

                // {X:-0.6000118 Y:-0.3826835 Z:0.7025235}
                //new Sphere(new Vector3(-2.8f, 1.5f, 4), 1f, new Material { SpecularMultiplier = 0.5f, SpecularExponent = 1, Transparency = 1,
                //    Affector = (ref IntersectionData col) => {
                //        //if (r.NextDouble() < 0.5f)
                //        col.hit -= new Vector3(-2.8f, 1.5f, 4);
                //        col.hit *= 10;
                //        col.hit += Vector3.Up * 2.0f + Vector3.Left * 2.5f + Vector3.Forward * 8;
                //        col.incidentDirection = Vector3.Lerp(col.incidentDirection, -Vector3.Forward, 0.8f).ToNormal();
                //    } }),
                //new Plane(new XNAPlane(Vector3.Up, 2f), mirror),
            };
            lightSources = new LightSource[] {
                new LightSource() { position = new Vector3(0, 3, 0), color = Color.White, intensity = 50f },
                new LightSource() { position = new Vector3(-4, -5, 8), color = Color.Blue, intensity = 20f },
                new LightSource() { position = new Vector3(2f, 0, 6f), color = Color.Green, intensity = 2f }
            };

            if (SINGLE_RENDER)
            {
                Task.Run(RenderImage);
            }

            base.Initialize();
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            if (!SINGLE_RENDER)
            {
                // These are just some simple mathematically-driven animations.
                float time = (float)gameTime.TotalGameTime.TotalMilliseconds / 1000;
                (raycastObjects[2] as Box).direction = new Vector2(time / 2, MathF.Cos(time / 5) * MathF.PI / 6).ToNormalDirection();
                (raycastObjects[3] as Box).direction = new Vector2(-time / 2, -time / 3).ToNormalDirection();
                (raycastObjects[3] as Box).position.Y = 0.6f + (0.4f * MathF.Sin(time / 2));
            }
            
            base.Update(gameTime);
        }


        //int imgNumber = 0;
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(BACKGROUND_COLOR);

            if (!SINGLE_RENDER)
            {
                RenderImage();
                //canvas.SetData(canvasPixels);
                //using (var fs = File.OpenWrite($"frame_{imgNumber.ToString("D4")}.png"))
                //    canvas.SaveAsPng(fs, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
                //if (++imgNumber == 200)
                //{
                //    Exit();
                //}
            }

            lock (canvasPixels)
            {
                canvas.SetData(canvasPixels);
                spriteBatch.Begin(/*SpriteSortMode.Immediate, rasterizerState: rasterizerState*/);
                spriteBatch.Draw(canvas, GraphicsDevice.Viewport.Bounds, Color.White);
                spriteBatch.End();

                base.Draw(gameTime);
            }
        }

        float slowMaxLuma = 0;
        void RenderImage()
        {
            Vector3[] naivePass = new Vector3[canvasPixels.Length];
            float maxLuma = 1f;

            // Get the raw colors as vector3s. Color adjustment will be handled in later passes.
#if MULTICORE_RENDER
            // Parallel.For is used for multicore computation.
            Parallel.For(0, canvas.Width, x =>
            {
                Parallel.For(0, canvas.Height, y =>
                {
#else
            for (int x = 0; x < canvas.Width; x++)
            {
                for (int y = 0; y < canvas.Height; y++)
                {
#endif
                    float yaw = (fov / canvas.Width) * (x - canvas.Width / 2);
                    float pitch = (fov / canvas.Width) * (y - canvas.Height / 2);
                    Vector3 rayColor = Raycast(new Ray(
                            cameraPosition,
                            (new Vector2(yaw, pitch) + cameraRotation.ToYawPitch()).ToNormalDirection()
                        ), MAX_DEPTH);
                    naivePass[x + (/* Flip the camera so that up is positive Y */ canvas.Height - y - 1) * canvas.Width] = rayColor;

                    // This is only necessary on a single render, though a branching statement would probably be more expensive than this.
                    // Either way, the optimization from removing this line is minimal at best.
                    canvasPixels[x + (canvas.Height - y - 1) * canvas.Width] = new Color(rayColor);

                    maxLuma = MathF.Max(maxLuma, rayColor.X * 0.229f + rayColor.Y * 0.587f + rayColor.Z * 0.114f);
#if MULTICORE_RENDER
                });
            });
#else
                }
            }
#endif

            maxLuma *= 1.06f;
            if (slowMaxLuma == 0)
                slowMaxLuma = maxLuma;
            else
                slowMaxLuma = MathHelper.Lerp(slowMaxLuma, maxLuma, 0.01f);

            // Adjust everything to the max luma value. A bunch of magic numbers here.
            // I don't actually know if this is how you're supposed to scale down lumosity, but it looks fine enough.
            float redAdjust = slowMaxLuma * 0.229f;
            float greenAdjust = slowMaxLuma * 0.587f;
            float blueAdjust = slowMaxLuma * 0.114f;
            for (int i = 0; i < naivePass.Length; i++)
            {
                Vector3 rawColor = naivePass[i];
                Vector3 adjustedColor = new Vector3(rawColor.X * .229f / redAdjust, rawColor.Y * .587f / greenAdjust, rawColor.Z * .114f / blueAdjust);
                canvasPixels[i] = new Color(adjustedColor);
            }
        }

        Vector3 Raycast(Ray ray, int maxDepth, float currentIOR = 1)
        {
            if (maxDepth <= 0)
            {
                return BACKGROUND_COLOR.ToVector3();
            }

            // Get closest raycast object
            IntersectionData? closestCollision = null;
            foreach (IRaycastObject obj in raycastObjects)
            {
                var col = obj.Intersect(ray);
                if (col == null) continue;
                if (closestCollision == null || col?.DistanceSquared < closestCollision?.DistanceSquared) closestCollision = col;
            }
            if (closestCollision == null)
            {
                return BACKGROUND_COLOR.ToVector3();
            }

            Vector3 diffuseColor = Vector3.Zero;
            Vector3 specularColor = Vector3.Zero;
            Vector3 reflectedColor = Vector3.Zero;
            Vector3 refractedColor = Vector3.Zero;

            IntersectionData collision = closestCollision.Value;
            collision.collided.Material.Affector(ref collision);
            Vector3 hitNormal = collision.normalDirection;
            IRaycastObject hitObject = collision.collided;
            Material mat = hitObject.Material;
            Vector3 reflectOrigin = collision.hit + (collision.normalDirection * bias);

            Ray reflectRay = new Ray(reflectOrigin, hitNormal);
            float fresnel = MathHelper.Lerp(VectorHelper.FresnelFactor(collision.incidentDirection, hitNormal, currentIOR, mat.IndexOfRefraction), 1, mat.Metallicity);
            if (fresnel > 0)
                reflectedColor += Raycast(reflectRay, maxDepth - 1, currentIOR) * fresnel;
            if (mat.Transparency > 0)
            {
                Vector3 refractDirection = collision.GetRefractiveDirection(currentIOR);
                Ray transmissionRay = new Ray(collision.hit + (refractDirection * bias), refractDirection);
                refractedColor += Raycast(transmissionRay, maxDepth - 1, mat.IndexOfRefraction) * (1-fresnel) * mat.Transparency;
            }

            // Hit each light source, if not blocked.
            foreach (LightSource light in lightSources)
            {
                float distanceSquared = (light.position - collision.hit).LengthSquared();
                Vector3 lightDirection = (light.position - collision.hit).ToNormal();
                // Bias avoids objects occluding themselves. Floating-point error stuff.
                Ray lightRay = new Ray(reflectOrigin, lightDirection);

                // Check for occluding objects
                float transparencyMultiplier = 1f;
                foreach (IRaycastObject obj in raycastObjects)
                {
                    var col = obj.Intersect(lightRay);
                    if (col != null && col?.DistanceSquared < distanceSquared)
                    {
                        transparencyMultiplier *= col.Value.collided.Material.Transparency;
                        if (transparencyMultiplier <= 0)
                            goto LightEnd;
                    }
                }

                float lDotN = MathF.Max(0f, Vector3.Dot(lightDirection, hitNormal));
                diffuseColor += light.color.ToVector3() * light.intensity * lDotN * mat.Color.ToVector3() / distanceSquared * mat.DiffuseMultiplier;
                specularColor += MathF.Pow(MathF.Max(0, -Vector3.Dot(-lightDirection - (2 * Vector3.Dot(-lightDirection, hitNormal) * hitNormal), ray.Direction)), mat.SpecularExponent) * light.color.ToVector3() * mat.SpecularMultiplier;

            LightEnd:
                ;
            }
            return diffuseColor + specularColor + reflectedColor + refractedColor;
        }
    }
}
