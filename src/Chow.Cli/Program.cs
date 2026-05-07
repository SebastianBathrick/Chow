using Chow.Interpreter;

const char NEWLINE_CHAR = '\n';

ChowModule module = new ChowModule();

while (true)
{
    try
    {
        var srcCode = string.Join(NEWLINE_CHAR, GetLineList());
        module.Execute(srcCode);
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex);
    }
}

static List<string> GetLineList()
{
    // Starts at the beginning of line 1
    var lines = new List<string>() { string.Empty };
    var currLine = 0;
    var cursorY = Console.CursorTop;
    var cursorX = 0;

    ConsoleKeyInfo keyInfo;
    bool isSubmitting = false;

    do
    {

        // Do not display the key in the console window until after its evaluated
        keyInfo = Console.ReadKey(intercept: true);

        switch(keyInfo.Key)
        {
            case ConsoleKey.Enter:
                cursorX = 0;

                if (keyInfo.Modifiers == ConsoleModifiers.Shift)
                {
                    // ENTER + SHIFT = move cursor to newline and add a new line to the list
                    lines.Add(string.Empty);
                    currLine++;
                }
                else
                {
                    // ENTER = move cursor to new line
                    isSubmitting = true;
                }

                cursorY++;
                break;

            case ConsoleKey.L:
                Console.Clear();
                cursorY = cursorY - Console.CursorTop;
                
                for(int i = 0; i < lines.Count; i++)
                {
                    RedrawLine(lines[i], cursorY);
                }
                break;

            case ConsoleKey.Backspace:
                if (cursorX <= 0)
                {
                    break;
                }

                // Set to the intended position after the character is being deleted
                cursorX--;
                var backspacedLine = lines[currLine].Remove(cursorX , 1);

                RedrawLine(backspacedLine, cursorY);

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
                    RedrawLine(insertedLine, cursorY);
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
                    RedrawLine(insertedLine, cursorY);
                    lines[currLine] = insertedLine;
                }

                cursorX++;
                break;
        }

        Console.SetCursorPosition(cursorX, cursorY);
    }
    while (!isSubmitting);

    return lines;
}

static void RedrawLine(string line, int cursorY)
{
    // Set cursor before loop to account line.Length = 1
    Console.SetCursorPosition(0, cursorY);
    var redrawCursorX = 1;

    while (redrawCursorX < line.Length)
    {
        Console.SetCursorPosition(redrawCursorX, cursorY);

        var posDrawChar = line[redrawCursorX++];
        Console.Write(posDrawChar);
    }

    // Delete the trailing char
    Console.Write(' ');
}