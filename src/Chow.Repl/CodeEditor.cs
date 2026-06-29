namespace Chow.Repl;

class CodeEditor : ICodeEditor
{
    readonly IRenderer _renderer;
    readonly IInputReceiver _inputReceiver;
    readonly TextArea _renderText;
    readonly TextArea _srcText;

    // Our own view of the cursor. It shares the renderer's area (and padding)
    // with the cursors inside the TextAreas, so it moves in lockstep with them.
    // Reading it lets us report how far the cursor travelled between edits
    // without TextArea having to expose any cursor state.
    readonly Cursor2D _cursor;
    readonly bool _doesWrap;

    public CodeEditor(IRenderer renderer, IInputReceiver inputReceiver, int indentScale = 1, bool doesWrap = true)
    {
        _renderer = renderer;
        _inputReceiver = inputReceiver;
        _doesWrap = doesWrap;

        // The source text will be EXACTLY what the input receiver provides, without any formatting
        _srcText = new TextArea(renderer.AreaWidth, renderer.AreaHeight, doesWrap: false);

        // The render text will be formatted with the given indent scale and wrapping
        _renderText = new TextArea(renderer.AreaWidth, renderer.AreaHeight, indentScale, doesWrap);

        _cursor = new Cursor2D(renderer.AreaWidth, renderer.AreaHeight);
    }

    public string GetTextSubmission()
    {
        // Paint the initial (empty) line; the cursor sits at the origin, so this
        // first frame carries no displacement.
        _renderer.UpdateLine(default, _renderText.CurrentLine);

        while (_inputReceiver.TryGetNextInput(out var input))
        {
            var from = _cursor.Position;

            Apply(input);

            _renderer.UpdateLine(_cursor.Position - from, _renderText.CurrentLine);
        }

        return _srcText.ToString();
    }

    // Applies a single input to both text models and mirrors the resulting cursor
    // move onto our own cursor, so the next displacement is measured correctly.
    // Each cursor move is gated on the render model's success, so edits that no-op
    // at a boundary leave the cursor (and the reported displacement) untouched.
    void Apply(ReceivedInput input)
    {
        switch (input.Type)
        {
            case InputType.AppendChar:
                if (input.Value is char c)
                {
                    // A char typed at the right edge wraps onto a new line below
                    // when wrapping is on; otherwise it's inserted in place.
                    var wraps = _cursor.IsRightmost && _doesWrap;
                    var lengthBefore = _renderText.CurrentLine.Length;

                    _srcText.TryAddChar(c);
                    if (_renderText.TryAddChar(c))
                    {
                        if (wraps)
                        {
                            _cursor.Jump(new Vector2D(0, 1));
                            SnapToLineEnd();
                        }
                        else
                        {
                            // Advance by however many characters were inserted:
                            // one normally, or a run of spaces for indentation.
                            var inserted = _renderText.CurrentLine.Length - lengthBefore;
                            _cursor.Jump(new Vector2D(inserted, 0));
                        }
                    }
                }
                break;

            case InputType.Backspace:
                _srcText.TryRemoveChar();
                if (_renderText.TryRemoveChar())
                {
                    _cursor.Jump(new Vector2D(-1, 0));
                }
                break;

            case InputType.Left:
                _srcText.TryMoveLeft();
                if (_renderText.TryMoveLeft())
                {
                    _cursor.Jump(new Vector2D(-1, 0));
                }
                break;

            case InputType.Right:
                _srcText.TryMoveRight();
                if (_renderText.TryMoveRight())
                {
                    _cursor.Jump(new Vector2D(1, 0));
                }
                break;

            case InputType.Newline:
                _srcText.TryInsertLine();
                if (_renderText.TryInsertLine())
                {
                    _cursor.Jump(new Vector2D(0, 1));
                    SnapToLineEnd();
                }
                break;

            case InputType.Up:
                _srcText.TryPrevLine();
                if (_renderText.TryPrevLine())
                {
                    _cursor.Jump(new Vector2D(0, -1));
                    SnapToLineEnd();
                }
                break;

            case InputType.Down:
                _srcText.TryNextLine();
                if (_renderText.TryNextLine())
                {
                    _cursor.Jump(new Vector2D(0, 1));
                    SnapToLineEnd();
                }
                break;

            case InputType.None:
                break;
        }
    }

    // Snaps our cursor to the end of the current line. Used after moves that land
    // the editing cursor at a line end: vertical navigation, a fresh line, and the
    // new line a wrap spills onto.
    void SnapToLineEnd()
    {
        _cursor.JumpToLeftmost();
        _cursor.JumpRight(_renderText.CurrentLine.Length);
    }
}