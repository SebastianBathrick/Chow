using System;
using System.Runtime.CompilerServices;

namespace Chow.SourceData
{
    /// <summary>
    /// CPython 3.11's 64-bit <c>hash()</c>, as computed when CPython runs with <c>PYTHONHASHSEED=0</c>.
    /// Sets place their elements by these hashes, so matching them is what makes set iteration order
    /// match CPython's. (Dicts iterate in insertion order and don't need it.)
    /// </summary>
    static class PythonHash
    {
        // sys.hash_info: the modulus is the Mersenne prime 2^61 - 1.
        const int ModulusBits = 61;
        const ulong Modulus = (1UL << ModulusBits) - 1;
        const long InfinityHash = 314159;

        // CPython 3.11 hashes NaN and None by object address, so no fixed value matches it; these only
        // need to be stable. NaN uses sys.hash_info.nan.
        const long NanHash = 0;
        const long NoneHash = 0xFCA86420;

        // -1 is CPython's error marker, so a hash that comes out as -1 becomes -2.
        const long ErrorMarker = -1;
        const long ErrorMarkerReplacement = -2;

        const int FloatChunkBits = 28;
        const double FloatChunkScale = 268435456.0; // 2^28
        const double SubnormalScale = 18014398509481984.0; // 2^54

        // SipHash-1-3 initialization constants ("somepseudorandomlygeneratedbytes"), with k0 = k1 = 0.
        const ulong SipInit0 = 0x736f6d6570736575UL;
        const ulong SipInit1 = 0x646f72616e646f6dUL;
        const ulong SipInit2 = 0x6c7967656e657261UL;
        const ulong SipInit3 = 0x7465646279746573UL;

        /// <summary>CPython's <c>hash(value)</c>. The caller must have checked that the value is hashable.</summary>
        public static long Compute(SourceValue value)
        {
            switch (value.DataType)
            {
                case DataType.None:
                    return NoneHash;
                case DataType.Bool:
                case DataType.Long:
                    return HashLong(value.ToLong());
                case DataType.Double:
                    return HashDouble(value.ToDouble());
                case DataType.Str:
                    return HashString((string)value.ToObject());
                case DataType.FrozenSet:
                    return ((SourceSet)value.ToObject()).FrozenSetHash;
                default:
                    // Identity-hashed objects (iterators, values views) hash by address in CPython,
                    // which nothing outside the process can reproduce.
                    return RuntimeHelpers.GetHashCode(value.ToObject());
            }
        }

        // long_hash: |value| mod (2^61 - 1), with the sign put back.
        static long HashLong(long value)
        {
            var magnitude = value < 0 ? (ulong)(-(value + 1)) + 1 : (ulong)value;
            var reduced = (long)(magnitude % Modulus);
            return AvoidErrorMarker(value < 0 ? -reduced : reduced);
        }

        // _Py_HashDouble: the float's exact value mod (2^61 - 1), so equal ints and floats hash alike.
        static long HashDouble(double value)
        {
            if (double.IsNaN(value))
            {
                return NanHash;
            }

            if (double.IsInfinity(value))
            {
                return value > 0 ? InfinityHash : -InfinityHash;
            }

            var mantissa = Frexp(value, out var exponent);
            var sign = 1;

            if (mantissa < 0)
            {
                sign = -1;
                mantissa = -mantissa;
            }

            // Process 28 bits of the mantissa at a time.
            ulong x = 0;

            while (mantissa != 0)
            {
                x = ((x << FloatChunkBits) & Modulus) | x >> (ModulusBits - FloatChunkBits);
                mantissa *= FloatChunkScale;
                exponent -= FloatChunkBits;

                var chunk = (ulong)mantissa;
                mantissa -= chunk;
                x += chunk;

                if (x >= Modulus)
                {
                    x -= Modulus;
                }
            }

            // Multiplying by 2^exponent mod (2^61 - 1) is a rotation of the 61 bits.
            exponent = exponent >= 0
                ? exponent % ModulusBits
                : ModulusBits - 1 - ((-1 - exponent) % ModulusBits);
            x = ((x << exponent) & Modulus) | x >> (ModulusBits - exponent);

            return AvoidErrorMarker(sign < 0 ? -(long)x : (long)x);
        }

        // C's frexp: value = mantissa * 2^exponent with 0.5 <= |mantissa| < 1 (or 0). Built from the
        // IEEE bits because netstandard2.0 has no Math.ILogB.
        static double Frexp(double value, out int exponent)
        {
            if (value == 0)
            {
                exponent = 0;
                return value;
            }

            var bits = BitConverter.DoubleToInt64Bits(value);
            var biasedExponent = (int)((bits >> 52) & 0x7FF);

            if (biasedExponent == 0)
            {
                // Subnormal: scale into the normal range first.
                var scaled = Frexp(value * SubnormalScale, out exponent);
                exponent -= 54;
                return scaled;
            }

            exponent = biasedExponent - 1022;

            // Keep the sign and mantissa bits, and set the exponent so the value lands in [0.5, 1).
            const long signAndMantissaMask = unchecked((long)0x800FFFFFFFFFFFFFUL);
            return BitConverter.Int64BitsToDouble((bits & signAndMantissaMask) | (1022L << 52));
        }

        /// <summary>
        /// unicode_hash: SipHash-1-3 over the bytes CPython stores the string as: 1, 2, or 4 bytes per
        /// code point depending on the largest code point, little-endian.
        /// </summary>
        static long HashString(string value)
        {
            if (value.Length == 0)
            {
                return 0;
            }

            var width = StorageWidth(value, out var codePointCount);
            var state = new SipHashState();
            var word = 0UL;
            var wordBytes = 0;

            for (var i = 0; i < value.Length; i++)
            {
                int codePoint = value[i];

                if (width == 4 && char.IsHighSurrogate(value[i]) && i + 1 < value.Length && char.IsLowSurrogate(value[i + 1]))
                {
                    codePoint = char.ConvertToUtf32(value[i], value[i + 1]);
                    i++;
                }

                for (var b = 0; b < width; b++)
                {
                    word |= (ulong)((codePoint >> (8 * b)) & 0xFF) << (8 * wordBytes);
                    wordBytes++;

                    if (wordBytes == 8)
                    {
                        state.Compress(word);
                        word = 0;
                        wordBytes = 0;
                    }
                }
            }

            var byteCount = (ulong)codePointCount * (ulong)width;
            return AvoidErrorMarker((long)state.Finish(word | (byteCount << 56)));
        }

        // CPython's string kinds: Latin-1 (1 byte), UCS-2 (2 bytes), or UCS-4 (4 bytes). A surrogate
        // pair is one code point above U+FFFF; a lone surrogate is an ordinary UCS-2 code point.
        static int StorageWidth(string value, out int codePointCount)
        {
            var largest = 0;
            var pairs = 0;

            for (var i = 0; i < value.Length; i++)
            {
                var c = value[i];

                if (char.IsHighSurrogate(c) && i + 1 < value.Length && char.IsLowSurrogate(value[i + 1]))
                {
                    pairs++;
                    i++;
                    continue;
                }

                largest = Math.Max(largest, c);
            }

            codePointCount = value.Length - pairs;

            if (pairs > 0)
            {
                return 4;
            }

            return largest < 256 ? 1 : 2;
        }

        static long AvoidErrorMarker(long hash)
        {
            return hash == ErrorMarker ? ErrorMarkerReplacement : hash;
        }

        // pyhash.c siphash13 with a zero key.
        struct SipHashState
        {
            ulong _v0;
            ulong _v1;
            ulong _v2;
            ulong _v3;
            bool _isInitialized;

            public void Compress(ulong message)
            {
                EnsureInitialized();
                _v3 ^= message;
                Round();
                _v0 ^= message;
            }

            public ulong Finish(ulong lastWord)
            {
                Compress(lastWord);
                _v2 ^= 0xff;
                Round();
                Round();
                Round();
                return (_v0 ^ _v1) ^ (_v2 ^ _v3);
            }

            void EnsureInitialized()
            {
                if (_isInitialized)
                {
                    return;
                }

                _v0 = SipInit0;
                _v1 = SipInit1;
                _v2 = SipInit2;
                _v3 = SipInit3;
                _isInitialized = true;
            }

            // SINGLE_ROUND: HALF_ROUND(v0, v1, v2, v3, 13, 16) then HALF_ROUND(v2, v1, v0, v3, 17, 21).
            void Round()
            {
                unchecked
                {
                    _v0 += _v1;
                    _v2 += _v3;
                    _v1 = RotateLeft(_v1, 13) ^ _v0;
                    _v3 = RotateLeft(_v3, 16) ^ _v2;
                    _v0 = RotateLeft(_v0, 32);

                    _v2 += _v1;
                    _v0 += _v3;
                    _v1 = RotateLeft(_v1, 17) ^ _v2;
                    _v3 = RotateLeft(_v3, 21) ^ _v0;
                    _v2 = RotateLeft(_v2, 32);
                }
            }

            static ulong RotateLeft(ulong value, int bits)
            {
                return (value << bits) | (value >> (64 - bits));
            }
        }
    }
}
