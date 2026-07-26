namespace Jacere.Progress;

public class Progress<T> : Progress, IProgressCounter<T>
{
    private readonly IProgressCounter<T> _counter;

    internal Progress(IProgressCounter<T> counter) : base(counter)
    {
        _counter = counter;
    }

    public T? CurrentValue => _counter.CurrentValue;

    public void Increment(T current) => _counter.Increment(current);

    public void Add(long count, T current) => _counter.Add(count, current);

    public void Set(long count, T current) => _counter.Set(count, current);

    public void Set(T current) => _counter.Set(current);

    public IProgressCounter<T> SetCurrentValueFormatter(Func<T, TextLine> formatter) => _counter.SetCurrentValueFormatter(formatter);
}