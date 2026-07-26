namespace Jacere.Progress.Counter;

public interface IProgressCounter<T> : IProgressCounter
{
    T? CurrentValue { get; }
    void Increment(T current);
    void Add(long count, T current);
    void Set(long count, T current);
    void Set(T current);
    IProgressCounter<T> SetCurrentValueFormatter(Func<T, TextLine> formatter);
}