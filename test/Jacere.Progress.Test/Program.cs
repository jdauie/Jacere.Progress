using Jacere.Progress;
using Jacere.Progress.Test;

await MediaScanner.DoThing();

// todo: rebuild WriterContext; figure out bugs on resize

// todo: make it an option whether it tries to preserve/clear

//Console.CursorVisible = false;

//// deal with powershell width (-1) later

//// test the buffer running out
//Console.BufferHeight = Console.WindowHeight;


////var top = Console.CursorTop;
////Console.SetCursorPosition(0, top);

////Console.WriteLine("".PadRight(usableWidth, 'x'));
////Console.WriteLine("".PadRight(usableWidth, 'y'));

//////Console.WindowWidth -= 5;
//////Console.BufferWidth -= 5;

//for (var i = 0; i < Console.BufferHeight - 1; i++)
//{
//    Console.WriteLine($"pre {i}");
//}

//{
//    using var context = new WriterContext();
//    var cancellationTokenSource = new CancellationTokenSource();

//    var task = ProgressLoop(context, cancellationTokenSource.Token);

//    Console.ReadLine();

//    cancellationTokenSource.Cancel();
//    await task;
//}

//async Task ProgressLoop(WriterContext context, CancellationToken token)
//{
//    var i = 0;

//    while (!token.IsCancellationRequested)
//    {
//        using var scope = context.Scope();

//        var width = Console.WindowWidth;
//        var usableWidth = width - 1;
        
//        context.Write($"[{i}] line 1 [top: {Console.CursorTop}]".PadRight(usableWidth, 'X') + "\n", ConsoleColor.DarkCyan);
//        context.Write($"[{i}] line 2 [top: {Console.CursorTop}]".PadRight(usableWidth, 'Y') + "\n", ConsoleColor.Blue);
//        context.Write($"[{i}] line 3 [top: {Console.CursorTop}]".PadRight(usableWidth, 'Z'), ConsoleColor.DarkYellow);

//        ++i;

//        try
//        {
//            await Task.Delay(TimeSpan.FromMilliseconds(100), token);
//        }
//        catch (OperationCanceledException)
//        {
//        }
//    }
//}

//class WriterContext2 : IAsyncDisposable
//{
//    private int _cursorTop;

//    public WriterContext2()
//    {
//        _cursorTop = Console.CursorTop;

//        Console.CursorVisible = false;
//    }

//    public async Task Write(string block)
//    {
//        var buffer = block.ReplaceLineEndings("\n");
        
//        foreach (var c in buffer)
//        {
//            if (c == '\n')
//            {
//                var cursorTop = Console.CursorTop;
//                //Console.SetCursorPosition();
//                //Console.OpenStandardOutput();

//                Console.WriteLine();
//                //output.WriteLine($" [CursorTop: {Console.CursorTop}, _cursorTop: {_cursorTop}]");
                
//                if (cursorTop == Console.CursorTop)
//                {
//                    // if ending a line doesn't change CursorTop, we are at the bottom of a full buffer
//                    --cursorTop;
//                }
//                //Console.SetCursorPosition(0, 0);

//            }
//            else
//            {
//                Console.Write(c);
//            }
//        }
        
//        Console.SetCursorPosition(0, _cursorTop);
//    }

//    //public async Task WriteLine(string s)
//    //{
//    //    return await Write();
//    //}

//    public ValueTask DisposeAsync()
//    {
//        Console.CursorVisible = true;

//        return ValueTask.CompletedTask;
//    }
//}