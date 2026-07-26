using System.Collections.Immutable;
using System.IO.Enumeration;

namespace Jacere.Progress.Test;

internal class MediaScanner
{
    public static async Task DoThing()
    {
        var dirsToScan = new Dictionary<string, Func<string, bool>>
        {
            { @"M:\movies", _ => true },
            { @"M:\tv", _ => true }
        };

        await Steps();

        foreach (var (dir, filter) in dirsToScan)
        {
            await Scan(dir, filter);
        }

        // todo: add datetime formatting options (e.g. local vs utc)

        // todo: track the last N durations for time estimation?  This is tricky because they might be Add(K)/Set(K) instead of increment
        // Set(K) to a lower value would just have to turn off estimation I suppose

        // todo: nested counters? or does that not add anything?
    }

    static async Task Steps()
    {
        const int scale = 2;

        await using var progress = Progress.Unknown("db:table.event");

        progress.Hide();

        progress
            .Step("sort input")
            .SetValueFormatter(c => new TextLine().Add(c.IsComplete ? "done" : "...", c.Style));

        await Task.Delay(TimeSpan.FromSeconds(1 * scale));

        var count = 100;
        progress.Step("load temp", count).Primary();

        for (var i = 0; i < count; i++)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(10 * scale));

            progress.Increment();
        }

        var count2 = 150;
        progress.Step("copy", count2).Primary();

        for (var i = 0; i < count2; i++)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(10 * scale));

            progress.Increment();
        }

        progress
            .Step("sort result")
            .SetValueFormatter(c => new TextLine().Add(c.IsComplete ? "done" : "...", c.Style));

        await Task.Delay(TimeSpan.FromSeconds(1 * scale));
    }

    static async Task Scan(string dir, Func<string, bool> filter)
    {
        var entries = EnumerateFileSystemEntries(dir, SearchOption.TopDirectoryOnly)
            .ToImmutableList();
        //var entriesSize = entries.Sum(x => x.Length);

        await using var progress = Progress.Known($"scan:{dir}", entries.Count);

        progress.SetNameFormatter(ProgressCounter.ScopedNameFormatter);

        progress.Counter("size")
            //.SetTotal(entriesSize)
            .SetValueFormatter(ProgressCounter.BinaryByteSizeFormatter).Persist();

        progress.Counter<FileSystemEntry2>("files")
            .SetCurrentValueFormatter(f => new TextLine().Add(Path.GetFileName(f.FullPath), CounterStyle.Priority2));

        foreach (var entry in entries)
        {
            if (!filter(entry.FullPath))
            {
                continue;
            }

            progress.Increment();

            var supportedExtensions = new HashSet<string> { ".mp4", ".avi", ".mkv" };

            if (entry.IsDirectory)
            {
                progress.Counter("directories").Increment();

                var nestedEntries = EnumerateFileSystemEntries(entry.FullPath, SearchOption.AllDirectories);

                foreach (var nestedEntry in nestedEntries.Where(x => !x.IsDirectory))
                {
                    progress.Counter("nested files").Increment();
                    progress.Counter("size").Add(nestedEntry.Length);

                    var ext = Path.GetExtension(nestedEntry.FullPath).ToLowerInvariant();
                    //progress.Counter($"file:{ext[1..]}").Increment();
                }
            }
            else
            {
                progress.Counter<FileSystemEntry2>("files").Increment(entry);
                progress.Counter("size").Add(entry.Length);

                var ext = Path.GetExtension(entry.FullPath).ToLowerInvariant();
                //progress.Counter($"file:{ext[1..]}").Increment();

                if (supportedExtensions.Contains(ext))
                {
                    var file = TagLib.File.Create(entry.FullPath);
                    var titleShouldBeUpdated = !string.IsNullOrEmpty(file.Tag.Title) &&
                                               file.Tag.Title != Path.GetFileNameWithoutExtension(entry.FullPath);

                    if (titleShouldBeUpdated)
                    {
                        progress.Counter("update title").Increment();
                    }
                }
            }

            await Task.Delay(TimeSpan.FromMilliseconds(10));
            //await Task.Delay(TimeSpan.FromSeconds(10000));
        }
    }

    static IEnumerable<FileSystemEntry2> EnumerateFileSystemEntries(
        string directory,
        SearchOption options)
    {
        var options2 = FromSearchOption(options);
        return new FileSystemEnumerable<FileSystemEntry2>(directory,
            (ref FileSystemEntry entry) =>
                new FileSystemEntry2(entry.ToSpecifiedFullPath(), entry.IsDirectory, entry.Length), options2)
        {
            ShouldIncludePredicate = (ref FileSystemEntry _) => true
        };
    }

    static EnumerationOptions FromSearchOption(SearchOption searchOption)
    {
        if (searchOption != SearchOption.TopDirectoryOnly && searchOption != SearchOption.AllDirectories)
        {
            throw new ArgumentOutOfRangeException(nameof(searchOption));
        }

        return new EnumerationOptions
        {
            RecurseSubdirectories = searchOption == SearchOption.AllDirectories,
            MatchType = MatchType.Win32,
            AttributesToSkip = FileAttributes.None,
            IgnoreInaccessible = false
        };
    }

    record FileSystemEntry2(string FullPath, bool IsDirectory, long Length);
}