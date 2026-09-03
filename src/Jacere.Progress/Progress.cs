using ByteSizeLib;
using Jacere.Progress.Writer;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using Jacere.Progress.Counter;

namespace Jacere.Progress;

public class Progress : IProgressCounter, IAsyncDisposable
{
    public static readonly Func<ValueFormatterContext, TextLine> DefaultValueFormatter = c => new TextLine().Add($"{c.Value:n0}", c.Style);
    public static readonly Func<ValueFormatterContext, TextLine> BinaryByteSizeFormatter = c => new TextLine().Add(ByteSize.FromBytes(c.Value).ToBinaryString(), c.Style);

    public static readonly Func<NameFormatterContext, TextLine> DefaultNameFormatter = c => new TextLine().Add(c.Name, c.Style);
    public static readonly Func<NameFormatterContext, TextLine> ScopedNameFormatter = c =>
    {
        var i = c.Name.IndexOf(':');
        if (i == -1)
        {
            return new TextLine()
                .Add(c.Name, c.Style);
        }

        return new TextLine()
            .Add(c.Name[..i], CounterStyle.Priority1)
            .Add(":", CounterStyle.Priority3)
            .Add(c.Name[(i + 1)..], c.Style);
    };

    private static bool _running;

    private readonly WriterContext _writer;
    private readonly ConsoleProgressBar _progressBar;
    private readonly Task _task;

    private IProgressCounter _current;
    private ImmutableList<IProgressCounter> _counters;
    private readonly ConcurrentDictionary<string, Lazy<IProgressCounter>> _counterLookup = new();
    private bool _persistCounters;
    private bool _disposed;

    private TimeSpan _updateInterval = TimeSpan.FromMilliseconds(50);

    private readonly CancellationTokenSource _source = new();

    public static Progress Known(string name, long count)
    {
        var counter = new ProgressCounter(name);
        var progress = new Progress(counter);
        progress.SetTotal(count);
        return progress;
    }

    public static Counter.Progress<T> Known<T>(string name, long count)
    {
        var counter = new ProgressCounter<T>(name);
        var progress = new Counter.Progress<T>(counter);
        progress.SetTotal(count);
        return progress;
    }

    public static Progress Unknown(string name)
    {
        var counter = new ProgressCounter(name);
        return new Progress(counter);
    }

    public static Counter.Progress<T> Unknown<T>(string name)
    {
        var counter = new ProgressCounter<T>(name);
        return new Counter.Progress<T>(counter);
    }

    public string Name => _current.Name;
    public long Current => _current.Current;
    public DateTime Start => _current.Start;
    public long? Total => _current.Total;
    public bool IsPrimary => _current.IsPrimary;
    public bool IsHidden => _current.IsHidden;
    public bool IsPersistent => _current.IsPersistent;
    public ProgressCounter SetTotal(long total) => _current.SetTotal(total);

    public ProgressCounter SetNameFormatter(Func<NameFormatterContext, TextLine> formatter) =>
        _current.SetNameFormatter(formatter);

    public ProgressCounter SetValueFormatter(Func<ValueFormatterContext, TextLine> formatter) =>
        _current.SetValueFormatter(formatter);

    public IProgressCounter Primary() => _current.Primary();

    public IProgressCounter Hide() => _current.Hide();

    public IProgressCounter Persist() => _current.Persist();

    public TextLine GetFormattedName() => _current.GetFormattedName();

    public TextLine GetFormattedValue(bool includeTotalIfAvailable = true) =>
        _current.GetFormattedValue(includeTotalIfAvailable);

    public void Increment() => _current.Increment();

    public void Add(long count) => _current.Add(count);

    public void Set(long count) => _current.Set(count);

    public void Complete()
    {
        _current.Complete();
    }

    protected Progress(IProgressCounter counter)
    {
        if (Interlocked.Exchange(ref _running, true))
        {
            throw new InvalidOperationException($"Only one instance of {nameof(Progress)} can be created.");
        }

        _current = counter;
        _counters = ImmutableList.Create(counter);
        _counterLookup[counter.Name] = new Lazy<IProgressCounter>(() => counter);
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

        var firstCounter = _counters[0];

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
            .Add($" {progress:P0} ", CounterStyle.Progress)
            .Add(firstCounter.GetFormattedName())
            .Pad()
            .Add($@"(started {Start:yyyy-MM-dd HH\:mm\:ss}Z) ", CounterStyle.Progress2)
            .Add($@"{DateTime.UtcNow - Start:dd\.hh\:mm\:ss} ", CounterStyle.Progress)
            .Write(_writer);

        WriteCounters(false);
    }

    private void WriteCounters(bool persistentOnly)
    {
        // todo: should these be ordered? optionally?
        var counters = _counters
            .Where(x => (!persistentOnly && !x.IsHidden) || (x != this && (x.IsPersistent || _persistCounters)));
        foreach (var counter in counters)
        {
            // todo: save last printed values in case the formatter loses resolution? (so we can indicate an update)
            // really this entails a rethink of the formatter pattern
            // (by "loses resolution" I mean if the console got resized and there is more/less space available)

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

        var firstCounter = _counters[0];

        var primaryCounter = _counters.FirstOrDefault(x => x.IsPrimary) ?? firstCounter;

        new TextLine()
            .Add(firstCounter.GetFormattedName())
            .Add(": ", CounterStyle.Priority3)
            .Add(primaryCounter.GetFormattedValue(false))
            .Add(" in ", CounterStyle.Priority3)
            .Add($@"{DateTime.UtcNow - firstCounter.Start:dd\.hh\:mm\:ss}", CounterStyle.Priority2)
            .Write(_writer);
        
        WriteCounters(true);
    }

    public IProgressCounter Counter(string name)
    {
        var counter = _counterLookup.GetOrAdd(name, x => new Lazy<IProgressCounter>(() =>
        {
            var c = new ProgressCounter(x);
            _counters = _counters.Add(c);
            return c;
        })).Value;

        
        return counter;
    }

    public IProgressCounter<T> Counter<T>(string name)
    {
        var counter = _counterLookup.GetOrAdd(name, x => new Lazy<IProgressCounter>(() =>
        {
            var c = new ProgressCounter<T>(x);
            _counters = _counters.Add(c);
            return c;
        })).Value;

        if (counter is IProgressCounter<T> g)
        {
            return g;
        }

        throw new ArgumentException("counter type mismatch");
    }

    public IProgressCounter Step(string name, long? total = null)
    {
        var counter = Counter(name);
        _current.Complete();
        _current = counter;
        if (total != null)
        {
            _current.SetTotal(total.Value);
        }
        return counter;
    }

    public IProgressCounter<T> Step<T>(string name, long? total = null)
    {
        var counter = Counter<T>(name);
        _current.Complete();
        _current = counter;
        if (total != null)
        {
            _current.SetTotal(total.Value);
        }
        return counter;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        await _source.CancelAsync();
        await _task;

        _source.Dispose();

        WriteComplete();

        _writer.Dispose();

        Interlocked.Exchange(ref _running, false);

        GC.SuppressFinalize(this);

        _disposed = true;
    }
}