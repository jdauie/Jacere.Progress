namespace Jacere.Progress.Writer;

public class ConsoleProgressBar
{
    private const char ProgressBackgroundChar = '\u2593';

    private readonly ConsoleProgressSpinner _spinner = new();

    public TextLine GetLine(double progress, WriterContext writer)
    {
        var progressWidth = (int)Math.Min(progress * writer.WindowWidth, writer.WindowWidth - 1);

        var progressIndicator = _spinner.GetChar().ToString();

        return new TextLine()
            .Pad(progressWidth, CounterStyle.ProgressBar)
            .Add(progressIndicator, CounterStyle.Progress)
            .Pad(ProgressBackgroundChar);
    }
}