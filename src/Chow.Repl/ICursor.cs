namespace Chow.Repl;

public interface ICursor
{
    public int X { get; }
    
    public int Y { get; }
    
    // --- Incremental Movement ---

    // Note: x and y start at 0. (0, 0) represents the beginning of the first line.
    // The minimum x and y values are zero. So if a method attempts to go below those mins
    // return the current axis value
    
    // y++ return new y
    int MoveUp();
    
    // y-- return new y
    int MoveDown();

    // x-- return new x
    int MoveLeft();
    
    // x++ return new x
    int MoveRight();
    
    // --- Jump To Line Position ---
    
    // Sets the maximum x value for the current line, moves to it
    void JumpToColumn(int columnIndex);
    
    // Moves to (0, y), returns 0
    int JumpToFirstColumn();

    // Jumps to (x, 0), returns 0
    int JumpFirstLine();

    // Jumps to (0, 0)
    void JumpToStart();
}
