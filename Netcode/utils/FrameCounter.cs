namespace Utils
{
    public static class FrameCounter
    {
        private static float timeCounter = -1f;
        public static int count = 0;
        public static void Update()
        {
            if (timeCounter < 0) timeCounter = Time.time;
            else if (Time.time - timeCounter < 1) count++;
            else
            {
                EnsInstance.frameRate = count;
                //Debug.Log("frame=" + count);
                count = 0;
                timeCounter = Time.time;
            }
        }
    }
}