// Copyright 2025 Code Philosophy
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using dnlib.DotNet;
using Obfuz.ObfusPasses.SymbolObfus;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

class AllowAll : IObfuscationPolicy
{
    public bool NeedRename(TypeDef t) => t.Name != "Plain08";
    public bool NeedRename(MethodDef m) => m.Name != "C";
    public bool NeedRename(FieldDef f) => f.Name != "f3";
    public bool NeedRename(PropertyDef p) => p.Name != "Prop2";
    public bool NeedRename(EventDef e) => e.Name != "E2";
}

static class Program
{
    static int failures;
    static void Check(bool ok, string what)
    {
        Console.WriteLine((ok ? "PASS  " : "FAIL  ") + what);
        if (!ok) failures++;
    }

    static ModuleDefMD Load(string path)
    {
        var ctx = ModuleDef.CreateModuleContext();
        ((AssemblyResolver)ctx.AssemblyResolver).EnableTypeDefCache = true;
        ((AssemblyResolver)ctx.AssemblyResolver).DefaultModuleContext = ctx;
        var res = (AssemblyResolver)ctx.AssemblyResolver;
        res.PreSearchPaths.Add(Path.GetDirectoryName(Path.GetFullPath(path)));
        res.PreSearchPaths.Add(Path.GetDirectoryName(typeof(object).Assembly.Location));
        res.PostSearchPaths.Add(Path.GetDirectoryName(typeof(object).Assembly.Location));
        return ModuleDefMD.Load(path, ctx);
    }

    static Dictionary<string, List<string>> Snapshot(ModuleDefMD mod)
    {
        var d = new Dictionary<string, List<string>>();
        d["#types"] = mod.Types.Select(t => t.FullName).ToList();
        foreach (var t in mod.GetTypes())
        {
            d[t.FullName + "#f"] = t.Fields.Select(f => f.Name.String).ToList();
            d[t.FullName + "#m"] = t.Methods.Select(m => m.Name.String).ToList();
            d[t.FullName + "#p"] = t.Properties.Select(p => p.Name.String).ToList();
            d[t.FullName + "#e"] = t.Events.Select(e => e.Name.String).ToList();
        }
        return d;
    }

    static bool Same(List<string> a, List<string> b) => a.SequenceEqual(b);

    static readonly HashSet<string> Pinned = new HashSet<string> { "Plain08", "C", "f3", "Prop2", "E2", "V1", "V2", "V3", "<Module>", "Boot" };

    static bool IsRenamable(string key, string name)
    {
        if (Pinned.Contains(name) || name.StartsWith(".")) return false;
        if (key.StartsWith("Fx.Colour") || key.StartsWith("Fx.SeqLayout#f") || key.StartsWith("Fx.ExpLayout#f")
            || key.StartsWith("Fx.PlainStruct#f") || key.StartsWith("Fx.SerialisableData#f")
            || key.StartsWith("Fx.Script#f") || key.StartsWith("Fx.ScriptChild#f") || key.StartsWith("Fx.SoData#f")) return false;
        return true;
    }

    static int Main()
    {
        string dll = "fixture/bin/Debug/netstandard2.0/fixture.dll";
        var before = Snapshot(Load(dll));

        var mod1 = Load(dll);
        mod1.EnableTypeDefFindCache = true;
        var primed = mod1.Find("Fx.Plain01", false);
        var primedEnumerator = mod1.GetTypes().GetEnumerator();
        primedEnumerator.MoveNext();
        new MemberReorder(1234, new AllowAll()).Process(new List<ModuleDef> { mod1 });
        Check(primed != null && mod1.Find("Fx.Plain01", false) != null
            && mod1.Find("Fx2.PlainHost", false) != null, "TypeDef find cache still resolves after reordering");
        var after1 = Snapshot(mod1);

        var mod2 = Load(dll);
        new MemberReorder(9999, new AllowAll()).Process(new List<ModuleDef> { mod2 });
        var after2 = Snapshot(mod2);

        foreach (var key in new[] {
            "Fx.Colour#f",            "Fx.SeqLayout#f",            "Fx.ExpLayout#f",            "Fx.PlainStruct#f",            "Fx.SerialisableData#f",            "Fx.Script#f",            "Fx.ScriptChild#f",            "Fx.SoData#f",        })
        {
            Check(Same(before[key], after1[key]), "field order pinned: " + key);
        }

        foreach (var t in new[] { "Fx.BaseVirt", "Fx.DerivedVirt" })
        {
            var b = before[t + "#m"];
            var a = after1[t + "#m"];
            bool ok = true;
            for (int i = 0; i < b.Count; i++)
            {
                if (b[i].StartsWith("V") && a[i] != b[i]) ok = false;
            }
            Check(ok, "virtual slots pinned: " + t);
        }

        Check(before["Fx.Plain01#f"].IndexOf("f3") == after1["Fx.Plain01#f"].IndexOf("f3"), "policy-pinned field index held");
        Check(before["Fx.Plain01#m"].IndexOf("C") == after1["Fx.Plain01#m"].IndexOf("C"), "policy-pinned method index held");
        Check(before["Fx.Plain09#p"].IndexOf("Prop2") == after1["Fx.Plain09#p"].IndexOf("Prop2"), "policy-pinned property index held");
        Check(before["Fx.Plain10#e"].IndexOf("E2") == after1["Fx.Plain10#e"].IndexOf("E2"), "policy-pinned event index held");
        Check(before["#types"].IndexOf("Fx.Plain08") == after1["#types"].IndexOf("Fx.Plain08"), "policy-pinned type index held");
        Check(before["#types"].IndexOf("Fx.BootA") == after1["#types"].IndexOf("Fx.BootA"), "RuntimeInitializeOnLoadMethod type index held");
        Check(after1["#types"][0] == "<Module>", "<Module> stays at index 0");

        bool memberOk = before.Keys.All(k => after1.ContainsKey(k)
            && before[k].OrderBy(x => x, StringComparer.Ordinal).SequenceEqual(after1[k].OrderBy(x => x, StringComparer.Ordinal)));
        Check(memberOk, "no member lost or duplicated");

        Check(!Same(before["#types"], after1["#types"]), "type order changed vs original");
        Check(!Same(after1["#types"], after2["#types"]), "type order differs between seeds");
        int movedFields = before["Fx.Plain01#f"].Where((n, i) => after1["Fx.Plain01#f"][i] != n).Count();
        Check(movedFields > 0, "plain class fields reordered");
        int movedMethods = before["Fx.Plain02#m"].Where((n, i) => after1["Fx.Plain02#m"][i] != n).Count();
        Check(movedMethods > 0, "plain class methods reordered");

        var outPath = "reordered.dll";
        mod1.Write(outPath);
        var reloaded = Load(outPath);
        Check(Same(after1["#types"], reloaded.Types.Select(t => t.FullName).ToList()), "written module preserves new type order");
        Check(reloaded.GetTypes().Count() == mod1.GetTypes().Count(), "written module keeps every type");
        var rd = Snapshot(reloaded);
        Check(rd.Keys.All(k => Same(after1[k], rd[k])), "written module preserves every member order");
        var owned = reloaded.GetTypes().All(t => t.Module != null
            && t.Fields.All(f => f.DeclaringType == t) && t.Methods.All(m => m.DeclaringType == t));
        Check(owned, "declaring-type back-pointers intact after reorder");

        var asm = System.Reflection.Assembly.LoadFrom(Path.GetFullPath(outPath));
        var derived = asm.GetType("Fx.DerivedVirt");
        var instance = Activator.CreateInstance(derived);
        var baseType = asm.GetType("Fx.BaseVirt");
        Check((int)baseType.GetMethod("V1").Invoke(instance, null) == 11
           && (int)baseType.GetMethod("V2").Invoke(instance, null) == 22
           && (int)baseType.GetMethod("V3").Invoke(instance, null) == 33, "virtual dispatch still resolves to the override");
        var p09 = asm.GetType("Fx.Plain09");
        var p09i = Activator.CreateInstance(p09);
        p09.GetProperty("Prop3").SetValue(p09i, 7);
        Check((int)p09.GetProperty("Prop3").GetValue(p09i) == 7, "property accessors still bound after reorder");
        var exp = asm.GetType("Fx.ExpLayout");
        var expi = Activator.CreateInstance(exp);
        exp.GetField("b").SetValue(expi, 5);
        Check((int)exp.GetField("b").GetValue(expi) == 5 && (int)exp.GetField("a").GetValue(expi) == 0, "explicit-layout offsets still honoured");
        var seqFields = asm.GetType("Fx.SeqLayout").GetFields().Select(f => f.Name).ToList();
        Check(seqFields.SequenceEqual(new[] { "a", "b", "c", "d" }), "sequential-layout field order unchanged at runtime");
        var scriptFields = asm.GetType("Fx.Script").GetFields().Select(f => f.Name).ToList();
        Check(scriptFields.SequenceEqual(new[] { "hp", "title", "speed", "armed" }), "MonoBehaviour serialised field order unchanged at runtime");
        var enumNames = Enum.GetNames(asm.GetType("Fx.Colour")).ToList();
        Check(enumNames.SequenceEqual(new[] { "Red", "Green", "Blue", "Alpha", "Cyan", "Magenta" }), "enum member order unchanged at runtime");

        foreach (var key in new[] { "Fx2.PlainBase#f", "Fx2.PlainGrand#f", "Fx2.PlainMid#f", "Fx2.SerChild#f", "Fx2.SerLeaf#f" })
        {
            Check(Same(before[key], after1[key]), "base-of-serialisable field order pinned: " + key);
        }
        Check(before.ContainsKey("Fx2.PlainHost#f") && after1["#types"].Count == before["#types"].Count, "nested-type-bearing types survive the pass");
        Check(Load(dll).GetTypes().Any(t => t.NestedTypes.Count >= 2), "fixture actually contains a type with multiple nested types");

        int renamed = 0, aligned = 0;
        foreach (var key in before.Keys)
        {
            var a = after1[key];
            var b = after2[key];
            for (int i = 0; i < a.Count; i++)
            {
                if (!IsRenamable(key, a[i])) continue;
                renamed++;
                if (a[i] == b[i]) aligned++;
            }
        }
        double rate = renamed == 0 ? 0 : (double)aligned / renamed;
        Console.WriteLine($"positional recovery of renamed members across two seeds: {aligned}/{renamed} = {rate:P1}");
        Check(rate < 0.35, "ACCEPTANCE plan-06: two consecutive builds cannot be aligned index-for-index");

        int baseRenamed = 0, baseAligned = 0;
        foreach (var key in before.Keys)
        {
            var o = before[key];
            var a = after1[key];
            for (int i = 0; i < o.Count; i++)
            {
                if (!IsRenamable(key, o[i])) continue;
                baseRenamed++;
                if (o[i] == a[i]) baseAligned++;
            }
        }
        double baseRate = baseRenamed == 0 ? 0 : (double)baseAligned / baseRenamed;
        Console.WriteLine($"positional recovery of renamed members vs the unobfuscated original: {baseAligned}/{baseRenamed} = {baseRate:P1}");
        Check(baseRate < 0.35, "ACCEPTANCE plan-06: a build cannot be aligned against the unobfuscated original");

        Console.WriteLine(failures == 0 ? "ALL PASS" : failures + " FAILURE(S)");
        return failures == 0 ? 0 : 1;
    }
}
