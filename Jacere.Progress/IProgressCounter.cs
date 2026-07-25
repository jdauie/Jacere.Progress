namespace Jacere.Progress;

public interface IProgressCounter
{
    string Name { get; }
    long Current { get; }
    DateTime Start { get; }
    long? Total { get; }
    bool IsPersistent { get; }
    ProgressCounter SetTotal(long total);
    ProgressCounter SetNameFormatter(Func<string, CounterStyle, TextLine> formatter);
    ProgressCounter SetValueFormatter(Func<long, CounterStyle, TextLine> formatter);
    void Persist();
    TextLine GetFormattedName();
    TextLine GetFormattedValue(bool includeTotalIfAvailable = true);
    void Increment();
    void Add(long count);
    void Set(long count);
}