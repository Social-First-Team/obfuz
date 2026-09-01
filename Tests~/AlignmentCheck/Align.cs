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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

static class Align
{
    static ModuleDefMD Load(string p)
    {
        var ctx = ModuleDef.CreateModuleContext();
        ((AssemblyResolver)ctx.AssemblyResolver).PreSearchPaths.Add(
            System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(p)));
        return ModuleDefMD.Load(p, ctx);
    }

    static string FieldNameOf(string sig)
    {
        int i = sig.LastIndexOf(' ');
        return i < 0 ? sig : sig.Substring(i + 1);
    }

    static string MethodNameOf(string sig)
    {
        int paren = sig.IndexOf('(');
        string head = paren < 0 ? sig : sig.Substring(0, paren);
        int sep = head.IndexOf("::", StringComparison.Ordinal);
        if (sep < 0) return head;
        string n = head.Substring(sep + 2);
        return n.StartsWith("@") ? n.Substring(1) : n;
    }

    static int Main(string[] args)
    {
        string origPath = args[0], obfPath = args[1], mapPath = args[2];

        var typeBack = new Dictionary<string, string>();
        var memberBack = new Dictionary<string, string>();
        var renamedKey = new HashSet<string>();

        var doc = new XmlDocument();
        doc.Load(mapPath);
        foreach (XmlNode aN in doc.DocumentElement.ChildNodes)
        {
            if (!(aN is XmlElement a) || a.Name != "assembly") continue;
            foreach (XmlNode tN in a.ChildNodes)
            {
                if (!(tN is XmlElement t) || t.Name != "type") continue;
                string origType = t.Attributes["fullName"].Value;
                string newType = t.Attributes["newFullName"]?.Value;
                string obfType = string.IsNullOrEmpty(newType) ? origType : newType;
                typeBack[obfType] = origType;
                if (!string.IsNullOrEmpty(newType) && newType != origType) renamedKey.Add("T|" + obfType);

                foreach (XmlNode mN in t.ChildNodes)
                {
                    if (!(mN is XmlElement m)) continue;
                    string kind = m.Name;
                    string sig = m.Attributes["signature"]?.Value;
                    if (sig == null) continue;
                    string origName = kind == "method" ? MethodNameOf(sig) : FieldNameOf(sig);
                    string newName = m.Attributes["newName"]?.Value;
                    string obfName = string.IsNullOrEmpty(newName) ? origName : newName;
                    string key = kind + "|" + obfType + "::" + obfName;
                    memberBack[key] = origType + "::" + origName;
                    if (!string.IsNullOrEmpty(newName) && newName != origName) renamedKey.Add(key);
                }
            }
        }

        var mo = Load(origPath);
        var mb = Load(obfPath);

        DoTypes(mo, mb, typeBack, renamedKey);
        DoMembers(mo, mb, typeBack, memberBack, renamedKey, "method");
        DoMembers(mo, mb, typeBack, memberBack, renamedKey, "field");
        WithinType(mo, mb, typeBack, memberBack, renamedKey, "method");
        WithinType(mo, mb, typeBack, memberBack, renamedKey, "field");
        return 0;
    }

    static List<TypeDef> TypeRows(ModuleDefMD m)
    {
        var n = (int)m.Metadata.TablesStream.TypeDefTable.Rows;
        var r = new List<TypeDef>(n);
        for (uint rid = 1; rid <= n; rid++) { var t = m.ResolveTypeDef(rid); if (t != null) r.Add(t); }
        return r;
    }

    static void DoTypes(ModuleDefMD mo, ModuleDefMD mb, Dictionary<string, string> typeBack, HashSet<string> renamedKey)
    {
        var orig = TypeRows(mo).Select(t => t.FullName).ToList();
        var origSet = new HashSet<string>(orig);
        var obf = new List<string>();
        var obfRenamed = new List<bool>();
        foreach (var t in TypeRows(mb))
        {
            if (!typeBack.TryGetValue(t.FullName, out var o) || !origSet.Contains(o)) continue;
            obf.Add(o);
            obfRenamed.Add(renamedKey.Contains("T|" + t.FullName));
        }
        Report("types", orig, obf, obfRenamed);
    }

    static void DoMembers(ModuleDefMD mo, ModuleDefMD mb, Dictionary<string, string> typeBack,
        Dictionary<string, string> memberBack, HashSet<string> renamedKey, string kind)
    {
        var orig = new List<string>();
        foreach (var t in TypeRows(mo))
            foreach (var n in kind == "method" ? t.Methods.Select(x => x.Name.String) : t.Fields.Select(x => x.Name.String))
                orig.Add(t.FullName + "::" + n);
        var origSet = new HashSet<string>(orig);

        var obf = new List<string>();
        var obfRenamed = new List<bool>();
        foreach (var t in TypeRows(mb))
        {
            foreach (var n in kind == "method" ? t.Methods.Select(x => x.Name.String) : t.Fields.Select(x => x.Name.String))
            {
                string key = kind + "|" + t.FullName + "::" + n;
                if (!memberBack.TryGetValue(key, out var o) || !origSet.Contains(o)) continue;
                obf.Add(o);
                obfRenamed.Add(renamedKey.Contains(key));
            }
        }
        Report(kind + "s", orig, obf, obfRenamed);
    }

    static void WithinType(ModuleDefMD mo, ModuleDefMD mb, Dictionary<string, string> typeBack,
        Dictionary<string, string> memberBack, HashSet<string> renamedKey, string kind)
    {
        var origByType = new Dictionary<string, List<string>>();
        foreach (var t in TypeRows(mo))
            origByType[t.FullName] = (kind == "method" ? t.Methods.Select(x => x.Name.String) : t.Fields.Select(x => x.Name.String)).ToList();

        int ren = 0, renHit = 0;
        foreach (var t in TypeRows(mb))
        {
            if (!typeBack.TryGetValue(t.FullName, out var ot) || !origByType.TryGetValue(ot, out var origList)) continue;
            var mapped = new List<string>();
            var isRen = new List<bool>();
            foreach (var n in kind == "method" ? t.Methods.Select(x => x.Name.String) : t.Fields.Select(x => x.Name.String))
            {
                string key = kind + "|" + t.FullName + "::" + n;
                if (!memberBack.TryGetValue(key, out var full)) continue;
                string bare = full.Substring(full.LastIndexOf("::", StringComparison.Ordinal) + 2);
                if (!origList.Contains(bare)) continue;
                mapped.Add(bare); isRen.Add(renamedKey.Contains(key));
            }
            var keep = new HashSet<string>(mapped);
            var filteredOrig = origList.Where(keep.Contains).ToList();
            int n2 = Math.Min(filteredOrig.Count, mapped.Count);
            for (int i = 0; i < n2; i++)
            {
                if (!isRen[i]) continue;
                ren++;
                if (filteredOrig[i] == mapped[i]) renHit++;
            }
        }
        Console.WriteLine($"{kind + "s",-8} WITHIN-TYPE renamed holding their slot {renHit,6}/{ren,-6} {(ren == 0 ? 0 : 100.0 * renHit / ren),5:F2}%");
    }

    static void Report(string label, List<string> origAll, List<string> obf, List<bool> renamed)
    {
        var keep = new HashSet<string>(obf);
        var orig = origAll.Where(keep.Contains).ToList();
        if (orig.Count != obf.Count)
        {
            Console.WriteLine($"{label,-8} SKEW: filtered original {orig.Count} vs obfuscated {obf.Count} - not comparable");
        }
        int n = Math.Min(orig.Count, obf.Count);
        int all = 0, allHit = 0, ren = 0, renHit = 0;
        for (int i = 0; i < n; i++)
        {
            bool hit = orig[i] == obf[i];
            all++; if (hit) allHit++;
            if (renamed[i]) { ren++; if (hit) renHit++; }
        }
        Console.WriteLine($"{label,-8} compared {orig.Count,6} of {origAll.Count,6} | aligned {allHit,6}/{all,-6} {(all == 0 ? 0 : 100.0 * allHit / all),5:F2}% | renamed aligned {renHit,5}/{ren,-6} {(ren == 0 ? 0 : 100.0 * renHit / ren),5:F2}%");
    }
}
