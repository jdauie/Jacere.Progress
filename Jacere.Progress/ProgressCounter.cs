using ByteSizeLib;

namespace Jacere.Progress;

public class ProgressCounter : IProgressCounter
{
    // todo: move these
    public static Func<long, CounterStyle, TextLine> DefaultValueFormatter = (v, s) => new TextLine().Add($"{v:n0}", s);
    public static Func<long, CounterStyle, TextLine> BinaryByteSizeFormatter = (v, s) => new TextLine().Add(ByteSize.FromBytes(v).ToBinaryString(), s);

    public static Func<string, CounterStyle, TextLine> DefaultNameFormatter = (v, s) => new TextLine().Add(v, s);
    public static Func<string, CounterStyle, TextLine> ScopedNameFormatter = (v, s) =>
    {
        var i = v.IndexOf(':');
        if (i == -1)
        {
            return new TextLine()
                .Add(v, s);
        }

        return new TextLine()
            .Add(v[..i], CounterStyle.Priority1)
            .Add(":", CounterStyle.Priority3)
            .Add(v[(i + 1)..], s);
    };
    
    public string Name { get; }

    private long _current;
    private Func<ProgressCounter, TextLine> _nameFormatter;
    private Func<bool, TextLine> _valueFormatter;

    public long Current => _current;

    public DateTime Start { get; }
    public long? Total { get; private set; }
    public bool IsPersistent { get; private set; }

    public ProgressCounter(string name)
    {
        Name = name;
        Start = DateTime.UtcNow;
        _nameFormatter = CreateNameFormatter(DefaultNameFormatter);
        _valueFormatter = CreateValueFormatter(DefaultValueFormatter);
    }

    public ProgressCounter SetTotal(long total)
    {
        Total = total;
        return this;
    }

    public ProgressCounter SetNameFormatter(Func<string, CounterStyle, TextLine> formatter)
    {
        _nameFormatter = CreateNameFormatter(formatter);
        return this;
    }
    
    public ProgressCounter SetValueFormatter(Func<long, CounterStyle, TextLine> formatter)
    {
        _valueFormatter = CreateValueFormatter(formatter);
        return this;
    }

    public void Persist()
    {
        IsPersistent = true;
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

    protected virtual TextLine FormatValue(Func<long, CounterStyle, TextLine> formatter, bool includeTotalIfAvailable)
    {
        var line = new TextLine()
            .Add(formatter(Current, CounterStyle.Priority1));

        if (includeTotalIfAvailable && Total.HasValue)
        {
            line.Add(" of ", CounterStyle.Priority3);
            line.Add(formatter(Total.Value, CounterStyle.Priority2));
        }

        return line;
    }

    private static Func<ProgressCounter, TextLine> CreateNameFormatter(Func<string, CounterStyle, TextLine> formatter)
    {
        return c => new TextLine()
            .Add(formatter(c.Name, CounterStyle.ProgressName));
    }

    private Func<bool, TextLine> CreateValueFormatter(Func<long, CounterStyle, TextLine> formatter)
    {
        return includeTotalIfAvailable => FormatValue(formatter, includeTotalIfAvailable);
    }
}