using System.Collections.Concurrent;

namespace Jacere.Progress;

public class Progress : IProgressCounter, IAsyncDisposable
{
    private readonly WriterContext _writer;
    private readonly ConsoleProgressBar _progressBar;
    private readonly Task _task;

    private readonly IProgressCounter _counter;
    private readonly ConcurrentDictionary<string, IProgressCounter> _counters = new();
    private bool _persistCounters;

    private TimeSpan _updateInterval = TimeSpan.FromMilliseconds(50);
    //private TimeSpan _updateInterval = TimeSpan.FromMinutes(100);

    private readonly CancellationTokenSource _source = new();

    public static Progress Known(string name, long count)
    {
        var counter = new ProgressCounter(name);
        var progress = new Progress(counter);
        progress.SetTotal(count);
        return progress;
    }

    public static Progress<T> Known<T>(string name, long count)
    {
        var counter = new ProgressCounter<T>(name);
        var progress = new Progress<T>(counter);
        progress.SetTotal(count);
        return progress;
    }

    public static Progress Unknown(string name)
    {
        var counter = new ProgressCounter(name);
        return new Progress(counter);
    }

    public static Progress<T> Unknown<T>(string name)
    {
        var counter = new ProgressCounter<T>(name);
        return new Progress<T>(counter);
    }

    public string Name => _counter.Name;
    public long Current => _counter.Current;
    public DateTime Start => _counter.Start;
    public long? Total => _counter.Total;
    public bool IsPersistent => _counter.IsPersistent;
    public ProgressCounter SetTotal(long total) => _counter.SetTotal(total);

    public ProgressCounter SetNameFormatter(Func<string, CounterStyle, TextLine> formatter) =>
        _counter.SetNameFormatter(formatter);

    public ProgressCounter SetValueFormatter(Func<long, CounterStyle, TextLine> formatter) =>
        _counter.SetValueFormatter(formatter);

    public void Persist() => _counter.Persist();

    public TextLine GetFormattedName() => _counter.GetFormattedName();

    public TextLine GetFormattedValue(bool includeTotalIfAvailable = true) =>
        _counter.GetFormattedValue(includeTotalIfAvailable);

    public void Increment() => _counter.Increment();

    public void Add(long count) => _counter.Add(count);

    public void Set(long count) => _counter.Set(count);

    protected Progress(IProgressCounter counter)
    {
        _counter = counter;
        _writer = new WriterContext();
        _progressBar = new ConsoleProgressBar();
        _task = UpdateDisplay(_source.Token);
    }

    public Progress SetUpdateInterval(TimeSpan delay)
    {
        _updateInterval = delay;
        return this;
    }
    
    // todo: worth keeping?
    public Progress PersistCounters()
    {
        _persistCounters = true;
        return this;
    }

    private async Task UpdateDisplay(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            WriteProgress();

            try
            {
                await Task.Delay(_updateInterval, token);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }

    private void WriteProgress()
    {
        using var _ = _writer.Scope();

        var progress = 0.0;
        var count = Total;
        if (count.HasValue)
        {
            progress = count.Value == 0
                ? 1
                : Math.Min((double) Current / count.Value, 1.0);
        }

        new TextLine()
            .Add(_progressBar.GetLine(progress, _writer))
            .Write(_writer);

        new TextLine()
            .Add($" {progress:P} ", CounterStyle.Progress)
            .Add(GetFormattedName())
            .Pad()
            .Add($@"(started {Start:yyyy-MM-dd HH\:mm\:ss}Z) ", CounterStyle.Progress2)
            .Add($@"{DateTime.UtcNow - Start:dd\.hh\:mm\:ss} ", CounterStyle.Progress)
            .Write(_writer);

        WriteCounters(false);
    }

    private void WriteCounters(bool persistentOnly)
    {
        // todo: should these be ordered? optionally?
        var counters = new[] { _counter }
            .Concat(_counters.Values)
            .Where(x => !persistentOnly || (x != this && (x.IsPersistent || _persistCounters)));
        foreach (var counter in counters)
        {
            // todo: save last printed values in case the formatter loses resolution? (so we can indicate an update)
            // really this entails a rethink of the formatter pattern

            new TextLine()
                .Add($"  {counter.Name}", CounterStyle.Priority2)
                .Add(": ", CounterStyle.Priority3)
                .Add(counter.GetFormattedValue(!persistentOnly))
                .Write(_writer);
        }
    }

    private void WriteComplete()
    {
        using var _ = _writer.Scope();

        new TextLine()
            .Add(GetFormattedName())
            .Add(": ", CounterStyle.Priority3)
            .Add(GetFormattedValue(false))
            .Add(" in ", CounterStyle.Priority3)
            .Add($@"{DateTime.UtcNow - Start:dd\.hh\:mm\:ss}", CounterStyle.Priority2)
            .Write(_writer);
        
        WriteCounters(true);
    }

    public IProgressCounter Counter(string name)
    {
        return _counters.GetOrAdd(name, x => new ProgressCounter(x));
    }

    public IProgressCounter<T> Counter<T>(string name)
    {
        var counter = _counters.GetOrAdd(name, x => new ProgressCounter<T>(x));

        if (counter is IProgressCounter<T> g)
        {
            return g;
        }

        throw new ArgumentException("counter type mismatch");
    }

    public async ValueTask DisposeAsync()
    {
        await _source.CancelAsync();
        await _task;

        _source.Dispose();

        WriteComplete();

        _writer.Dispose();
    }
}