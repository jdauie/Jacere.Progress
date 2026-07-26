namespace Jacere.Progress;

public class TextLine
{
    private readonly List<ITextStyle> _parts;

    public TextLine()
    {
        _parts = new List<ITextStyle>();
    }

    public TextLine Add(TextLine line)
    {
        _parts.AddRange(line._parts);
        return this;
    }

    public TextLine Add(string text, CounterStyle style = CounterStyle.Default)
    {
        _parts.Add(new TextStyle(text, style));
        return this;
    }

    public TextLine Pad(int width, CounterStyle style = CounterStyle.Default)
    {
        _parts.Add(new TextPadding(width: width, style: style));
        return this;
    }

    public TextLine Pad(char fillChar = ' ')
    {
        _parts.Add(new TextPadding(fillChar));
        return this;
    }

    public override string ToString()
    {
        return string.Join("", _parts.OfType<TextStyle>().Select(x => x.Text));
    }

    public void Write(WriterContext writer)
    {
        var partsWidth = _parts.Sum(x => x.Width ?? 0);

        if (partsWidth > writer.WindowWidth)
        {
            var partsByLength = _parts
                .OfType<TextStyle>()
                .OrderByDescending(x => x.Text.Length)
                .ToList();
            
            foreach (var part in partsByLength)
            {
                if (partsWidth - part.Text.Length < writer.WindowWidth)
                {
                    var maxPartWidth = writer.WindowWidth - partsWidth + part.Text.Length;
                    var replace = maxPartWidth > 3
                        ? part.Text[..(maxPartWidth - 3)] + "..."
                        : "";
                    var i = _parts.IndexOf(part);
                    _parts[i] = new TextStyle(replace, part.Style);
                    break;
                }
                else
                {
                    _parts.Remove(part);
                    partsWidth -= part.Width!.Value;
                }
            }
        }

        var paddingWidth = Math.Max(writer.WindowWidth - partsWidth, 0);

        // todo: multiple paddings should probably be an exception
        var padding = _parts
            .OfType<TextPadding>()
            .FirstOrDefault(x => !x.Width.HasValue);

        if (padding != null)
        {
            padding.Width = paddingWidth;
        }
        else if (paddingWidth > 0)
        {
            padding = new TextPadding(width: paddingWidth);

            _parts.Add(padding);
        }

        foreach (var part in _parts)
        {
            ConsoleColor? background = null;
            ConsoleColor? foreground = null;

            switch (part.Style)
            {
                case CounterStyle.Default:
                    break;
                case CounterStyle.ProgressBar:
                    background = ConsoleColor.Yellow;
                    break;
                case CounterStyle.Progress:
                    foreground = ConsoleColor.Yellow;
                    break;
                case CounterStyle.Progress2:
                    foreground = ConsoleColor.DarkYellow;
                    break;
                case CounterStyle.Priority1:
                    foreground = ConsoleColor.White;
                    break;
                case CounterStyle.Priority2:
                    foreground = ConsoleColor.Gray;
                    break;
                case CounterStyle.Priority3:
                    foreground = ConsoleColor.DarkGray;
                    break;
                case CounterStyle.ProgressName:
                    foreground = ConsoleColor.Green;
                    break;
                case CounterStyle.Category1:
                    foreground = ConsoleColor.Cyan;
                    break;
                case CounterStyle.Category2:
                    foreground = ConsoleColor.DarkRed;
                    break;
                case CounterStyle.Category3:
                    foreground = ConsoleColor.DarkMagenta;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var w = new WriterContextWrapper(writer, foreground, background);

            if (part is TextStyle text)
            {
                w.Write(text.Text);
            }
            else if (part is TextPadding p)
            {
                w.Write("".PadRight(p.Width!.Value, p.FillChar));
            }
        }

        writer.WriteLine("");
    }

    private interface ITextStyle
    {
        CounterStyle Style { get; }
        int? Width { get; }
    }

    private class TextStyle : ITextStyle
    {
        public string Text { get; }
        public CounterStyle Style { get; }
        public int? Width => Text.Length;

        public TextStyle(string text, CounterStyle style = CounterStyle.Default)
        {
            Text = text;
            Style = style;
        }
    }

    private class TextPadding : ITextStyle
    {
        public char FillChar { get; }
        public int? Width { get; set; }
        public CounterStyle Style { get; }

        public TextPadding(char fillChar = ' ', int? width = null, CounterStyle style = CounterStyle.Default)
        {
            FillChar = fillChar;
            Width = width;
            Style = style;
        }
    }
}