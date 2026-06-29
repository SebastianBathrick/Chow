namespace Chow.Repl;

class ConsoleInputReceiver : IInputReceiver
{
    public bool TryGetNextInput(out ReceivedInput input)
    {
        var key = Console.ReadKey(intercept: true);

        switch (key.Key)
        {
            case ConsoleKey.Escape:
                input = new(InputType.None);
                return false;

            case ConsoleKey.Backspace:
                input = new(InputType.Backspace);
                return true;

            case ConsoleKey.Delete:
                input = new(InputType.Delete);
                return true;

            case ConsoleKey.LeftArrow:
                input = new(InputType.Left);
                return true;

            case ConsoleKey.RightArrow:
                input = new(InputType.Right);
                return true;

            case ConsoleKey.UpArrow:
                input = new(InputType.Up);
                return true;

            case ConsoleKey.DownArrow:
                input = new(InputType.Down);
                return true;

            case ConsoleKey.Enter:
                input = new(InputType.Newline);
                return true;

            case ConsoleKey.Tab:
                input = new(InputType.AppendChar, '\t');
                return true;

            default:
                if (key.KeyChar >= 32 && key.KeyChar <= 127)
                    input = new(InputType.AppendChar, key.KeyChar);
                else
                    input = new(InputType.None);
                return true;
        }
    }
}
