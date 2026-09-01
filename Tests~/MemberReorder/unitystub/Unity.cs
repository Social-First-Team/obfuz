namespace UnityEngine
{
    public class Object { }
    public class Component : Object { }
    public class Behaviour : Component { }
    public class MonoBehaviour : Behaviour { }
    public class ScriptableObject : Object { }
    [System.AttributeUsage(System.AttributeTargets.Method)]
    public class RuntimeInitializeOnLoadMethodAttribute : System.Attribute { }
}
