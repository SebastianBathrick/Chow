using System.Runtime.InteropServices;

namespace Chow.Cli;

internal static class CommandLineEditor
{
    private const string LINE_INDICATOR = ">>> ";
    private const string LINE_SEPERATOR = "\n";
    private const string KEYBOARD_INTERRUPT_MESSAGE = "Keyboard Interupt";

    private static readonly List<string> _lines = new() { string.Empty };
    private static readonly string[] _helpLines = BuildHelpLines();

    private static int _startTop;
    private static int _cursorX;
    private static int _cursorY;
    private static int _desiredX;
    private static int _drawnLineCount;
    private static bool _showHelp;

    private static bool _hasSelection;
    private static int _selectionAnchorX;
    private static int _selectionAnchorY;

    /// <summary>
    /// Reads a multi-line source block from the console. Returns false when the
    /// user requests REPL exit (Ctrl+D); otherwise returns true with the entered
    /// source in <paramref name="sourceCode"/>.
    /// </summary>
    public static bool TryReadBlock(out string sourceCode)
    {
        ReserveInitialRows();

        while (true)
        {
            Draw();

            ConsoleKeyInfo key = Console.ReadKey(true);

            if (TryHandleControlChord(key, out bool exitRequested, out sourceCode))
            {
                if (exitRequested)
                {
                    return false;
                }

                continue;
            }

            if (TryHandleNavigationOrEdit(key, out bool submitted))
            {
                if (submitted)
                {
                    sourceCode = string.Join(LINE_SEPERATOR, _lines);
                    return true;
                }

                continue;
            }

            if (!char.IsControl(key.KeyChar))
            {
                InsertChar(key.KeyChar);
            }
        }
    }

    #region Key Dispatch Methods

    private static bool TryHandleControlChord(ConsoleKeyInfo key, out bool exitRequested, out string sourceCode)
    {
        exitRequested = false;
        sourceCode = string.Empty;

        if (!key.Modifiers.HasFlag(ConsoleModifiers.Control))
        {
            return false;
        }

        switch (key.Key)
        {
            case ConsoleKey.Backspace:
                BackspaceToken();
                return true;

            case ConsoleKey.C:
                if (_hasSelection && Clipboard.IsSupported)
                {
                    Clipboard.TrySetText(GetSelectedText());
                }
                else
                {
                    KeyboardInterrupt();
                }
                return true;

            case ConsoleKey.X:
                if (_hasSelection && Clipboard.IsSupported)
                {
                    Clipboard.TrySetText(GetSelectedText());
                    DeleteSelection();
                }
                return true;

            case ConsoleKey.V:
                if (Clipboard.IsSupported && Clipboard.TryGetText(out string pasted))
                {
                    if (_hasSelection)
                    {
                        DeleteSelection();
                    }

                    InsertText(pasted);
                }
                return true;

            case ConsoleKey.D:
                EndBlock();
                exitRequested = true;
                return true;

            case ConsoleKey.L:
                Console.Clear();
                _startTop = 0;
                _drawnLineCount = TotalRows();
                return true;

            case ConsoleKey.R:
                ResetInput();
                return true;

            default:
                return false;
        }
    }

    private static bool TryHandleNavigationOrEdit(ConsoleKeyInfo key, out bool submitted)
    {
        submitted = false;
        bool shift = key.Modifiers.HasFlag(ConsoleModifiers.Shift);

        switch (key.Key)
        {
            case ConsoleKey.Enter:
                if (shift)
                {
                    NewLine();
                    return true;
                }

                EndBlock();
                submitted = true;
                return true;

            case ConsoleKey.Escape:
                if (_hasSelection)
                {
                    ClearSelection();
                }
                return true;

            case ConsoleKey.LeftArrow:
                MoveLeft(shift);
                return true;

            case ConsoleKey.RightArrow:
                MoveRight(shift);
                return true;

            case ConsoleKey.UpArrow:
                MoveUp(shift);
                return true;

            case ConsoleKey.DownArrow:
                MoveDown(shift);
                return true;

            case ConsoleKey.Backspace:
                Backspace();
                return true;

            case ConsoleKey.F1:
                _showHelp = !_showHelp;
                return true;

            default:
                return false;
        }
    }

    #endregion

    #region Lifecycle Methods

    private static void ReserveInitialRows()
    {
        int reserve = TotalRows();

        for (int i = 0; i < reserve; i++)
        {
            Console.WriteLine();
        }

        _startTop = Console.CursorTop - reserve;
        _cursorY = _lines.Count - 1;
        _cursorX = _lines[_cursorY].Length;
        _desiredX = _cursorX;
        _drawnLineCount = reserve;
        ClearSelection();
    }

    private static void EndBlock()
    {
        Console.SetCursorPosition(0, BlockTop() + _lines.Count);
        Console.WriteLine();
    }

    private static void ResetInput()
    {
        _lines.Clear();
        _lines.Add(string.Empty);
        _cursorX = 0;
        _cursorY = 0;
        _desiredX = 0;
        ClearSelection();
    }

    private static void KeyboardInterrupt()
    {
        Console.SetCursorPosition(0, BlockTop() + _lines.Count);
        Console.WriteLine();
        Console.WriteLine(KEYBOARD_INTERRUPT_MESSAGE);

        int reserve = TotalRows();

        for (int i = 0; i < reserve; i++)
        {
            Console.WriteLine();
        }

        _startTop = Console.CursorTop - reserve;
        _drawnLineCount = reserve;
    }

    #endregion

    #region Drawing Methods

    private static int BlockTop()
    {
        return _startTop + (_showHelp ? _helpLines.Length : 0);
    }

    private static int TotalRows()
    {
        return _lines.Count + (_showHelp ? _helpLines.Length : 0);
    }

    private static void Draw()
    {
        int helpCount = _showHelp ? _helpLines.Length : 0;

        for (int i = 0; i < helpCount; i++)
        {
            Console.SetCursorPosition(0, _startTop + i);
            WriteFullWidthLine(_helpLines[i]);
        }

        int blockTop = _startTop + helpCount;
        (int startY, int startX, int endY, int endX) = GetOrderedSelection();

        for (int i = 0; i < _lines.Count; i++)
        {
            Console.SetCursorPosition(0, blockTop + i);
            DrawLine(i, startY, startX, endY, endX);
        }

        int totalRows = helpCount + _lines.Count;

        for (int i = totalRows; i < _drawnLineCount; i++)
        {
            Console.SetCursorPosition(0, _startTop + i);
            Console.Write(new string(' ', Console.WindowWidth));
        }

        _drawnLineCount = totalRows;

        Console.SetCursorPosition(LINE_INDICATOR.Length + _cursorX, blockTop + _cursorY);
    }

    private static void DrawLine(int row, int startY, int startX, int endY, int endX)
    {
        string content = _lines[row];
        Console.Write(LINE_INDICATOR);

        if (!_hasSelection || row < startY || row > endY)
        {
            WriteRest(content);
            return;
        }

        int selStart = (row == startY) ? startX : 0;
        int selEnd = (row == endY) ? endX : content.Length;

        if (selStart > content.Length)
        {
            selStart = content.Length;
        }

        if (selEnd > content.Length)
        {
            selEnd = content.Length;
        }

        Console.Write(content.Substring(0, selStart));

        ConsoleColor prevBg = Console.BackgroundColor;
        ConsoleColor prevFg = Console.ForegroundColor;
        Console.BackgroundColor = ConsoleColor.DarkGray;
        Console.ForegroundColor = ConsoleColor.White;

        string mid = content.Substring(selStart, selEnd - selStart);
        bool drewNewlineMarker = mid.Length == 0 && row < endY;

        if (drewNewlineMarker)
        {
            Console.Write(' ');
        }
        else
        {
            Console.Write(mid);
        }

        Console.BackgroundColor = prevBg;
        Console.ForegroundColor = prevFg;

        Console.Write(content.Substring(selEnd));

        int written = LINE_INDICATOR.Length + content.Length + (drewNewlineMarker ? 1 : 0);
        int remaining = Console.WindowWidth - written;

        if (remaining > 0)
        {
            Console.Write(new string(' ', remaining));
        }
    }

    private static void WriteFullWidthLine(string text)
    {
        Console.Write(text);
        int remaining = Console.WindowWidth - text.Length;

        if (remaining > 0)
        {
            Console.Write(new string(' ', remaining));
        }
    }

    private static void WriteRest(string tail)
    {
        Console.Write(tail);
        int written = LINE_INDICATOR.Length + tail.Length;
        int remaining = Console.WindowWidth - written;

        if (remaining > 0)
        {
            Console.Write(new string(' ', remaining));
        }
    }

    #endregion

    #region Editing Methods

    private static void InsertChar(char c)
    {
        if (_hasSelection)
        {
            DeleteSelection();
        }

        _lines[_cursorY] = _lines[_cursorY].Insert(_cursorX, c.ToString());
        _cursorX++;
        _desiredX = _cursorX;
    }

    private static void NewLine()
    {
        if (_hasSelection)
        {
            DeleteSelection();
        }

        string current = _lines[_cursorY];
        string before = current.Substring(0, _cursorX);
        string after = current.Substring(_cursorX);

        _lines[_cursorY] = before;
        _lines.Insert(_cursorY + 1, after);

        _cursorY++;
        _cursorX = 0;
        _desiredX = _cursorX;
    }

    private static void Backspace()
    {
        if (_hasSelection)
        {
            DeleteSelection();
            return;
        }

        if (_cursorX > 0)
        {
            _lines[_cursorY] = _lines[_cursorY].Remove(_cursorX - 1, 1);
            _cursorX--;
        }
        else if (_cursorY > 0)
        {
            _cursorX = _lines[_cursorY - 1].Length;
            _lines[_cursorY - 1] += _lines[_cursorY];
            _lines.RemoveAt(_cursorY);
            _cursorY--;
        }

        _desiredX = _cursorX;
    }

    private static void BackspaceToken()
    {
        if (_hasSelection)
        {
            DeleteSelection();
            return;
        }

        if (_cursorX == 0)
        {
            Backspace();
            return;
        }

        string line = _lines[_cursorY];
        int end = _cursorX;
        int i = end;

        while (i > 0 && char.IsWhiteSpace(line[i - 1]))
        {
            i--;
        }

        if (i > 0)
        {
            bool wordChunk = IsWordChar(line[i - 1]);

            while (i > 0 && IsWordChar(line[i - 1]) == wordChunk && !char.IsWhiteSpace(line[i - 1]))
            {
                i--;
            }
        }

        _lines[_cursorY] = line.Remove(i, end - i);
        _cursorX = i;
        _desiredX = _cursorX;
    }

    private static void InsertText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        string normalized = text.Replace("\r\n", "\n").Replace("\r", "\n");
        string[] segments = normalized.Split('\n');

        string current = _lines[_cursorY];
        string before = current.Substring(0, _cursorX);
        string after = current.Substring(_cursorX);

        if (segments.Length == 1)
        {
            _lines[_cursorY] = before + segments[0] + after;
            _cursorX = before.Length + segments[0].Length;
        }
        else
        {
            _lines[_cursorY] = before + segments[0];

            for (int i = 1; i < segments.Length - 1; i++)
            {
                _lines.Insert(_cursorY + i, segments[i]);
            }

            string lastSeg = segments[segments.Length - 1];
            _lines.Insert(_cursorY + segments.Length - 1, lastSeg + after);

            _cursorY += segments.Length - 1;
            _cursorX = lastSeg.Length;
        }

        _desiredX = _cursorX;
    }

    private static bool IsWordChar(char c)
    {
        return char.IsLetterOrDigit(c) || c == '_';
    }

    #endregion

    #region Selection Methods

    private static void BeginOrExtendSelection()
    {
        if (!_hasSelection)
        {
            _selectionAnchorX = _cursorX;
            _selectionAnchorY = _cursorY;
            _hasSelection = true;
        }
    }

    private static void ClearSelection()
    {
        _hasSelection = false;
        _selectionAnchorX = 0;
        _selectionAnchorY = 0;
    }

    private static (int StartY, int StartX, int EndY, int EndX) GetOrderedSelection()
    {
        if (!_hasSelection)
        {
            return (0, 0, 0, 0);
        }

        bool anchorFirst = _selectionAnchorY < _cursorY
            || (_selectionAnchorY == _cursorY && _selectionAnchorX <= _cursorX);

        if (anchorFirst)
        {
            return (_selectionAnchorY, _selectionAnchorX, _cursorY, _cursorX);
        }

        return (_cursorY, _cursorX, _selectionAnchorY, _selectionAnchorX);
    }

    private static string GetSelectedText()
    {
        if (!_hasSelection)
        {
            return string.Empty;
        }

        (int sy, int sx, int ey, int ex) = GetOrderedSelection();

        if (sy == ey)
        {
            return _lines[sy].Substring(sx, ex - sx);
        }

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.Append(_lines[sy].Substring(sx));
        sb.Append('\n');

        for (int i = sy + 1; i < ey; i++)
        {
            sb.Append(_lines[i]);
            sb.Append('\n');
        }

        sb.Append(_lines[ey].Substring(0, ex));
        return sb.ToString();
    }

    private static void DeleteSelection()
    {
        if (!_hasSelection)
        {
            return;
        }

        (int sy, int sx, int ey, int ex) = GetOrderedSelection();

        string head = _lines[sy].Substring(0, sx);
        string tail = _lines[ey].Substring(ex);

        _lines[sy] = head + tail;

        for (int i = ey; i > sy; i--)
        {
            _lines.RemoveAt(i);
        }

        _cursorY = sy;
        _cursorX = sx;
        _desiredX = _cursorX;
        ClearSelection();
    }

    #endregion

    #region Movement Methods

    private static void MoveLeft(bool extendSelection)
    {
        if (extendSelection)
        {
            BeginOrExtendSelection();
        }
        else
        {
            ClearSelection();
        }

        if (_cursorX > 0)
        {
            _cursorX--;
        }
        else if (_cursorY > 0)
        {
            _cursorY--;
            _cursorX = _lines[_cursorY].Length;
        }

        _desiredX = _cursorX;
    }

    private static void MoveRight(bool extendSelection)
    {
        if (extendSelection)
        {
            BeginOrExtendSelection();
        }
        else
        {
            ClearSelection();
        }

        if (_cursorX < _lines[_cursorY].Length)
        {
            _cursorX++;
        }
        else if (_cursorY < _lines.Count - 1)
        {
            _cursorY++;
            _cursorX = 0;
        }

        _desiredX = _cursorX;
    }

    private static void MoveUp(bool extendSelection)
    {
        if (extendSelection)
        {
            BeginOrExtendSelection();
        }
        else
        {
            ClearSelection();
        }

        if (_cursorY > 0)
        {
            _cursorY--;
            _cursorX = Math.Min(_desiredX, _lines[_cursorY].Length);
        }
        else
        {
            _cursorX = 0;
            _desiredX = _cursorX;
        }
    }

    private static void MoveDown(bool extendSelection)
    {
        if (extendSelection)
        {
            BeginOrExtendSelection();
        }
        else
        {
            ClearSelection();
        }

        if (_cursorY < _lines.Count - 1)
        {
            _cursorY++;
            _cursorX = Math.Min(_desiredX, _lines[_cursorY].Length);
        }
        else
        {
            _cursorX = _lines[_cursorY].Length;
            _desiredX = _cursorX;
        }
    }

    #endregion

    #region Help Methods

    private static string[] BuildHelpLines()
    {
        bool isMac = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        bool isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        string mod = isMac ? "CMD" : "CTRL";

        if (isLinux)
        {
            return new[]
            {
                "Shortcuts:",
                "  [ENTER]:        Submit          [SHIFT+ARROWS]: Select",
                "  [SHIFT+ENTER]:  Newline         [BACKSPACE]:    Delete Char",
                "  [ESC]:          Clear Selection [CTRL+R]:       Reset Input",
                "  [CTRL+L]:       Clear Screen    [CTRL+D]:       Exit",
                "  [F1]:           Toggle Help     Clipboard not supported on Linux",
            };
        }

        return new[]
        {
            "Shortcuts:",
            $"  [ENTER]:        Submit          [SHIFT+ARROWS]: Select",
            $"  [SHIFT+ENTER]:  Newline         [{mod}+C]:        Copy",
            $"  [ESC]:          Clear Selection [{mod}+X]:        Cut",
            $"  [{mod}+L]:       Clear Screen    [{mod}+V]:        Paste",
            $"  [{mod}+R]:       Reset Input     [BACKSPACE]:    Delete Char",
            $"  [{mod}+D]:       Exit            [F1]:           Toggle Help",
        };
    }

    #endregion
}
