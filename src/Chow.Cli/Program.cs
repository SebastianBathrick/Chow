using System;
using System.Text.RegularExpressions;
using Chow.Interpreter;
using Chow.Interpreter.Values;
using Chow.Repl;

const char NEWLINE_CHAR = '\n';
const string START_INDICATOR = ">>> ";
const string TRAILING_INDICATOR = "... ";
const string FILE_PATH_PATTERN = @"^(?:(?:[A-Za-z]:[\\/])|(?:\\\\[^\\/:*?""<>|\r\n]+\\[^\\/:*?""<>|\r\n]+[\\/]?)|[\\/])?(?:[^\\/:*?""<>|\r\n]+[\\/])*[^\\/:*?""<>|\r\n]+$";
const string REQUIRED_EXTENSION = ".chw";

ChowModule module = new ChowModule();

module["print"] = new ChowDynamic((ChowValue val) =>
{
    Console.WriteLine(val);
    return ChowValue.None;
});

module["input"] = new ChowDynamic(() =>
{
    string? input = Console.ReadLine();

    if (input == null)
    {
        input = string.Empty;
    }

    return new ChowStr(input);
});

module["float"] = new ChowDynamic((ChowValue val) =>
{
    if (val.Is<float>())
    {
        return new ChowFloat(val.As<float>());
    }
    if (val.Is<int>())
    {
        return new ChowFloat((float)val.As<int>());
    }
    if (val.Is<bool>())
    {
        return new ChowFloat(val.As<float>());
    }
    if (val is ChowStr s)
    {
        if (float.TryParse(s.Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float parsed))
        {
            return new ChowFloat(parsed);
        }
        throw new InvalidOperationException($"could not convert string to float: '{s.Value}'");
    }
    throw new InvalidOperationException($"float() argument must be a string or a number, not '{ChowTypeName(val)}'");
});

module["str"] = new ChowDynamic((ChowValue val) =>
{
    return new ChowStr(val.ToString());
});

module["int"] = new ChowDynamic((ChowValue val) =>
{
    if (val.Is<int>())
    {
        return new ChowInt(val.As<int>());
    }
    if (val.Is<float>())
    {
        return new ChowInt((int)val.As<float>());
    }
    if (val.Is<bool>())
    {
        return new ChowInt(val.As<int>());
    }
    if (val is ChowStr s)
    {
        if (int.TryParse(s.Value, out int parsed))
        {
            return new ChowInt(parsed);
        }
        throw new InvalidOperationException($"invalid literal for int() with base 10: '{s.Value}'");
    }
    throw new InvalidOperationException($"int() argument must be a string, a bytes-like object or a real number, not '{ChowTypeName(val)}'");
});

module["bool"] = new ChowDynamic((ChowValue val) =>
{
    if (val.IsNone)
    {
        return new ChowBool(false);
    }
    if (val.Is<bool>())
    {
        return new ChowBool(val.As<bool>());
    }
    if (val.Is<int>())
    {
        return new ChowBool(val.As<int>() != 0);
    }
    if (val.Is<float>())
    {
        return new ChowBool(val.As<float>() != 0f);
    }
    if (val is ChowStr s)
    {
        return new ChowBool(s.Value.Length != 0);
    }
    throw new InvalidOperationException($"bool() argument not supported for type '{ChowTypeName(val)}'");
});

module["len"] = new ChowDynamic((ChowValue val) =>
{
    if (val is ChowStr s)
    {
        return new ChowInt(s.Value.Length);
    }
    throw new InvalidOperationException($"object of type '{ChowTypeName(val)}' has no len()");
});

module["type"] = new ChowDynamic((ChowValue val) =>
{
    return new ChowStr(ChowTypeName(val));
});

module["abs"] = new ChowDynamic((ChowValue val) =>
{
    if (val.Is<int>())
    {
        return (ChowValue)new ChowInt(Math.Abs(val.As<int>()));
    }
    if (val.Is<float>())
    {
        return (ChowValue)new ChowFloat(Math.Abs(val.As<float>()));
    }
    if (val.Is<bool>())
    {
        return (ChowValue)new ChowInt(val.As<int>());
    }
    throw new InvalidOperationException($"bad operand type for abs(): '{ChowTypeName(val)}'");
});

module["round"] = new ChowDynamic((ChowValue val) =>
{
    if (val.Is<int>())
    {
        return new ChowInt(val.As<int>());
    }
    if (val.Is<float>())
    {
        return new ChowInt((int)Math.Round((double)val.As<float>(), MidpointRounding.ToEven));
    }
    if (val.Is<bool>())
    {
        return new ChowInt(val.As<int>());
    }
    throw new InvalidOperationException($"type {ChowTypeName(val)} doesn't define __round__ method");
});

module["min"] = new ChowDynamic((ChowValue[] args) =>
{
    if (args.Length != 2)
    {
        throw new InvalidOperationException($"min() expected 2 arguments, got {args.Length}");
    }
    if (!ChowIsNumeric(args[0]) || !ChowIsNumeric(args[1]))
    {
        throw new InvalidOperationException("min() arguments must be numbers");
    }
    return ChowAsDouble(args[0]) <= ChowAsDouble(args[1]) ? args[0] : args[1];
});

module["max"] = new ChowDynamic((ChowValue[] args) =>
{
    if (args.Length != 2)
    {
        throw new InvalidOperationException($"max() expected 2 arguments, got {args.Length}");
    }
    if (!ChowIsNumeric(args[0]) || !ChowIsNumeric(args[1]))
    {
        throw new InvalidOperationException("max() arguments must be numbers");
    }
    return ChowAsDouble(args[0]) >= ChowAsDouble(args[1]) ? args[0] : args[1];
});

if (args.Length > 0)
{
    string arg = args[0];

    if (Regex.IsMatch(arg, FILE_PATH_PATTERN))
    {
        if (!arg.EndsWith(REQUIRED_EXTENSION, StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine($"Error: file must have '{REQUIRED_EXTENSION}' extension.");
            return;
        }

        try
        {
            string src = File.ReadAllText(arg);
            module.Execute(src);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
        }

        return;
    }

    module.AddHook(new PrintExprStatementHook());

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
    bool isSubmitting = false;

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

                for (int i = 0; i < lines.Count; i++)
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

static string ChowTypeName(ChowValue val)
{
    if (val.IsNone)
    {
        return "NoneType";
    }
    if (val.Is<bool>())
    {
        return "bool";
    }
    if (val.Is<int>())
    {
        return "int";
    }
    if (val.Is<float>())
    {
        return "float";
    }
    if (val is ChowStr)
    {
        return "str";
    }
    if (val is ChowDynamic d && d.Value != null)
    {
        return d.Value.GetType().Name;
    }
    return "object";
}

static bool ChowIsNumeric(ChowValue val)
{
    return val.Is<int>() || val.Is<float>() || val.Is<bool>();
}

static double ChowAsDouble(ChowValue val)
{
    if (val.Is<int>())
    {
        return val.As<int>();
    }
    if (val.Is<float>())
    {
        return val.As<float>();
    }
    if (val.Is<bool>())
    {
        return val.As<int>();
    }
    throw new InvalidOperationException("Value is not numeric");
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