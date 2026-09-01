using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Fx
{
    public enum Colour { Red, Green, Blue, Alpha, Cyan, Magenta }

    [StructLayout(LayoutKind.Sequential)]
    public class SeqLayout { public int a; public int b; public int c; public int d; }

    [StructLayout(LayoutKind.Explicit)]
    public class ExpLayout
    {
        [FieldOffset(0)] public int a;
        [FieldOffset(4)] public int b;
        [FieldOffset(8)] public int c;
    }

    public struct PlainStruct { public int a; public int b; public int c; public int d; }

    [Serializable]
    public class SerialisableData { public int a; public string b; public float c; public bool d; }

    public class Script : MonoBehaviour
    {
        public int hp; public string title; public float speed; public bool armed;
        public void M1() { } public void M2() { } public void M3() { } public void M4() { }
    }

    public class ScriptChild : Script { public int extra; public string more; public double yet; }

    public class SoData : ScriptableObject { public int x; public string y; public float z; }

    public class BaseVirt
    {
        public virtual int V1() => 1;
        public virtual int V2() => 2;
        public virtual int V3() => 3;
        public int P1() => 1;
        public int P2() => 2;
        public int P3() => 3;
        public int P4() => 4;
    }

    public class DerivedVirt : BaseVirt
    {
        public override int V1() => 11;
        public override int V2() => 22;
        public override int V3() => 33;
        public int Q1() => 1;
        public int Q2() => 2;
        public int Q3() => 3;
        public int Q4() => 4;
    }

    public class BootA { [RuntimeInitializeOnLoadMethod] public static void Boot() { } }

    public class Plain01 { public int f1, f2, f3, f4, f5, f6; public void A(){} public void B(){} public void C(){} public void D(){} public void E(){} public void F(){} }
    public class Plain02 { public int f1, f2, f3, f4, f5, f6; public void A(){} public void B(){} public void C(){} public void D(){} public void E(){} public void F(){} }
    public class Plain03 { public int f1, f2, f3, f4, f5, f6; public void A(){} public void B(){} public void C(){} public void D(){} public void E(){} public void F(){} }
    public class Plain04 { public int f1, f2, f3, f4, f5, f6; public void A(){} public void B(){} public void C(){} public void D(){} public void E(){} public void F(){} }
    public class Plain05 { public int f1, f2, f3, f4, f5, f6; public void A(){} public void B(){} public void C(){} public void D(){} public void E(){} public void F(){} }
    public class Plain06 { public int f1, f2, f3, f4, f5, f6; public void A(){} public void B(){} public void C(){} public void D(){} public void E(){} public void F(){} }
    public class Plain07 { public int f1, f2, f3, f4, f5, f6; public void A(){} public void B(){} public void C(){} public void D(){} public void E(){} public void F(){} }
    public class Plain08 { public int f1, f2, f3, f4, f5, f6; public void A(){} public void B(){} public void C(){} public void D(){} public void E(){} public void F(){} }
    public class Plain09 { public int Prop1 {get;set;} public int Prop2 {get;set;} public int Prop3 {get;set;} public int Prop4 {get;set;} }
    public class Plain10 { public event Action E1; public event Action E2; public event Action E3; public event Action E4; public void Fire(){E1();E2();E3();E4();} }
}
