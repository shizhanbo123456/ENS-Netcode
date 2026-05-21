namespace Utils
{
    public class ReachTime
    {
        public bool Reached
        {
            get
            {
                return Time.time >= targetTime;
            }
        }
        private float targetTime;
        public enum InitTimeFlagType
        {
            ReachAt,ReachAfter
        }
        public ReachTime() { }
        public ReachTime(float time,InitTimeFlagType flagtype)
        {
            switch (flagtype)
            {
                case InitTimeFlagType.ReachAt: ReachAt(time); break;
                case InitTimeFlagType.ReachAfter:ReachAfter(time);break;
            }
        }
        public void ReachAt(float time)
        {
            targetTime = time;
        }
        public void ReachAfter(float time)
        {
            targetTime = Time.time+time;
        }
    }
}