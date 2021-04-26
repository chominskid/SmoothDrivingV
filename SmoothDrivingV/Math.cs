using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GTA;
using GTA.Math;

namespace SmoothDrivingV
{
    public static class MathExt
    {
        public const float pi = 3.1415926535897932384626433832795f;
        public const float deg2Rad = 0.0174532925199f;

        public static float GetForwardSpeed(this Entity entity)
        {
            Vector3 velocity = entity.Velocity;
            Quaternion rotation = entity.Quaternion;
            rotation.Invert();

            return (rotation * velocity).Y;
        }

        public static float Abs(float input)
        {
            if (input < 0.0f)
            {
                return -input;
            }

            return input;
        }

        public static float Clamp(float input, float min, float max)
        {
            if (input > max)
            {
                return max;
            }
            if (input < min)
            {
                return min;
            }

            return input;
        }

        public static int Clamp(int input, int min, int max)
        {
            if (input > max)
            {
                return max;
            }
            if (input < min)
            {
                return min;
            }

            return input;
        }

        public static float Sin(float x)
        {
            return (float)Math.Sin(x * deg2Rad);
        }

        public static float Cos(float x)
        {
            return (float)Math.Cos(x * deg2Rad);
        }

        public static float Tan(float x)
        {
            return (float)Math.Tan(x * deg2Rad);
        }
    }
}
