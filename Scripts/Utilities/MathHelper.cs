using System;

namespace SpaceSim
{
    public static class MathHelper
    {
        public const float Pi = 3.1415926535f;

        public static float DegreesToRadians(float degrees)
        {
            return degrees * Pi / 180.0f;
        }

        public static float RadiansToDegrees(float radians)
        {
            return radians * 180.0f / Pi;
        }
    }
}