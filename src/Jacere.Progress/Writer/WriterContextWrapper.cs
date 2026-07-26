namespace Jacere.Progress.Writer;

public class WriterContextWrapper(WriterContext context, ConsoleColor? foreground, ConsoleColor? background)
{
    public void Write(string? value)
    {
        context.Write(value, foreground, background);
    }
}