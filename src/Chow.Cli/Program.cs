using Chow.Interpreter;

const string LINE_INDICATOR = ">>> ";
const string LINE_SEPERATOR = "\n";

bool isRunning = true;
int startTop = 0;
int cursorX = 0;
int cursorY = 0;
List<string> lines = new() { string.Empty };

do
{
    if (!ReadEditableBlock(out string sourceCode))
    {
        isRunning = false;
        continue;
    }

    lines.Add(string.Empty);

    try
    {
        ChowInstance instance = new ChowInstance();
        var returnValue = instance.Run(sourceCode);
        Console.WriteLine(returnValue);
        Console.WriteLine(instance.GetVariableDebugInfo());
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex);
    }
}
while (isRunning);

return 0;

bool ReadEditableBlock(out string sourceCode)
{
    for (int i = 0; i < lines.Count; i++)
        Console.WriteLine();
    startTop = Console.CursorTop - lines.Count;
    cursorY = lines.Count - 1;
    cursorX = lines[cursorY].Length;

    while (true)
    {
        Draw();

        ConsoleKeyInfo key = Console.ReadKey(true);

        switch (key.Key)
        {
            case ConsoleKey.Enter:
                if (!key.Modifiers.HasFlag(ConsoleModifiers.Shift))
                {
                    Console.SetCursorPosition(0, startTop + lines.Count);
                    Console.WriteLine();

                    sourceCode = string.Join(LINE_SEPERATOR, lines);
                    return true;
                }

                NewLine();
                break;

            case ConsoleKey.Escape:
                Console.SetCursorPosition(0, startTop + lines.Count);
                Console.WriteLine();
                sourceCode = string.Empty;
                return false;

            case ConsoleKey.LeftArrow:
                MoveLeft();
                break;

            case ConsoleKey.RightArrow:
                MoveRight();
                break;

            case ConsoleKey.UpArrow:
                MoveUp();
                break;

            case ConsoleKey.DownArrow:
                MoveDown();
                break;

            case ConsoleKey.Backspace:
                Backspace();
                break;

            default:
                if (!char.IsControl(key.KeyChar))
                {
                    InsertChar(key.KeyChar);
                }
                break;
        }
    }
}

void Draw()
{
    for (int i = 0; i < lines.Count; i++)
    {
        Console.SetCursorPosition(0, startTop + i);

        string text = LINE_INDICATOR + lines[i];
        Console.Write(text);

        int remaining = Console.WindowWidth - text.Length;

        if (remaining > 0)
        {
            Console.Write(new string(' ', remaining));
        }
    }

    Console.SetCursorPosition(LINE_INDICATOR.Length + cursorX, startTop + cursorY);
}

void InsertChar(char c)
{
    lines[cursorY] = lines[cursorY].Insert(cursorX, c.ToString());
    cursorX++;
}

void NewLine()
{
    string current = lines[cursorY];

    string before = current.Substring(0, cursorX);
    string after = current.Substring(cursorX);

    lines[cursorY] = before;
    lines.Insert(cursorY + 1, after);

    cursorY++;
    cursorX = 0;
}

void Backspace()
{
    if (cursorX > 0)
    {
        lines[cursorY] = lines[cursorY].Remove(cursorX - 1, 1);
        cursorX--;
    }
    else if (cursorY > 0)
    {
        cursorX = lines[cursorY - 1].Length;
        lines[cursorY - 1] += lines[cursorY];
        lines.RemoveAt(cursorY);
        cursorY--;
    }
}

void MoveLeft()
{
    if (cursorX > 0)
    {
        cursorX--;
    }
    else if (cursorY > 0)
    {
        cursorY--;
        cursorX = lines[cursorY].Length;
    }
}

void MoveRight()
{
    if (cursorX < lines[cursorY].Length)
    {
        cursorX++;
    }
    else if (cursorY < lines.Count - 1)
    {
        cursorY++;
        cursorX = 0;
    }
}

void MoveUp()
{
    if (cursorY > 0)
    {
        cursorY--;
        cursorX = Math.Min(cursorX, lines[cursorY].Length);
    }
}

void MoveDown()
{
    if (cursorY < lines.Count - 1)
    {
        cursorY++;
        cursorX = Math.Min(cursorX, lines[cursorY].Length);
    }
}

/*
// We will loop just for testing purposes in Visual Studio so we dont need to restart the program every time we want to test a new file or source code input. In production, this would likely be a one-time execution of a file or source code input and then the program would exit.
while (true)
{
    Console.WriteLine("Enter a file path (with extension or path separator) or inline source code:");
    Console.WriteLine("Escapes are supported: \\n, \\r, \\t, \\f, \\\\.");
    Console.Write("> ");
    string? input = Console.ReadLine();

    if (input == null)
    {
        break;
    }

    try
    {
        string sourceCode;
        if (LooksLikeFilePath(input))
        {
            sourceCode = ReadFileContents(input);
            sourceCode = DecodeEscapes(sourceCode);
        }
        else
        {
            sourceCode = DecodeEscapes(input);
        }
        ChowInstance instance = new ChowInstance();
        instance.Run(sourceCode);
        Console.WriteLine(instance.GetVariableDebugInfo());
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex);
    }
}

static bool LooksLikeFilePath(string input)
{
    return input.Contains('/') || input.Contains('\\') || Path.HasExtension(input);
}

static string ReadFileContents(string filePath)
{
    if (string.IsNullOrWhiteSpace(filePath))
        throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

    if (!string.Equals(Path.GetExtension(filePath), ".chw", StringComparison.OrdinalIgnoreCase))
        throw new ArgumentException($"File must have a .chw extension: {filePath}", nameof(filePath));

    string fullPath = Path.GetFullPath(filePath);

    if (!File.Exists(fullPath))
        throw new FileNotFoundException($"File not found at path: {fullPath}", fullPath);

    FileInfo fileInfo = new FileInfo(fullPath);
    if (fileInfo.Length == 0)
        throw new InvalidOperationException($"File is empty: {fullPath}");

    return File.ReadAllText(fullPath);
}

static string DecodeEscapes(string input)
{
    var decoded = new System.Text.StringBuilder(input.Length);
    bool isEscaped = false;

    foreach (char currentChar in input)
    {
        if (!isEscaped)
        {
            if (currentChar == '\\')
            {
                isEscaped = true;
                continue;
            }

            decoded.Append(currentChar);
            continue;
        }

        switch (currentChar)
        {
            case 'n':
                decoded.Append('\n');
                break;

            case 'r':
                decoded.Append('\r');
                break;

            case 't':
                decoded.Append('\t');
                break;

            case 'f':
                decoded.Append('\f');
                break;

            case '\\':
                decoded.Append('\\');
                break;

            default:
                decoded.Append('\\');
                decoded.Append(currentChar);
                break;
        }

        isEscaped = false;
    }

    if (isEscaped)
    {
        decoded.Append('\\');
    }

    return decoded.ToString();
}

// NOTE: This file is temporary test/development code.
// Chow.Cli will not have direct access to internal Chow library functionality.
*/
