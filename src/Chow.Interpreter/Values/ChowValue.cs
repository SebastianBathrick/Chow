namespace Chow.Interpreter.Values
{
    public abstract class ChowValue
    {
        public static ChowValue None => ChowNone.Instance;

        public bool IsNone => this == None;

        // TODO: Refactor AsType<T>() and IsType<T>() to use a nullable type
        public abstract TDataType AsType<TDataType>() where TDataType : struct;

        public abstract bool IsType<TDataType>() where TDataType : struct;


        // TODO: Remove this overide because AsType<string>() serves the same purpose and their will be less code
        public abstract override string ToString();
    }
}

