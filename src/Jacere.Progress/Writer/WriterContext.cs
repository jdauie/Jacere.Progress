namespace Jacere.Progress.Writer;

public class WriterContext : IDisposable
{
    // todo: this "-1" is to deal with powershell console
    public int WindowWidth => Console.WindowWidth - 1;

    private int _cursorTop;
    private int _lines;

    public WriterContext()
    {
        _cursorTop = Console.CursorTop;
        Console.CursorVisible = false;
    }

    public WriterContextScope Scope() => new (this);

    public void Reset()
    {
        var top = Console.CursorTop;

        Console.SetCursorPosition(0, _cursorTop);

        _lines = top - _cursorTop;
    }

    public void Clear()
    {
        var top = Console.CursorTop;

        var lines = top - _cursorTop;
        var remainingLines = _lines - lines;

        for (var i = 0; i < remainingLines; i++)
        {
            // todo: what was the point of this? in case the output got shorter, I guess? EDIT: YES
            Console.WriteLine("".PadRight(WindowWidth));
        }

        Console.SetCursorPosition(0, top);

        _lines = 0;
    }

    private static void WriteValue(string value, ConsoleColor? foreground, ConsoleColor? background)
    {
        var currentForeground = Console.ForegroundColor;
        var currentBackground = Console.BackgroundColor;

        if (foreground.HasValue)
        {
            Console.ForegroundColor = foreground.Value;
        }

        if (background.HasValue)
        {
            Console.BackgroundColor = background.Value;
        }

        Console.Write(value);

        Console.ForegroundColor = currentForeground;
        Console.BackgroundColor = currentBackground;
    }

    public void Write(string? value, ConsoleColor? foreground = null, ConsoleColor? background = null)
    {
        if (value == null)
        {
            return;
        }

        // todo: detect change in console width during this operation?

        // I think this logic was to make it work when it hit the end of the visible console (EDIT: NO, IT'S WHEN THE BUFFER IS FULL)
        // but it's breaking when the window gets resized

        // but even with it turned off, there is breakage occasionally,
        // with extra chars being left in the last column, and as if the line breaks are getting lost?

        var top = Console.CursorTop;

        WriteValue(value, foreground, background);

        var lineBreaks = value.Length - value.Replace("\n", "").Length;

        var newTop = Console.CursorTop;

        var newPreviousTop = newTop - lineBreaks;

        var topDiff = top - newPreviousTop;

        _cursorTop -= topDiff;
    }

    public void WriteLine(string value)
    {
        Write(value);
        Write("\n");
    }

    public void Dispose()
    {
        Console.CursorVisible = true;

        GC.SuppressFinalize(this);
    }
}