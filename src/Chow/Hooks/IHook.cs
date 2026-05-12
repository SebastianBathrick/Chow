namespace Chow.Interpreter.Hooks
{
    public interface IHook
    {
        void Invoke(object value = null);
    }
}
