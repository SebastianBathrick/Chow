namespace Chow.Interpreter.Values
{
    public abstract class ChowValue
    {
        public static ChowValue None => ChowNone.Instance;

        public bool IsNone => this == None;

        public abstract DataType As<DataType>() where DataType : struct;

        public abstract bool Is<DataType>() where DataType : struct;


        public abstract override string ToString();
    }
}

