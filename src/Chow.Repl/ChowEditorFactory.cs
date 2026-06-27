static class ChowEditorFactory
{
    public static ICodeEditor CreateEditor()
    {
        return new CodeEditor(new ConsoleRenderer(), new ConsoleInputReceiver());
    }
}