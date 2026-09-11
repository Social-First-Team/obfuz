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

﻿using System;
using System.Text;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Assertions;

namespace Obfuz
{
    public abstract class EncryptorBase : IEncryptor
    {
        public abstract int OpCodeCount { get; }

        public static int[] ConvertToIntKey(byte[] key)
        {
            Assert.AreEqual(0, key.Length % 4);
            int align4Length = key.Length / 4;
            int[] intKey = new int[align4Length];
            Buffer.BlockCopy(key, 0, intKey, 0, key.Length);
            return intKey;
        }

        public abstract int Encrypt(int value, int opts, int salt);
        public abstract int Decrypt(int value, int opts, int salt);

        public virtual long Encrypt(long value, int opts, int salt)
        {
            int low = (int)value;
            int high = (int)(value >> 32);
            int encryptedLow = Encrypt(low, opts, salt);
            int encryptedHigh = Encrypt(high, opts, salt);
            return ((long)encryptedHigh << 32) | (uint)encryptedLow;
        }

        public virtual long Decrypt(long value, int opts, int salt)
        {
            int low = (int)value;
            int high = (int)(value >> 32);
            int decryptedLow = Decrypt(low, opts, salt);
            int decryptedHigh = Decrypt(high, opts, salt);
            return ((long)decryptedHigh << 32) | (uint)decryptedLow;
        }

        public virtual unsafe float Encrypt(float value, int opts, int salt)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return value;
            }
            ref int intValue = ref *(int*)&value;
            int xorValue = ((1 << 23) - 1) & Decrypt(0xABCD, opts, salt);
            intValue ^= xorValue;
            return value;
        }

        public virtual unsafe float Decrypt(float value, int opts, int salt)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return value;
            }
            ref int intValue = ref *(int*)&value;
            int xorValue = ((1 << 23) - 1) & Decrypt(0xABCD, opts, salt);
            intValue ^= xorValue;
            return value;
        }

        public virtual unsafe double Encrypt(double value, int opts, int salt)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                return value;
            }
            ref long longValue = ref *(long*)&value;
            long xorValue = ((1L << 52) - 1) & Decrypt(0xAABBCCDDL, opts, salt);
            longValue ^= xorValue;
            return value;
        }

        public virtual unsafe double Decrypt(double value, int opts, int salt)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                return value;
            }
            ref long longValue = ref *(long*)&value;
            long xorValue = ((1L << 52) - 1) & Decrypt(0xAABBCCDDL, opts, salt);
            longValue ^= xorValue;
            return value;
        }

        public virtual unsafe byte[] Encrypt(byte[] value, int offset, int length, int ops, int salt)
        {
            if (length == 0)
            {
                return Array.Empty<byte>();
            }

            var encryptedBytes = new byte[length];
            int intArrLength = length >> 2;

            // align to 4
            if ((offset & 0x3) != 0)
            {
                Buffer.BlockCopy(value, offset, encryptedBytes, 0, length);

                // encrypt int

                fixed (byte* dstBytePtr = &encryptedBytes[0])
                {
                    int* dstIntPtr = (int*)dstBytePtr;
                    int last = 0;
                    for (int i = 0; i < intArrLength; i++)
                    {
                        last ^= Encrypt(dstIntPtr[i], ops, salt);
                        dstIntPtr[i] = last;
                    }
                }
                for (int i = intArrLength * 4; i < length; i++)
                {
                    encryptedBytes[i] = (byte)(encryptedBytes[i] ^ salt);
                }
            }
            else
            {
                // encrypt int
                fixed (byte* srcBytePtr = &value[offset])
                {
                    fixed (byte* dstBytePtr = &encryptedBytes[0])
                    {
                        int* srcIntPtr = (int*)srcBytePtr;
                        int* dstIntPtr = (int*)dstBytePtr;

                        int last = 0;
                        for (int i = 0; i < intArrLength; i++)
                        {
                            last ^= Encrypt(srcIntPtr[i], ops, salt);
                            dstIntPtr[i] = last;
                        }
                    }
                }
                for (int i = intArrLength * 4; i < length; i++)
                {
                    encryptedBytes[i] = (byte)(value[offset + i] ^ salt);
                }
            }
            return encryptedBytes;
        }

        public unsafe virtual byte[] Decrypt(byte[] value, int offset, int length, int ops, int salt)
        {
            if (length == 0)
            {
                return Array.Empty<byte>();
            }
            var decryptedBytes = new byte[length];
            int intArrLength = length >> 2;

            // align to 4
            if ((offset & 0x3) != 0)
            {
                Buffer.BlockCopy(value, offset, decryptedBytes, 0, length);

                // encrypt int

                fixed (byte* dstBytePtr = &decryptedBytes[0])
                {
                    int* dstIntPtr = (int*)dstBytePtr;
                    int last = 0;
                    for (int i = 0; i < intArrLength; i++)
                    {
                        int oldLast = last;
                        last = dstIntPtr[i];
                        dstIntPtr[i] = Decrypt(last ^ oldLast, ops, salt);
                    }
                }
                for (int i = intArrLength * 4; i < length; i++)
                {
                    decryptedBytes[i] = (byte)(decryptedBytes[i] ^ salt);
                }
            }
            else
            {
                // encrypt int
                fixed (byte* srcBytePtr = &value[offset])
                {
                    fixed (byte* dstBytePtr = &decryptedBytes[0])
                    {
                        int* srcIntPtr = (int*)srcBytePtr;
                        int* dstIntPtr = (int*)dstBytePtr;
                        int last = 0;
                        for (int i = 0; i < intArrLength; i++)
                        {
                            int oldLast = last;
                            last = srcIntPtr[i];
                            dstIntPtr[i] = Decrypt(last ^ oldLast, ops, salt);
                        }
                    }
                }
                for (int i = intArrLength * 4; i < length; i++)
                {
                    decryptedBytes[i] = (byte)(value[offset + i] ^ salt);
                }
            }
            return decryptedBytes;
        }

        public virtual byte[] Encrypt(string value, int ops, int salt)
        {
            if (value.Length == 0)
            {
                return Array.Empty<byte>();
            }
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            return Encrypt(bytes, 0, bytes.Length, ops, salt);
        }

        public virtual string DecryptString(byte[] value, int offset, int length, int ops, int salt)
        {
            if (length == 0)
            {
                return string.Empty;
            }
            byte[] bytes = Decrypt(value, offset, length, ops, salt);
            return Encoding.UTF8.GetString(bytes);
        }

        public virtual unsafe void EncryptBlock(byte[] data, int ops, int salt)
        {
            int length = data.Length;
            int intArrLength = length >> 2;

            fixed (byte* dstBytePtr = &data[0])
            {
                int* dstIntPtr = (int*)dstBytePtr;
                int last = 0;
                for (int i = 0; i < intArrLength; i++)
                {
                    last ^= Encrypt(dstIntPtr[i], ops, salt);
                    dstIntPtr[i] = last;
                }
            }
            for (int i = intArrLength * 4; i < length; i++)
            {
                data[i] = (byte)(data[i] ^ salt);
            }
        }

        public virtual unsafe void DecryptBlock(byte[] data, int ops, int salt)
        {
            fixed (byte* dataPtr = &data[0])
            {
                DecryptBlock(dataPtr, data.Length, ops, salt);
            }
        }

        private unsafe void DecryptBlock(byte* data, int length, int ops, int salt)
        {
            int intArrLength = length >> 2;

            int* dstIntPtr = (int*)data;
            int last = 0;
            for (int i = 0; i < intArrLength; i++)
            {
                int oldLast = last;
                last = dstIntPtr[i];
                dstIntPtr[i] = Decrypt(oldLast ^ last, ops, salt);
            }
            for (int i = intArrLength * 4; i < length; i++)
            {
                data[i] = (byte)(data[i] ^ salt);
            }
        }

        /// <summary>
        /// Copies an encrypted blob into a freshly created array and decrypts it in place.
        ///
        /// The copy pins the destination and memcpys rather than using Buffer.BlockCopy, because
        /// BlockCopy rejects any array whose element class is not primitive - an enum array is
        /// IL2CPP_TYPE_VALUETYPE, so it throws "Object must be an array of primitives." at type
        /// initialisation time. RuntimeHelpers.InitializeArray, which this replaces, accepts enum
        /// and multidimensional arrays, so refusing them here silently broke every C# array
        /// initialiser with an enum element type.
        /// </summary>
        public virtual unsafe void DecryptInitializeArray(System.Array arr, byte[] data, int offset, int length, int ops, int salt)
        {
            if (length <= 0)
            {
                return;
            }
            void* dataPtr = UnsafeUtility.PinGCArrayAndGetDataAddress(arr, out ulong handle);
            try
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                // BlockCopy used to bounds check for us. Roslyn always sizes the blob to the array,
                // so a mismatch means the obfuscator emitted the wrong length, not bad user input.
                int arrayBytes = UnsafeUtility.SizeOf(arr.GetType().GetElementType()) * arr.Length;
                Assert.IsTrue(arrayBytes >= length,
                    $"DecryptInitializeArray: {length} bytes into an array holding {arrayBytes}");
#endif
                fixed (byte* srcPtr = &data[offset])
                {
                    UnsafeUtility.MemCpy(dataPtr, srcPtr, length);
                }
                DecryptBlock((byte*)dataPtr, length, ops, salt);
            }
            finally
            {
                UnsafeUtility.ReleaseGCObject(handle);
            }
        }
    }
}
