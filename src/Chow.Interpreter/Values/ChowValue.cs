namespace Chow.Interpreter.Values
{
    public abstract class ChowValue
    {
        public static ChowValue None => ChowNone.Instance;

        public bool IsNone => this == None;

        // TODO: Refactor As<T>() and Is<T>() to use a nullable type
        public abstract TDataType As<TDataType>() where TDataType : struct;

        public abstract bool Is<TDataType>() where TDataType : struct;


        // TODO: Remove this overide because As<string>() serves the same purpose and their will be less code
        public abstract override string ToString();
    }
}

