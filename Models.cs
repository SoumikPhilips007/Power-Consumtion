using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace TimestampCalculator;

public class TimestampResult
{
    private static readonly SolidColorBrush GreenBrush  = new(Color.FromRgb(0x16, 0xA3, 0x4A));
    private static readonly SolidColorBrush GrayBrush   = new(Color.FromRgb(0x9C, 0xA3, 0xAF));
    private static readonly SolidColorBrush RedBrush    = new(Color.FromRgb(0xDC, 0x26, 0x26));

    public string    Label    { get; init; } = string.Empty;
    public string    RawValue { get; init; } = string.Empty;
    public DateTime? Parsed   { get; init; }

    public bool IsValid => Parsed.HasValue;
    public bool IsEmpty => string.IsNullOrWhiteSpace(RawValue);

    public string StatusText =>
        IsValid  ? "Valid"   :
        IsEmpty  ? "Empty"   :
                   "Invalid";

    public Brush StatusColor =>
        IsValid  ? GreenBrush :
        IsEmpty  ? GrayBrush  :
                   RedBrush;

    public string ParsedIso =>
        Parsed.HasValue ? Parsed.Value.ToString("o") : string.Empty;

    public Visibility HasRawValue =>
        !string.IsNullOrEmpty(RawValue) ? Visibility.Visible : Visibility.Collapsed;

    public Visibility HasParsed =>
        IsValid ? Visibility.Visible : Visibility.Collapsed;
}

public class ValueResult
{
    private static readonly SolidColorBrush GreenBrush = new(Color.FromRgb(0x16, 0xA3, 0x4A));
    private static readonly SolidColorBrush GrayBrush  = new(Color.FromRgb(0x9C, 0xA3, 0xAF));
    private static readonly SolidColorBrush RedBrush   = new(Color.FromRgb(0xDC, 0x26, 0x26));

    public string  Label    { get; init; } = string.Empty;
    public string  RawValue { get; init; } = string.Empty;
    public double? Parsed   { get; init; }

    public bool IsValid => Parsed.HasValue;
    public bool IsEmpty => string.IsNullOrWhiteSpace(RawValue);

    public Brush StatusColor =>
        IsValid  ? GreenBrush :
        IsEmpty  ? GrayBrush  :
                   RedBrush;

    public string FormattedValue =>
        Parsed.HasValue
            ? Parsed.Value.ToString("G10", CultureInfo.InvariantCulture)
            : IsEmpty ? "—" : "Invalid";
}

public class FileResult
{
    private static readonly SolidColorBrush GreenBrush = new(Color.FromRgb(0x16, 0xA3, 0x4A));
    private static readonly SolidColorBrush GrayBrush  = new(Color.FromRgb(0x9C, 0xA3, 0xAF));

    public string Label    { get; init; } = string.Empty;
    public string FilePath { get; init; } = string.Empty;
    public long?  SizeBytes { get; init; }

    public bool IsSelected => !string.IsNullOrEmpty(FilePath);

    public string FileName =>
        IsSelected ? System.IO.Path.GetFileName(FilePath) : "—";

    public Brush StatusColor =>
        IsSelected ? GreenBrush : GrayBrush;

    public string StatusText =>
        IsSelected ? "Selected" : "None";

    public Visibility HasFile =>
        IsSelected ? Visibility.Visible : Visibility.Collapsed;

    public string SizeBytesText =>
        SizeBytes.HasValue ? $"{SizeBytes.Value:N0} bytes" : "—";

    public string SizeKbText =>
        SizeBytes.HasValue ? $"{SizeBytes.Value / 1024.0:N2} KB" : "—";

    public string SizeMbText =>
        SizeBytes.HasValue ? $"{SizeBytes.Value / 1048576.0:N4} MB" : "—";
}
