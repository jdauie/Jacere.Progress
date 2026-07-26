namespace Jacere.Progress.Counter;

public class ProgressCounter : IProgressCounter
{
    public string Name { get; }

    private long _current;
    private Func<ProgressCounter, TextLine> _nameFormatter;
    private Func<bool, TextLine> _valueFormatter;
    private bool _complete;

    public long Current => _current;

    public DateTime Start { get; }
    public long? Total { get; private set; }
    public bool IsPrimary { get; private set; }
    public bool IsHidden { get; private set; }
    public bool IsPersistent { get; private set; }

    public ProgressCounter(string name)
    {
        Name = name;
        Start = DateTime.UtcNow;
        _nameFormatter = CreateNameFormatter(Progress.DefaultNameFormatter);
        _valueFormatter = CreateValueFormatter(Progress.DefaultValueFormatter);
    }

    public ProgressCounter SetTotal(long total)
    {
        Total = total;
        return this;
    }

    public ProgressCounter SetNameFormatter(Func<NameFormatterContext, TextLine> formatter)
    {
        _nameFormatter = CreateNameFormatter(formatter);
        return this;
    }
    
    public ProgressCounter SetValueFormatter(Func<ValueFormatterContext, TextLine> formatter)
    {
        _valueFormatter = CreateValueFormatter(formatter);
        return this;
    }

    public IProgressCounter Primary()
    {
        IsPrimary = true;
        return this;
    }

    public IProgressCounter Hide()
    {
        IsHidden = true;
        return this;
    }

    public IProgressCounter Persist()
    {
        IsPersistent = true;
        return this;
    }

    public TextLine GetFormattedName()
    {
        return _nameFormatter(this);
    }

    public TextLine GetFormattedValue(bool includeTotalIfAvailable = true)
    {
        return _valueFormatter(includeTotalIfAvailable);
    }

    public void Increment()
    {
        Interlocked.Increment(ref _current);
    }

    public void Add(long count)
    {
        Interlocked.Add(ref _current, count);
    }

    public void Set(long count)
    {
        Interlocked.Exchange(ref _current, count);
    }

    public void Complete()
    {
        _complete = true;
    }

    protected virtual TextLine FormatValue(Func<ValueFormatterContext, TextLine> formatter, bool includeTotalIfAvailable)
    {
        var line = new TextLine()
            .Add(formatter(new ValueFormatterContext(Current, CounterStyle.Priority1, _complete)));

        if (includeTotalIfAvailable && Total.HasValue)
        {
            line.Add(" of ", CounterStyle.Priority3);
            line.Add(formatter(new ValueFormatterContext(Total.Value, CounterStyle.Priority2, _complete)));
        }

        return line;
    }

    private Func<ProgressCounter, TextLine> CreateNameFormatter(Func<NameFormatterContext, TextLine> formatter)
    {
        return c => new TextLine()
            .Add(formatter(new NameFormatterContext(c.Name, CounterStyle.ProgressName, _complete)));
    }

    private Func<bool, TextLine> CreateValueFormatter(Func<ValueFormatterContext, TextLine> formatter)
    {
        return includeTotalIfAvailable => FormatValue(formatter, includeTotalIfAvailable);
    }
}