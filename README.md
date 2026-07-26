# Jacere.Progress

## Basic Usage

```csharp
async Task Scan(string dir)
{
    var entries = EnumerateFileSystemEntries(dir, SearchOption.TopDirectoryOnly)
        .ToImmutableList();

    await using var progress = Progress.Known($"scan:{dir}", entries.Count);

    foreach (var entry in entries)
    {
        // ...

        progress.Increment();
    }
}
```

## Formatters and Current Items

```csharp
async Task Scan(string dir)
{
    var entries = EnumerateFileSystemEntries(dir, SearchOption.TopDirectoryOnly)
        .ToImmutableList();

    await using var progress = Progress.Known($"scan:{dir}", entries.Count);

    progress.Counter("size")
        .SetValueFormatter(ProgressCounter.BinaryByteSizeFormatter).Persist();

    progress.Counter<FileSystemEntry2>("files")
        .SetCurrentValueFormatter(f => new TextLine().Add(Path.GetFileName(f.FullPath), CounterStyle.Priority2));

    foreach (var entry in entries)
    {
        progress.Increment();

        if (entry.IsDirectory)
        {
            progress.Counter("directories").Increment();
        }
        else
        {
            progress.Counter<FileSystemEntry2>("files").Increment(entry);
            progress.Counter("size").Add(entry.Length);
        }
    }
}
```

## Stepwise progress

```csharp
async Task Steps(IList<string> items)
{
    await using var progress = Progress.Unknown("db:table.event");

    progress.Hide();

    progress.Step("sort")
        .SetValueFormatter(c => new TextLine().Add(c.IsComplete ? "done" : "...", c.Style));

    // ...

    progress.Step("copy", items.Count).Primary();

    foreach (var item in items)
    {
        // ...

        progress.Increment();
    }
}
```