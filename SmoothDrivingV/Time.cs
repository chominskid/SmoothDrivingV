using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmoothDrivingV
{
    public static class Time
    {
        public static float deltaTime;

        private static int length = 200;
        private static float[] buffer = new float[length + 1];

        public static float maxDeltaTime = 0;

        public static void Update(float fps)
        {
            deltaTime = 1.0f / fps;

            for (int i = 0; i < length; i++)
            {
                if (i < length)
                {
                    buffer[i] = buffer[i + 1];
                }
            }

            buffer[length] = deltaTime;
            maxDeltaTime = buffer.Max();
        }
    }
}
