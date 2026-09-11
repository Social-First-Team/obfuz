using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.Linq;

static class Program
{
    static int Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("usage: check <OriginalAssemblies/X.dll> <ObfuscatedAssemblies/X.dll>");
            return 2;
        }
        ModuleDefMD orig = ModuleDefMD.Load(args[0]);
        ModuleDefMD obf = ModuleDefMD.Load(args[1]);

        var origParamCount = new Dictionary<string, int>();
        var ambiguous = new HashSet<string>();
        foreach (TypeDef t in orig.GetTypes())
        {
            foreach (MethodDef m in t.Methods)
            {
                string key = t.Name + "::" + m.Name;
                if (!origParamCount.ContainsKey(key)) origParamCount[key] = m.MethodSig.Params.Count;
                else ambiguous.Add(key);
            }
        }

        int pinned = 0, violations = 0;
        var samples = new List<string>();
        foreach (TypeDef t in obf.GetTypes())
        {
            foreach (MethodDef m in t.Methods)
            {
                // a surviving name means the rename policy pinned it
                if (m.Name.StartsWith("$") || m.Name.StartsWith(".")) continue;
                string key = t.Name + "::" + m.Name;
                if (ambiguous.Contains(key) || !origParamCount.TryGetValue(key, out int before)) continue;
                pinned++;
                int after = m.MethodSig.Params.Count;
                if (after == before) continue;
                violations++;
                if (samples.Count < 20) samples.Add($"    {key}  {before} -> {after} params");
            }
        }

        Console.WriteLine($"name-pinned methods compared : {pinned}");
        Console.WriteLine($"pinned methods whose signature changed : {violations}");
        foreach (string s in samples) Console.WriteLine(s);
        Console.WriteLine(violations == 0
            ? "PASS  no name-bound method had its signature changed"
            : "FAIL  name-bound methods were re-signed; anything invoking them by name is broken");
        return violations == 0 ? 0 : 1;
    }
}
