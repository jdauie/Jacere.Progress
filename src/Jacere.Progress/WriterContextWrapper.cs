namespace Jacere.Progress;

public class WriterContextWrapper
{
    private readonly WriterContext _context;
    private readonly ConsoleColor? _foreground;
    private readonly ConsoleColor? _background;

    public WriterContextWrapper(WriterContext context, ConsoleColor? foreground, ConsoleColor? background)
    {
        _context = context;
        _foreground = foreground;
        _background = background;
    }

    public void Write(string? value)
    {
        _context.Write(value, _foreground, _background);
    }
}