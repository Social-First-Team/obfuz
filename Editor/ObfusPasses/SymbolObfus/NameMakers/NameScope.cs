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

﻿using System.Collections.Generic;
using System.Text;

namespace Obfuz.ObfusPasses.SymbolObfus.NameMakers
{

    public class NameScope : NameScopeBase
    {
        private readonly string _namePrefix;
        private readonly List<string> _wordSet;
        private readonly System.Random _random;
        private readonly int _nameSpace;
        private int _nextIndex;

        public NameScope(string namePrefix, List<string> wordSet) : this(namePrefix, wordSet, 0)
        {
        }

        // A zero seed keeps the original deterministic base-N counter. Any other seed draws
        // names at random from a space wide enough that collisions are rare; NameScopeBase
        // retries on collision, so correctness does not depend on the space being large.
        public NameScope(string namePrefix, List<string> wordSet, int seed)
        {
            _namePrefix = namePrefix;
            _wordSet = wordSet;
            _nextIndex = 0;
            _random = seed != 0 ? new System.Random(seed) : null;
            long space = 1;
            for (int i = 0; i < 4; i++)
            {
                space *= wordSet.Count;
            }
            _nameSpace = (int)System.Math.Min(space, int.MaxValue);
        }

        protected override void BuildNewName(StringBuilder nameBuilder, string originalName, string lastName)
        {
            nameBuilder.Append(_namePrefix);
            for (int i = _random != null ? _random.Next(_nameSpace) : _nextIndex++; ;)
            {
                nameBuilder.Append(_wordSet[i % _wordSet.Count]);
                i = i / _wordSet.Count;
                if (i == 0)
                {
                    break;
                }
            }

            // keep generic type name pattern {name}`{n}, if not, il2cpp may raise exception in typeof(G<T>) when G contains a field likes `T a`.
            int index = originalName.LastIndexOf('`');
            if (index != -1)
            {
                nameBuilder.Append(originalName.Substring(index));
            }
        }
    }
}
