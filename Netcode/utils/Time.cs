using System;

namespace Utils
{
    public class Time
    {
        private static DateTime TimeStart;
        private static DateTime LastFrame;
        private static float m_deltaTime;


        public static float time
        {
            get
            {
                return (float)DateTime.Now.Subtract(TimeStart).TotalSeconds;
            }
        }
        public static float deltaTime
        {
            get
            {
                return m_deltaTime;
            }
        }


        public static void Init()
        {
            TimeStart = DateTime.Now;
            LastFrame = DateTime.Now;
        }
        public static void Update()
        {
            m_deltaTime = (float)DateTime.Now.Subtract(LastFrame).TotalSeconds;
            LastFrame = DateTime.Now;
        }
    }
}
