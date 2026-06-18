interface IRenderer
{
    protected Cursor2D Cursor2D { get; }

    protected List<int> LineLengths { get; }

    public int AreaWidth { get; }
    public int AreaHeight { get; }

    void UpdateLine(Vector2D displacement, string line);
}
