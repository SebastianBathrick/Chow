using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Chow.SourceData;

namespace Chow.Interpreter.Compilation
{
    /// <summary>
    /// The frozenset constants of one compilation, built the way CPython 3.11's compiler builds them and
    /// shared across the module and its functions as CPython's constant cache (<c>c_const_cache</c>) is:
    /// two literals with the same constant elements reuse one frozenset.
    /// </summary>
    sealed class ConstantSetCache
    {
        // The single characters CPython 3.11 has already interned at startup as objects other than its
        // cached one-character strings (they come from its deep-frozen standard-library modules).
        // Interning a literal of one of these swaps the object; interning any other character doesn't.
        const string StartupInternedCharacters = "abcdefhijklmnopqrstvwxyBCLST_";

        readonly Dictionary<string, SourceSet> _sets = new Dictionary<string, SourceSet>(StringComparer.Ordinal);
        readonly HashSet<string> _programNames;

        /// <param name="programNames">Every identifier in the program, which CPython interns while parsing.</param>
        public ConstantSetCache(HashSet<string> programNames)
        {
            _programNames = programNames;
        }

        /// <summary>
        /// The frozenset constant for a set display whose elements fold to <paramref name="elements"/>.
        /// </summary>
        public SourceSet GetOrBuild(List<SourceValue> elements)
        {
            // PyFrozenSet_New on the folded elements, in source order.
            var built = new SourceSet(isFrozen: true);

            foreach (var element in elements)
            {
                built.Add(element);
            }

            var key = ConstantKey(built);

            if (_sets.TryGetValue(key, out var existing))
            {
                return existing;
            }

            // merge_consts_recursive rebuilds the frozenset from its own iteration order. Then
            // intern_string_constants rebuilds it again if interning an element swapped in a different
            // string object, which happens when an equal string was already interned.
            var constant = Rebuild(built);

            if (ContainsAlreadyInternedString(constant))
            {
                constant = Rebuild(constant);
            }

            constant.PrecomputeHash();
            _sets[key] = constant;
            return constant;
        }

        static SourceSet Rebuild(SourceSet set)
        {
            var rebuilt = new SourceSet(isFrozen: true);
            var iterator = set.GetIterator();

            while (iterator.TryMoveNext(out var element))
            {
                rebuilt.Add(element);
            }

            return rebuilt;
        }

        // _PyCode_ConstantKey: equal sets of constant keys share a constant. Keys tell True from 1 and
        // -0.0 from 0.0, so those never share even though the sets compare equal.
        static string ConstantKey(SourceSet set)
        {
            var keys = new List<string>(set.Length);
            var iterator = set.GetIterator();

            while (iterator.TryMoveNext(out var element))
            {
                keys.Add(ElementKey(element));
            }

            keys.Sort(StringComparer.Ordinal);

            var sb = new StringBuilder();

            foreach (var key in keys)
            {
                sb.Append(key.Length.ToString(CultureInfo.InvariantCulture)).Append(':').Append(key);
            }

            return sb.ToString();
        }

        static string ElementKey(SourceValue element)
        {
            switch (element.DataType)
            {
                case DataType.None:
                    return "n";
                case DataType.Bool:
                    return element.ToBool() ? "b1" : "b0";
                case DataType.Long:
                    return "i" + element.ToLong().ToString(CultureInfo.InvariantCulture);
                case DataType.Double:
                    return "f" + BitConverter.DoubleToInt64Bits(element.ToDouble()).ToString(CultureInfo.InvariantCulture);
                case DataType.Str:
                    return "s" + element;
                default:
                    throw new InvalidOperationException($"{element.DataType} is not a constant type.");
            }
        }

        bool ContainsAlreadyInternedString(SourceSet set)
        {
            var iterator = set.GetIterator();

            while (iterator.TryMoveNext(out var element))
            {
                if (element.DataType == DataType.Str && IsAlreadyInterned((string)element.ToObject()))
                {
                    return true;
                }
            }

            return false;
        }

        // Only strings CPython would intern here can swap. Of those, the ones already interned are the
        // program's identifiers, which its parser interned, and the strings interned at startup. Only
        // startup's single characters are modeled; its other names (such as 'name' or 'key') aren't.
        bool IsAlreadyInterned(string text)
        {
            if (!IsNameCharacters(text))
            {
                return false;
            }

            // A one-character identifier is the same cached object as the literal, so it never swaps.
            return text.Length == 1
                ? StartupInternedCharacters.IndexOf(text[0]) >= 0
                : _programNames.Contains(text);
        }

        // all_name_chars: ASCII letters, digits, and underscores. The empty string also passes it in
        // CPython, but it's a singleton that is already the interned object, so it never swaps.
        static bool IsNameCharacters(string text)
        {
            if (text.Length == 0)
            {
                return false;
            }

            foreach (var c in text)
            {
                var isAsciiLetterOrDigit = c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z' || c >= '0' && c <= '9';

                if (!isAsciiLetterOrDigit && c != '_')
                {
                    return false;
                }
            }

            return true;
        }
    }
}
