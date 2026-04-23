using Microsoft.Win32;

namespace TimestampCalculator.Services;

public sealed class FileDialogService : IFileDialogService
{
    public string? BrowseFile(string title)
    {
        var dialog = new OpenFileDialog
        {
            Title = title,
            Filter = "All files (*.*)|*.*",
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
