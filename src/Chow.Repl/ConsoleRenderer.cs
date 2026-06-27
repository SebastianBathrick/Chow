
class ConsoleRenderer : IRenderer
{
    readonly int _areaWidth;
    readonly int _areaHeight;
    readonly Cursor2D _cursor;
    readonly List<int> _lineLengths = new();
    readonly int _originRow;

    public ConsoleRenderer()
    {

        Console.Clear();
        Console.WindowTop = 0;
        Console.WindowLeft = 0;

        _areaWidth = Console.WindowWidth;
        _areaHeight = Console.WindowHeight;
        _cursor = new Cursor2D(_areaWidth, _areaHeight);
        _originRow = Console.CursorTop;
    }

    public int AreaWidth => _areaWidth;
    public int AreaHeight => _areaHeight;

    Cursor2D IRenderer.Cursor2D => _cursor;
    List<int> IRenderer.LineLengths => _lineLengths;

    public void UpdateLine(Vector2D displacement, string line)
    {
        Console.CursorVisible = false;
        _cursor.Jump(displacement);

        while (_lineLengths.Count <= _cursor.Y)
            _lineLengths.Add(0);

        Console.SetCursorPosition(0, _originRow + _cursor.Y);
        Console.Write(line);

        var previousLength = _lineLengths[_cursor.Y];
        if (previousLength > line.Length)
            Console.Write(new string(' ', previousLength - line.Length));

        _lineLengths[_cursor.Y] = line.Length;

        Console.SetCursorPosition(_cursor.X, _originRow + _cursor.Y);
        Console.CursorVisible = true;
    }
}
