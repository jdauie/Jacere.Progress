namespace Jacere.Progress;

public class WriterContextScope : IDisposable
{
    private readonly WriterContext _writer;

    public WriterContextScope(WriterContext writer)
    {
        _writer = writer;
        _writer.Reset();
    }

    public void Dispose()
    {
        _writer.Clear();
    }
}