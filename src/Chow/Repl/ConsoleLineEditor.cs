namespace Chow.Repl
{
    /// <summary>
    /// Reads a single interactive, possibly multi-line, Chow source submission from the console.
    /// </summary>
    internal sealed class ConsoleLineEditor
    {
        const char NEWLINE_CHAR = '\n';

        /// <summary>
        /// Reads one source-code submission using the supplied prompt style.
        /// </summary>
        /// <param name="promptStyle">The prompt text to display for first and continuation lines.</param>
        /// <returns>
        /// The submitted source code, an empty string when the current submission is cancelled, or
        /// <see langword="null"/> when input reaches EOF or the user requests exit from an empty prompt.
        /// </returns>
        public string? ReadSubmission(PromptStyle promptStyle)
        {
            var lines = new List<string> { string.Empty };
            var currentLine = 0;
            var cursorColumn = 0;
            var editorTop = Console.CursorTop;

            RedrawSubmission(lines, editorTop, promptStyle, lines.Count);
            MoveCursorToCurrentLine(editorTop, currentLine, cursorColumn, promptStyle);

            while (true)
            {
                var keyInfo = Console.ReadKey(intercept: true);

                if (IsControlC(keyInfo))
                {
                    WriteCancelledMarker(editorTop, lines.Count);
                    // Ctrl+C at an empty prompt exits the REPL; otherwise it cancels only this submission.
                    return IsSubmissionEmpty(lines) ? null : string.Empty;
                }

                if (IsEndOfInput(keyInfo))
                {
                    MoveToAfterEditor(editorTop, lines.Count);
                    return null;
                }

                switch (keyInfo.Key)
                {
                    case ConsoleKey.Enter:
                        if ((keyInfo.Modifiers & ConsoleModifiers.Shift) == ConsoleModifiers.Shift)
                        {
                            lines.Add(string.Empty);
                            currentLine++;
                            cursorColumn = 0;
                            RedrawLine(string.Empty, editorTop + currentLine, promptStyle.ContinuationIndicator);
                        }
                        else
                        {
                            MoveToAfterEditor(editorTop, lines.Count);
                            return string.Join(NEWLINE_CHAR, lines);
                        }
                        break;

                    case ConsoleKey.L when (keyInfo.Modifiers & ConsoleModifiers.Control) == ConsoleModifiers.Control:
                        Console.Clear();
                        editorTop = Console.CursorTop;
                        // Clearing the terminal invalidates previous row coordinates, so repaint from the new top.
                        RedrawSubmission(lines, editorTop, promptStyle, lines.Count);
                        break;

                    case ConsoleKey.Backspace:
                        DeleteCharacterBeforeCursor(lines, ref currentLine, ref cursorColumn, editorTop, promptStyle);
                        break;

                    case ConsoleKey.LeftArrow:
                        if (cursorColumn > 0)
                        {
                            cursorColumn--;
                        }
                        break;

                    case ConsoleKey.RightArrow:
                        if (cursorColumn < lines[currentLine].Length)
                        {
                            cursorColumn++;
                        }
                        break;

                    case ConsoleKey.Home:
                        cursorColumn = 0;
                        break;

                    case ConsoleKey.End:
                        cursorColumn = lines[currentLine].Length;
                        break;

                    case ConsoleKey.UpArrow:
                    case ConsoleKey.DownArrow:
                        break;

                    case ConsoleKey.Spacebar:
                        InsertCharacter(' ', lines, currentLine, ref cursorColumn, editorTop, promptStyle);
                        break;

                    case ConsoleKey.Tab:
                        InsertCharacter('\t', lines, currentLine, ref cursorColumn, editorTop, promptStyle);
                        break;

                    default:
                        if (!char.IsControl(keyInfo.KeyChar))
                        {
                            InsertCharacter(keyInfo.KeyChar, lines, currentLine, ref cursorColumn, editorTop, promptStyle);
                        }
                        break;
                }

                MoveCursorToCurrentLine(editorTop, currentLine, cursorColumn, promptStyle);
            }
        }

        static void InsertCharacter(
            char character,
            List<string> lines,
            int currentLine,
            ref int cursorColumn,
            int editorTop,
            PromptStyle promptStyle)
        {
            if (!CanInsertCharacter(lines[currentLine], promptStyle))
            {
                WriteBell();
                return;
            }

            lines[currentLine] = lines[currentLine].Insert(cursorColumn, new string(character, 1));
            cursorColumn++;

            var prompt = currentLine == 0 ? promptStyle.StartIndicator : promptStyle.ContinuationIndicator;
            RedrawLine(lines[currentLine], editorTop + currentLine, prompt);
        }

        static void DeleteCharacterBeforeCursor(
            List<string> lines,
            ref int currentLine,
            ref int cursorColumn,
            int editorTop,
            PromptStyle promptStyle)
        {
            if (cursorColumn > 0)
            {
                cursorColumn--;
                lines[currentLine] = lines[currentLine].Remove(cursorColumn, 1);

                var prompt = currentLine == 0 ? promptStyle.StartIndicator : promptStyle.ContinuationIndicator;
                RedrawLine(lines[currentLine], editorTop + currentLine, prompt);
                return;
            }

            if (currentLine == 0)
            {
                return;
            }

            var previousLineLength = lines[currentLine - 1].Length;
            var mergedLine = lines[currentLine - 1] + lines[currentLine];

            if (mergedLine.Length > MaxLineLength(promptStyle))
            {
                WriteBell();
                return;
            }

            var previousLineCount = lines.Count;
            // Backspace at the start of a continuation line joins it to the previous logical line.
            lines[currentLine - 1] = mergedLine;
            lines.RemoveAt(currentLine);
            currentLine--;
            cursorColumn = previousLineLength;

            RedrawSubmission(lines, editorTop, promptStyle, previousLineCount);
        }

        static void RedrawSubmission(List<string> lines, int editorTop, PromptStyle promptStyle, int previousLineCount)
        {
            for (var i = 0; i < lines.Count; i++)
            {
                var prompt = i == 0 ? promptStyle.StartIndicator : promptStyle.ContinuationIndicator;
                RedrawLine(lines[i], editorTop + i, prompt);
            }

            for (var i = lines.Count; i < previousLineCount; i++)
            {
                // When a line is removed, clear its old row so stale continuation text disappears.
                ClearLine(editorTop + i);
            }
        }

        static void RedrawLine(string line, int cursorTop, string prompt)
        {
            MoveCursor(0, cursorTop);
            Console.Write(prompt);
            Console.Write(line);

            var remainingWidth = Console.BufferWidth - prompt.Length - line.Length - 1;
            if (remainingWidth > 0)
            {
                // Clear characters left over from a longer previous rendering of the same row.
                Console.Write(new string(' ', remainingWidth));
            }
        }

        static void ClearLine(int cursorTop)
        {
            MoveCursor(0, cursorTop);

            var clearWidth = Math.Max(0, Console.BufferWidth - 1);
            if (clearWidth > 0)
            {
                Console.Write(new string(' ', clearWidth));
            }
        }

        static void MoveCursorToCurrentLine(int editorTop, int currentLine, int cursorColumn, PromptStyle promptStyle)
        {
            MoveCursor(promptStyle.IndicatorLength + cursorColumn, editorTop + currentLine);
        }

        static void MoveToAfterEditor(int editorTop, int lineCount)
        {
            MoveCursor(0, editorTop + lineCount);
        }

        static void MoveCursor(int left, int top)
        {
            var maxLeft = Math.Max(0, Console.BufferWidth - 1);
            var maxTop = Math.Max(0, Console.BufferHeight - 1);

            Console.SetCursorPosition(
                Math.Max(0, Math.Min(left, maxLeft)),
                Math.Max(0, Math.Min(top, maxTop)));
        }

        static int MaxLineLength(PromptStyle promptStyle)
        {
            return Math.Max(0, Console.BufferWidth - promptStyle.IndicatorLength - 1);
        }

        static bool CanInsertCharacter(string line, PromptStyle promptStyle)
        {
            return line.Length < MaxLineLength(promptStyle);
        }

        static bool IsSubmissionEmpty(List<string> lines)
        {
            for (var i = 0; i < lines.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(lines[i]))
                {
                    return false;
                }
            }

            return true;
        }

        static bool IsControlC(ConsoleKeyInfo keyInfo)
        {
            return keyInfo.Key == ConsoleKey.C
                && (keyInfo.Modifiers & ConsoleModifiers.Control) == ConsoleModifiers.Control;
        }

        static bool IsEndOfInput(ConsoleKeyInfo keyInfo)
        {
            return keyInfo.Key == ConsoleKey.Z
                && (keyInfo.Modifiers & ConsoleModifiers.Control) == ConsoleModifiers.Control;
        }

        static void WriteCancelledMarker(int editorTop, int lineCount)
        {
            MoveToAfterEditor(editorTop, lineCount);
            Console.WriteLine("^C");
        }

        static void WriteBell()
        {
            Console.Write('\a');
        }
    }
}
