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

namespace Obfuz.ObfusPasses.SymbolObfus.NameMakers
{

    public class WordSetNameMaker : NameMakerBase
    {
        private readonly string _namePrefix;
        private readonly List<string> _wordSet;
        private readonly int _seed;
        private int _scopeIndex;

        public WordSetNameMaker(string namePrefix, List<string> wordSet) : this(namePrefix, wordSet, 0)
        {
        }

        public WordSetNameMaker(string namePrefix, List<string> wordSet, int seed)
        {
            _namePrefix = namePrefix;
            _wordSet = wordSet;
            _seed = seed;
        }

        protected override INameScope CreateNameScope()
        {
            if (_seed == 0)
            {
                return new NameScope(_namePrefix, _wordSet);
            }

            // Distinct per scope, otherwise every scope would emit the same sequence.
            int scopeSeed = unchecked(_seed + _scopeIndex++ * (int)0x9E3779B9);
            return new NameScope(_namePrefix, _wordSet, scopeSeed != 0 ? scopeSeed : 1);
        }
    }
}
