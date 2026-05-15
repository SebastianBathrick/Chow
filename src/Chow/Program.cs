using System.Text.RegularExpressions;
using Chow.Interpreter;

const char NEWLINE_CHAR = '\n';
const string START_INDICATOR = ">>> ";
const string TRAILING_INDICATOR = "... ";
const string FILE_PATH_PATTERN = @"^(?:(?:[A-Za-z]:[\\/])|(?:\\\\[^\\/:*?""<>|\r\n]+\\[^\\/:*?""<>|\r\n]+[\\/]?)|[\\/])?(?:[^\\/:*?""<>|\r\n]+[\\/])*[^\\/:*?""<>|\r\n]+$";
const string REQUIRED_EXTENSION = ".chw";

var module = new ChowModule();

if (args.Length > 0)
{
    var arg = args[0];

    if (Regex.IsMatch(arg, FILE_PATH_PATTERN))
    {
        if (!arg.EndsWith(REQUIRED_EXTENSION, StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine($"Error: file must have '{REQUIRED_EXTENSION}' extension.");
            return;
        }

        try
        {
            var src = File.ReadAllText(arg);
            module.Execute(src);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
        }

        return;
    }


    try
    {
        module.Execute(arg);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine(ex);
    }

    return;
}

while (true)
{
    try
    {
        var srcCode = string.Join(NEWLINE_CHAR, GetLineList(START_INDICATOR, TRAILING_INDICATOR));
        module.Execute(srcCode);
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex);
    }
}

static List<string> GetLineList(string startIndicator, string trailingIndicator)
{
    if (startIndicator.Length != trailingIndicator.Length)
    {
        throw new ArgumentException(nameof(trailingIndicator));
    }

    var prefixLength = startIndicator.Length;

    // Starts at the beginning of line 1
    var lines = new List<string>() { string.Empty };
    var currLine = 0;
    var cursorY = Console.CursorTop;
    var cursorX = 0;

    Console.SetCursorPosition(0, cursorY);
    Console.Write(startIndicator);

    ConsoleKeyInfo keyInfo;
    var isSubmitting = false;

    do
    {

        // Do not display the key in the console window until after its evaluated
        keyInfo = Console.ReadKey(intercept: true);

        switch (keyInfo.Key)
        {
            case ConsoleKey.Enter:
                cursorX = 0;

                if (keyInfo.Modifiers == ConsoleModifiers.Shift)
                {
                    // ENTER + SHIFT = move cursor to newline and add a new line to the list
                    lines.Add(string.Empty);
                    currLine++;
                    cursorY++;
                    Console.SetCursorPosition(0, cursorY);
                    Console.Write(trailingIndicator);
                }
                else
                {
                    // ENTER = move cursor to new line
                    isSubmitting = true;
                    cursorY++;
                }
                break;

            case ConsoleKey.L when keyInfo.Modifiers == ConsoleModifiers.Control:
                Console.Clear();
                cursorY = currLine;

                for (var i = 0; i < lines.Count; i++)
                {
                    var redrawPrefix = i == 0 ? startIndicator : trailingIndicator;
                    RedrawLine(lines[i], i, redrawPrefix);
                }
                break;

            case ConsoleKey.Backspace:
                if (cursorX <= 0)
                {
                    break;
                }

                // Set to the intended position after the character is being deleted
                cursorX--;
                var backspacedLine = lines[currLine].Remove(cursorX, 1);

                RedrawLine(backspacedLine, cursorY, currLine == 0 ? startIndicator : trailingIndicator);

                lines[currLine] = backspacedLine;
                break;

            case ConsoleKey.LeftArrow:
                if (cursorX > 0)
                {
                    cursorX--;
                }
                break;

            case ConsoleKey.RightArrow:
                if (cursorX < lines[currLine].Length)
                {
                    cursorX++;
                }
                break;
            case ConsoleKey.Spacebar:
            case ConsoleKey.Tab:
                if (cursorX == lines[currLine].Length)
                {
                    lines[currLine] += keyInfo.KeyChar;
                    Console.Write(keyInfo.KeyChar);
                    cursorX++;
                }
                else
                {
                    var insertedLine = lines[currLine].Insert(cursorX, keyInfo.KeyChar.ToString());
                    RedrawLine(insertedLine, cursorY, currLine == 0 ? startIndicator : trailingIndicator);
                    lines[currLine] = insertedLine;
                    cursorX++;
                }
                break;

            default:
                if (char.IsControl(keyInfo.KeyChar))
                {
                    break;
                }

                if (cursorX == lines[currLine].Length)
                {
                    lines[currLine] += keyInfo.KeyChar;
                    Console.Write(keyInfo.KeyChar);
                }
                else
                {
                    var insertedLine = lines[currLine].Insert(cursorX, keyInfo.KeyChar.ToString());
                    RedrawLine(insertedLine, cursorY, currLine == 0 ? startIndicator : trailingIndicator);
                    lines[currLine] = insertedLine;
                }

                cursorX++;
                break;
        }

        Console.SetCursorPosition(cursorX + prefixLength, cursorY);
    }
    while (!isSubmitting);

    return lines;
}

static void RedrawLine(string line, int cursorY, string prefix)
{
    Console.SetCursorPosition(0, cursorY);
    Console.Write(prefix);

    var redrawCursorX = 0;

    while (redrawCursorX < line.Length)
    {
        Console.SetCursorPosition(redrawCursorX + prefix.Length, cursorY);

        var posDrawChar = line[redrawCursorX++];
        Console.Write(posDrawChar);
    }

    // Delete the trailing char
    Console.Write(' ');
}
