using System.Text;

namespace Chow.Repl;

class TextArea
{
    const int TabSpaceCount = 4;
    const int MaxAsciiValue = 127;

    readonly Cursor2D _cursor;
    readonly List<string> _lines;
    readonly int _indentScale;
    readonly bool _doesWrap;

    public string CurrentLine
    {
        get => _lines[_cursor.Y];
        private set => _lines[_cursor.Y] = value;
    }

    public TextArea(
        int areaWidth,
        int areaHeight,
        int indentScale = 1,
        bool doesWrap = true)
    {
        if (indentScale < 0)
        {
            throw new ArgumentException($"{nameof(indentScale)} must be at least 0.");
        }

        _indentScale = indentScale;
        _doesWrap = doesWrap;
        _lines = [string.Empty];
        _cursor = new Cursor2D(areaWidth, areaHeight);
    }

    public bool TryAddChar(char c, Vector2D displacement = default)
    {
        if (!IsAsciiChar(c))
        {
            return false;
        }

        // Leading indentation: expand a tab/space at the start of an otherwise
        // blank line into the configured number of spaces.
        if (IsWhitespaceChar(c) && string.IsNullOrWhiteSpace(CurrentLine))
        {
            var spaceCount = c == '\t' ? _indentScale * TabSpaceCount : _indentScale;

            for (int i = 0; i < spaceCount; i++)
            {
                if (!TryAddNonWhitespace(' '))
                {
                    return false;
                }
            }

            _cursor.Jump(displacement);
            return true;
        }

        if (!TryAddNonWhitespace(c))
        {
            return false;
        }

        _cursor.Jump(displacement);
        return true;
    }

    // Backspace: remove the character immediately to the left of the cursor.
    // Insertion leaves the cursor just past the last typed character, so the
    // target lives at column X - 1. Only the current line is affected; if the
    // cursor is at the left edge (nothing behind it on this line), nothing is
    // removed and the cursor is left untouched. displacement nudges the cursor
    // after a successful removal.
    public bool TryRemoveChar(Vector2D displacement = default)
    {
        if (_cursor.IsLeftmost)
        {
            return false;
        }

        var removeAt = _cursor.X - 1;

        if (removeAt >= CurrentLine.Length)
        {
            return false;
        }

        CurrentLine = CurrentLine.Remove(removeAt, 1);
        _cursor.MoveLeft();
        _cursor.Jump(displacement);
        return true;
    }

    bool TryAddNonWhitespace(char c)
    {
        if (!TryReserveColumn())
        {
            return false;
        }

        // Insert at the cursor's column rather than appending, so typing
        // mid-line pushes existing characters right.
        CurrentLine = CurrentLine.Insert(_cursor.X, c.ToString());
        _cursor.TryMoveRight();
        return true;
    }

    // A column is available if we're not at the right edge, or if wrapping is
    // enabled and we can start a fresh line below. Short-circuits: TryInsertLine
    // only runs when we're actually at the right edge.
    bool TryReserveColumn() => !_cursor.IsRightmost || (_doesWrap && TryInsertLine());

    public bool TryInsertLine()
    {
        if (_cursor.IsBottom)
        {
            return false;
        }

        // The new line will live at _cursor.Y + 1; never insert past the end.
        var insertAt = _cursor.Y + 1;

        if (insertAt > _lines.Count)
        {
            return false;
        }

        if (!_cursor.TryMoveDown())
        {
            return false;
        }

        _lines.Insert(_cursor.Y, string.Empty);
        MoveToValidColumn();
        return true;
    }

    public bool TryPrevLine()
    {
        if (_cursor.IsTop)
        {
            return false;
        }

        _cursor.MoveUp();
        MoveToValidColumn();
        return true;
    }

    public bool TryNextLine()
    {
        if (_cursor.IsBottom)
        {
            return false;
        }

        // Only materialize a new line if one doesn't already exist below.
        if (_cursor.Y + 1 >= _lines.Count)
        {
            _lines.Add(string.Empty);
        }

        _cursor.MoveDown();
        MoveToValidColumn();
        return true;
    }

    public bool TryMoveLeft() => _cursor.TryMoveLeft();

    public bool TryMoveRight()
    {
        // Stop at the end of the line's content: the cursor may sit one column
        // past the last character (the next insert point) but never beyond it,
        // which would put inserts out of range.
        if (_cursor.X >= CurrentLine.Length)
        {
            return false;
        }

        return _cursor.TryMoveRight();
    }

    void MoveToValidColumn()
    {
        _cursor.JumpToLeftmost();
        _cursor.JumpRight(CurrentLine.Length);
    }

    public override string ToString() => string.Join('\n', _lines);

    static bool IsWhitespaceChar(char c) => c == '\t' || c == ' ';

    static bool IsAsciiChar(char c) => c <= MaxAsciiValue;
}