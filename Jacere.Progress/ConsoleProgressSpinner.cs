namespace Jacere.Progress;

public class ConsoleProgressSpinner
{
    private const string ProgressIndicatorChars = @"/-\|";

    private static readonly TimeSpan SpinnerProgressInterval = TimeSpan.FromMilliseconds(100);

    private char _lastProgressIndicator = ProgressIndicatorChars[0];
    private DateTime _lastProgressIndicatorTime;

    public char GetChar()
    {
        if (_lastProgressIndicatorTime.Add(SpinnerProgressInterval) <= DateTime.UtcNow)
        {
            var lastProgressIndicatorIndex = ProgressIndicatorChars.IndexOf(_lastProgressIndicator);
            var nextProgressIndicator = ProgressIndicatorChars[(lastProgressIndicatorIndex + 1) % ProgressIndicatorChars.Length];

            _lastProgressIndicator = nextProgressIndicator;
            _lastProgressIndicatorTime = DateTime.UtcNow;
        }

        return _lastProgressIndicator;
    }
}