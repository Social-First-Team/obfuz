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
using Obfuz.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Obfuz.ObfusPasses.SymbolObfus
{
    public class MemberReorder
    {
        private readonly Random _random;
        private readonly IObfuscationPolicy _renamePolicy;

        public MemberReorder(int seed, IObfuscationPolicy renamePolicy)
        {
            if (seed == 0)
            {
                throw new Exception("SymbolObfuscationSettings.enableMemberReordering requires a non-zero nameRandomSeed, otherwise the member layout is identical in every build.");
            }
            _random = new Random(seed);
            _renamePolicy = renamePolicy;
        }

        public void Process(List<ModuleDef> modules)
        {
            foreach (ModuleDef mod in modules)
            {
                Reorder(mod.Types, t => t.IsGlobalModuleType || IsPositionPinnedType(t));
                foreach (TypeDef type in mod.GetTypes().ToList())
                {
                    if (MayReorderFields(type))
                    {
                        Reorder(type.Fields, f => !_renamePolicy.NeedRename(f));
                    }
                    Reorder(type.Methods, IsPositionPinnedMethod);
                    Reorder(type.Properties, p => !_renamePolicy.NeedRename(p));
                    Reorder(type.Events, e => !_renamePolicy.NeedRename(e));
                }
            }
        }

        private bool IsPositionPinnedType(TypeDef type)
        {
            if (!_renamePolicy.NeedRename(type))
            {
                return true;
            }
            return type.Methods.Any(MetaUtil.HasRuntimeInitializeOnLoadMethodAttribute);
        }

        private bool IsPositionPinnedMethod(MethodDef method)
        {
            if (method.IsVirtual || method.IsAbstract || method.HasOverrides)
            {
                return true;
            }
            if (MetaUtil.HasRuntimeInitializeOnLoadMethodAttribute(method))
            {
                return true;
            }
            return !_renamePolicy.NeedRename(method);
        }

        private static bool MayReorderFields(TypeDef type)
        {
            if (type.IsEnum || type.IsValueType)
            {
                return false;
            }
            if (type.IsSequentialLayout || type.IsExplicitLayout)
            {
                return false;
            }
            if (MetaUtil.IsScriptOrSerializableType(type))
            {
                return false;
            }
            return !type.Fields.Any(f => f.FieldOffset != null);
        }

        private void Reorder<T>(IList<T> members, Func<T, bool> isPinned)
        {
            int count = members.Count;
            if (count < 2)
            {
                return;
            }

            var order = new List<T>(count);
            var movableSlots = new List<int>();
            for (int i = 0; i < count; i++)
            {
                T member = members[i];
                order.Add(member);
                if (!isPinned(member))
                {
                    movableSlots.Add(i);
                }
            }
            if (movableSlots.Count < 2)
            {
                return;
            }

            var movable = movableSlots.Select(i => order[i]).ToList();
            for (int i = movable.Count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                T tmp = movable[i];
                movable[i] = movable[j];
                movable[j] = tmp;
            }
            for (int i = 0; i < movableSlots.Count; i++)
            {
                order[movableSlots[i]] = movable[i];
            }

            members.Clear();
            foreach (T member in order)
            {
                members.Add(member);
            }
        }
    }
}
