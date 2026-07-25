namespace Jacere.Progress;

public class ProgressCounter<T> : ProgressCounter, IProgressCounter<T>
{
    private Func<T, TextLine>? _currentFormatter;

    public ProgressCounter(string name) : base(name)
    {
    }

    public T? CurrentValue { get; private set; }

    public void Increment(T current)
    {
        Increment();
        CurrentValue = current;
    }

    public void Add(long count, T current)
    {
        Add(count);
        CurrentValue = current;
    }

    public void Set(long count, T current)
    {
        Set(count);
        CurrentValue = current;
    }

    public void Set(T current)
    {
        CurrentValue = current;
    }

    public IProgressCounter<T> SetCurrentValueFormatter(Func<T, TextLine> formatter)
    {
        _currentFormatter = formatter;
        return this;
    }

    protected override TextLine FormatValue(Func<long, CounterStyle, TextLine> formatter, bool includeTotalIfAvailable)
    {
        var line = base.FormatValue(formatter, includeTotalIfAvailable);

        if (includeTotalIfAvailable && CurrentValue != null && _currentFormatter != null)
        {
            line.Add(" ");
            line.Add(_currentFormatter(CurrentValue));
        }

        return line;
    }
}