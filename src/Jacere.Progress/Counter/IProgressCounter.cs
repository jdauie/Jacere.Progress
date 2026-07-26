namespace Jacere.Progress.Counter;

public interface IProgressCounter
{
    string Name { get; }
    long Current { get; }
    DateTime Start { get; }
    long? Total { get; }
    bool IsPrimary { get; }
    bool IsHidden { get; }
    bool IsPersistent { get; }
    ProgressCounter SetTotal(long total);
    ProgressCounter SetNameFormatter(Func<NameFormatterContext, TextLine> formatter);
    ProgressCounter SetValueFormatter(Func<ValueFormatterContext, TextLine> formatter);
    IProgressCounter Primary();
    IProgressCounter Hide();
    IProgressCounter Persist();
    TextLine GetFormattedName();
    TextLine GetFormattedValue(bool includeTotalIfAvailable = true);
    void Increment();
    void Add(long count);
    void Set(long count);
    void Complete();
}