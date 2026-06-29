namespace Chow.Repl;

interface IInputReceiver
{
    public bool TryGetNextInput(out ReceivedInput input);
}