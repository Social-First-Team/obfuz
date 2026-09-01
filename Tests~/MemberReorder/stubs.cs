namespace UnityEngine.Assertions {
    public static class Assert {
        public static void IsTrue(bool c) { if(!c) throw new System.Exception("assert"); }
        public static void IsTrue(bool c, string m) { if(!c) throw new System.Exception(m); }
        public static void IsNotNull(object o) { if(o==null) throw new System.Exception("null"); }
    }
}
namespace UnityEngine {
    public static class Debug {
        public static void Log(object o) { System.Console.WriteLine(o); }
        public static void LogWarning(object o) { System.Console.WriteLine(o); }
        public static void LogError(object o) { System.Console.WriteLine(o); }
    }
}
