using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RomaToHiragana.Services;

/// <summary>
/// 文件列表中的单个条目。属性为 init-only，只有 <see cref="Status"/> 和
/// <see cref="Include"/> 可变并通知 UI。
///</summary>
public sealed class FileItem : INotifyPropertyChanged
{
    private RenameStatus _status;
    private bool _include = true;

    public int Index { get; }
    public string OriginalPath { get; }
    public string OriginalFileName { get; }
    public string ConvertedFileName { get; }
    public string Extension { get; }

    /// <summary>
    /// 是否参与重命名。用户可通过界面勾选排除某些文件。
    ///</summary>
    public bool Include
    {
        get => _include;
        set
        {
            if (_include == value) return;
            _include = value;
            OnPropertyChanged();
        }
    }

    public RenameStatus Status
    {
        get => _status;
        set
        {
            if (_status == value) return;
            _status = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StatusDisplay));
        }
    }

    /// <summary>
    /// 供 UI 直接绑定的中文状态文本。
    ///</summary>
    public string StatusDisplay => Status.ToDisplay();

    public FileItem(
        int index,
        string originalPath,
        string originalFileName,
        string convertedFileName,
        string extension,
        RenameStatus status)
    {
        Index = index;
        OriginalPath = originalPath;
        OriginalFileName = originalFileName;
        ConvertedFileName = convertedFileName;
        Extension = extension;
        _status = status;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
