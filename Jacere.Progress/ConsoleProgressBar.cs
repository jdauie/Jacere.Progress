namespace Jacere.Progress;

public class ConsoleProgressBar
{
    private const char ProgressBackgroundChar = '\u2593';
    private const string ProgressIndicatorChars = @"/-\|";

    private static readonly TimeSpan SpinnerProgressInterval = TimeSpan.FromMilliseconds(100);

    private char _lastProgressIndicator = ProgressIndicatorChars[0];
    private DateTime _lastProgressIndicatorTime;

    public TextLine GetLine(double progress, WriterContext writer)
    {
        var progressWidth = (int)Math.Min(progress * writer.WindowWidth, writer.WindowWidth - 1);

        if (_lastProgressIndicatorTime.Add(SpinnerProgressInterval) <= DateTime.UtcNow)
        {
            var lastProgressIndicatorIndex = ProgressIndicatorChars.IndexOf(_lastProgressIndicator);
            var nextProgressIndicator = ProgressIndicatorChars[(lastProgressIndicatorIndex + 1) % ProgressIndicatorChars.Length];

            _lastProgressIndicator = nextProgressIndicator;
            _lastProgressIndicatorTime = DateTime.UtcNow;
        }

        var progressIndicator = _lastProgressIndicator.ToString();

        return new TextLine()
            .Pad(progressWidth, CounterStyle.ProgressBar)
            .Add(progressIndicator, CounterStyle.Progress)
            .Pad(ProgressBackgroundChar);
    }
}