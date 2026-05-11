using System;
using System.Collections.Generic;
using System.Text;

namespace Chow.Interpreter.Values.Internal
{
    internal class InternalList
    {
        List<TaggedUnion> _elements;

        public int Count => _elements.Count;

        public TaggedUnion this[int index]
        {
            get
            {
                if (index < 0 || index >= _elements.Count)
                {
                    throw new IndexOutOfRangeException();
                }
                return _elements[index];
            }
            set
            {
                if (index < 0 || index >= _elements.Count)
                {
                    throw new IndexOutOfRangeException();
                }
                _elements[index] = value;
            }
        }

        public InternalList()
        {
            _elements = new List<TaggedUnion>();
        }

        void Append(TaggedUnion[] args)
        {
            ValidateArguments(args, 1);
            _elements.Add(args[0]);
        }

        void Clear(TaggedUnion[] args)
        {
            ValidateArguments(args, 0);
            _elements.Clear();
        }

        void Insert(TaggedUnion[] args)
        {
            ValidateArguments(args, 2);
            if (args[0].Tag != Tag.Int)
            {
                throw new ArgumentException($"Argument 0 must be of type {Tag.Int}, but was {args[0].Tag}");
            }
            int index = args[0].IntegerValue;
            if (index < 0)
            {
                index = Math.Max(0, _elements.Count + index);
            }
            else if (index > _elements.Count)
            {
                index = _elements.Count;
            }
            _elements.Insert(index, args[1]);
        }

        TaggedUnion Pop(TaggedUnion[] args)
        {
            if (args != null && args.Length > 1)
            {
                throw new ArgumentException($"Method 'pop' takes at most 1 argument, but {args.Length} were provided");
            }
            if (_elements.Count == 0)
            {
                throw new InvalidOperationException("pop from empty list");
            }

            int index = _elements.Count - 1;
            if (args != null && args.Length == 1)
            {
                if (args[0].Tag != Tag.Int)
                {
                    throw new ArgumentException($"Argument 0 must be of type {Tag.Int}, but was {args[0].Tag}");
                }
                index = args[0].IntegerValue;
                if (index < 0)
                {
                    index = _elements.Count + index;
                }
                if (index < 0 || index >= _elements.Count)
                {
                    throw new IndexOutOfRangeException();
                }
            }

            TaggedUnion value = _elements[index];
            _elements.RemoveAt(index);
            return value;
        }

        void Remove(TaggedUnion[] args)
        {
            ValidateArguments(args, 1);
            for (int i = 0; i < _elements.Count; i++)
            {
                if (_elements[i] == args[0])
                {
                    _elements.RemoveAt(i);
                    return;
                }
            }
            throw new ArgumentException("list.remove(x): x not in list");
        }

        void Reverse(TaggedUnion[] args)
        {
            ValidateArguments(args, 0);
            _elements.Reverse();
        }

        public TaggedUnion CallMethod(string methodName, TaggedUnion[] args = null)
        {
            switch (methodName)
            {
                case "append":
                    Append(args);
                    break;

                case "clear":
                    Clear(args);
                    break;

                case "insert":
                    Insert(args);
                    break;

                case "pop":
                    return Pop(args);

                case "remove":
                    Remove(args);
                    break;

                case "reverse":
                    Reverse(args);
                    break;

                default:
                    throw new NotImplementedException($"Method '{methodName}' is not implemented for InternalList");
            }

            return TaggedUnion.None;
        }

        static void ValidateArguments(TaggedUnion[] args, int reqArgCount = 0, Tag[] reqTypes = null)
        {
            if (reqTypes == null && reqArgCount == 0)
            {
                return;
            }

            if (args.Length != reqTypes.Length)
            {
                throw new ArgumentException($"Method requires {reqTypes.Length} arguments, but {args.Length} were provided");
            }

            if (reqTypes == null)
            {
                return;
            }

            for (int i = 0; i < reqTypes.Length; i++)
            {
                if (args[i].Tag == reqTypes[i])
                {
                    continue;
                }

                throw new ArgumentException($"Argument {i} must be of type {reqTypes[i]}, but was {args[i].Tag}");
            }
        }
    }
}
