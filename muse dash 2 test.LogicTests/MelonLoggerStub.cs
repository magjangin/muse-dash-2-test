namespace MelonLoader
{
    public static class MelonLogger
    {
        public static void Msg(string message) { }
        public static void Msg(object message) { }
        public static void Warning(string message) { }
        public static void Warning(object message) { }
        public static void Error(string message) { }
        public static void Error(object message) { }
    }
}

namespace muse_dash_2_test
{
    public static class ModLogger
    {
        public static void Msg(string message) { }
        public static void Msg(string tag, string message) { }
        public static void Warning(string message) { }
        public static void Error(string message) { }
        public static void Error(string message, System.Exception ex) { }
        public static void Verbose(string message) { }
        public static void LogAlways(string message) { }
    }
}
