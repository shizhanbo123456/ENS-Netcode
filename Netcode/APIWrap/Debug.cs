using System;

namespace Utils
{
    public static class Debug
    {
        public static void Log(string msg)
        {
#if UNITY_EDITOR
            UnityEngine.Debug.Log(msg);
#elif !UNITY_2017_1_OR_NEWER
            Console.WriteLine("Log:"+msg);
#endif
        }
        public static void LogWarning(string msg)
        {
#if UNITY_EDITOR
            UnityEngine.Debug.LogWarning(msg);
#elif !UNITY_2017_1_OR_NEWER
            Console.WriteLine("Log:" + msg);
#endif
        }
        public static void LogError(string msg)
        {
#if UNITY_EDITOR
            UnityEngine.Debug.LogError(msg);
#elif !UNITY_2017_1_OR_NEWER
            Console.WriteLine("Log:" + msg);
#endif
        }
        private static System.Text.StringBuilder sb=new System.Text.StringBuilder();
        public static void PrintBytes(byte[] bytes,Segment segment)
        {
            for(int i = segment.StartIndex;i<segment.StartIndex+segment.Length;i++)sb.Append(bytes[i]).Append(' ');
#if UNITY_EDITOR
            UnityEngine.Debug.Log(sb.ToString());
#elif !UNITY_2017_1_OR_NEWER
            Console.WriteLine(sb.ToString());
#endif
            sb.Clear();
        }
        public static void PrintBytes(byte[] bytes)
        {
            for (int i = 0; i < bytes.Length; i++) sb.Append(bytes[i]).Append(' ');
#if UNITY_EDITOR
            UnityEngine.Debug.Log(sb.ToString());
#elif !UNITY_2017_1_OR_NEWER
            Console.WriteLine(sb.ToString());
#endif
            sb.Clear();
        }
        public static void ErrorCaught(System.Exception ex)
        {
#if UNITY_EDITOR
            UnityEngine.Debug.LogException(ex);
#elif !UNITY_2017_1_OR_NEWER
            Console.WriteLine(sb.ToString());
#endif
        }
    }
}