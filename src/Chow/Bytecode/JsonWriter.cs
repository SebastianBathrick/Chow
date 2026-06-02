using System.Collections.Generic;
using System.Text;

namespace Chow.Bytecode
{
    class JsonWriter
    {
        const int INDENT_SIZE = 2;

        readonly StringBuilder _sb = new StringBuilder();
        readonly Stack<bool> _emptyStack = new Stack<bool>();
        int _indent;

        public void OpenObject(string name = null)
        {
            OpenContainer('{', name);
        }

        public void CloseObject()
        {
            CloseContainer('}');
        }

        public void OpenArray(string name = null)
        {
            OpenContainer('[', name);
        }

        public void CloseArray()
        {
            CloseContainer(']');
        }

        public void WriteString(string name, string value)
        {
            BeginItem();
            AppendName(name);
            AppendQuoted(value);
        }

        public void WriteRaw(string name, string rawValue)
        {
            BeginItem();
            AppendName(name);
            _sb.Append(rawValue);
        }

        public void WriteStringItem(string value)
        {
            BeginItem();
            AppendQuoted(value);
        }

        public void WriteRawItem(string rawValue)
        {
            BeginItem();
            _sb.Append(rawValue);
        }

        public override string ToString()
        {
            return _sb.ToString();
        }

        void OpenContainer(char brace, string name)
        {
            if (_emptyStack.Count > 0)
            {
                BeginItem();

                if (name != null)
                {
                    AppendName(name);
                }
            }

            _sb.Append(brace);
            _emptyStack.Push(true);
            _indent++;
        }

        void CloseContainer(char brace)
        {
            var wasEmpty = _emptyStack.Pop();
            _indent--;

            if (!wasEmpty)
            {
                AppendNewLine();
            }

            _sb.Append(brace);
        }

        void BeginItem()
        {
            var wasEmpty = _emptyStack.Pop();

            if (!wasEmpty)
            {
                _sb.Append(',');
            }

            _emptyStack.Push(false);
            AppendNewLine();
        }

        void AppendName(string name)
        {
            AppendQuoted(name);
            _sb.Append(": ");
        }

        void AppendNewLine()
        {
            _sb.AppendLine();

            if (_indent > 0)
            {
                _sb.Append(' ', _indent * INDENT_SIZE);
            }
        }

        void AppendQuoted(string s)
        {
            _sb.Append('"');

            foreach (var c in s)
            {
                switch (c)
                {
                    case '"':
                    {
                        _sb.Append("\\\"");
                        break;
                    }
                    case '\\':
                    {
                        _sb.Append("\\\\");
                        break;
                    }
                    case '\n':
                    {
                        _sb.Append("\\n");
                        break;
                    }
                    case '\r':
                    {
                        _sb.Append("\\r");
                        break;
                    }
                    case '\t':
                    {
                        _sb.Append("\\t");
                        break;
                    }
                    default:
                    {
                        if (c < ' ')
                        {
                            _sb.Append("\\u");
                            _sb.Append(((int)c).ToString("X4"));
                        }
                        else
                        {
                            _sb.Append(c);
                        }

                        break;
                    }
                }
            }

            _sb.Append('"');
        }
    }
}
