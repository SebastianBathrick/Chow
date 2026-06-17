namespace Chow.Repl;

public class Cursor : ICursor
{
    public int X { get; private set; }

    public int Y { get; private set; }

    public int MoveUp()
    {
        Y++;
        return Y;
    }

    public int MoveDown()
    {
        if (Y > 0)
        {
            Y--;
        }

        return Y;
    }

    public int MoveLeft()
    {
        if (X > 0)
        {
            X--;
        }

        return X;
    }

    public int MoveRight()
    {
        X++;
        return X;
    }

    public void JumpToColumn(int columnIndex)
    {
        X = columnIndex < 0 ? 0 : columnIndex;
    }

    public int JumpToFirstColumn()
    {
        X = 0;
        return X;
    }

    public int JumpFirstLine()
    {
        Y = 0;
        return Y;
    }

    public void JumpToStart()
    {
        X = 0;
        Y = 0;
    }
}
