class Cursor2D
{
   
    readonly int _leftmost;
    readonly int _rightmost;

    readonly int _top;
    readonly int _bottom;

    public int AreaWidth => _rightmost + 1;

    public int AreaHeight => _bottom + 1;
    
    public int X { get; private set; }

    public int Y { get; private set; }

    public Vector2D Position => new(X, Y);


    public bool IsTop => Y == _top;

    public bool IsBottom => Y == _bottom;
    
    public bool IsLeftmost => X == _leftmost;

    public bool IsRightmost => X == _rightmost;
    
    public bool IsTopLeft => IsTop && IsLeftmost;

    public bool IsBottomRight => IsBottom && IsRightmost;

    public Cursor2D(
        int areaWidth = int.MaxValue,
        int areaHeight = int.MaxValue,
        int leftPadding = 0,
        int topPadding = 0)
    {
        if (areaWidth < 1 || areaHeight < 1)
        {
            throw new ArgumentException("Area dimensions must be at least 1.");
        }

        if (leftPadding < 0 || topPadding < 0)
        {
            throw new ArgumentException("Padding values cannot be negative.");
        }

        // Padding must leave at least one usable cell in each dimension.
        // Valid X range is [leftPadding, areaWidth - 1], so require leftPadding <= areaWidth - 1.
        if (leftPadding > areaWidth - 1)
        {
            throw new ArgumentException(
                $"leftPadding ({leftPadding}) leaves no usable width in area of {areaWidth}.");
        }

        if (topPadding > areaHeight - 1)
        {
            throw new ArgumentException(
                $"topPadding ({topPadding}) leaves no usable height in area of {areaHeight}.");
        }

        _leftmost = leftPadding;
        _rightmost = areaWidth - 1;
        _top = topPadding;
        _bottom = areaHeight - 1;

        // Start at the padded origin, inside the valid bounds.
        X = _leftmost;
        Y = _top;
    }

    public virtual int MoveDown()
    {

        if (!IsBottom)
        {
            Y++;
        }

        return Y;
    }

    public virtual int MoveUp()
    {
        if (!IsTop)
        {
            Y--;
        }

        return Y;
    }

    public virtual int MoveLeft()
    {
        if (!IsLeftmost)
        {
            X--;
        }

        return X;
    }

    public virtual int MoveRight()
    {
        if (!IsRightmost)
        {
            X++;
        }

        return X;
    }
    
    public int JumpUp(int jmpPosCount)
    {
        while (jmpPosCount-- > 0)
        {
            MoveUp();
        }

        return Y;
    }

    public int JumpDown(int posCount)
    {
        while (posCount-- > 0)
        {
            MoveDown();
        }

        return Y;
    }
    
    public int JumpRight(int posCount)
    {
        while (posCount-- > 0)
        {
            MoveRight();
        }

        return X;
    }
    
    public int JumpLeft(int posCount)
    {
        while (posCount-- > 0)
        {
            MoveLeft();
        }

        return X;
    }
    
    public int JumpToLeftmost()
    {
        while (!IsLeftmost)
        {
            MoveLeft();
        }
        
        return X;
    }

    public int JumpToRightmost()
    {
        while (!IsRightmost)
        {
            MoveRight();
        }
        
        return X;
    }


    public int JumpToTop()
    {
        while (!IsTop)
        {
            MoveUp();
        }
        
        return Y;
    }

    public int JumpToBottom()
    {
        while (!IsBottom)
        {
            MoveDown();
        }
        
        return Y;
    }

    public void JumpToTopLeft()
    {
        JumpToLeftmost();
        JumpToTop();
    }

    // Moves by a relative displacement, composing the directional jumps: X shifts
    // the column (right when positive, left when negative) and Y shifts the row
    // (down when positive, up when negative). Each leg is clamped to the bounds.
    public void Jump(Vector2D displacement)
    {
        if (displacement.X > 0)
        {
            JumpRight(displacement.X);
        }
        else if (displacement.X < 0)
        {
            JumpLeft(-displacement.X);
        }

        if (displacement.Y > 0)
        {
            JumpDown(displacement.Y);
        }
        else if (displacement.Y < 0)
        {
            JumpUp(-displacement.Y);
        }
    }

    public bool TryMoveUp()
{
    int before = Y;
    return MoveUp() != before;
}

public bool TryMoveDown()
{
    int before = Y;
    return MoveDown() != before;
}

public bool TryMoveLeft()
{
    int before = X;
    return MoveLeft() != before;
}

public bool TryMoveRight()
{
    int before = X;
    return MoveRight() != before;
}

    public override string ToString() => $"({X}, {Y})";
}