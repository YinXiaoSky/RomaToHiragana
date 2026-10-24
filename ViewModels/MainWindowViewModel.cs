using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using FluentAvalonia.UI.Controls;
using RomaToHiragana.Services;

namespace RomaToHiragana.ViewModels;

/// <summary>
/// 主窗口的展示逻辑：扫描、重命名编排、统计与状态提示。
/// 不引用任何控件，便于单元测试（可注入假的 <see cref="IFileScanner"/> / <see cref="IFileRenamer"/>）。
///</summary>
public sealed class MainWindowViewModel : ObservableObject
{
    private readonly IFileScanner _scanner;
    private readonly IFileRenamer _renamer;
    private readonly AsyncRelayCommand _refreshCommand;
    private readonly AsyncRelayCommand _renameCommand;
    private readonly AsyncRelayCommand _selectAllCommand;
    private readonly AsyncRelayCommand _selectNoneCommand;

    private bool _isBusy;
    private string _folderPath = string.Empty;
    private FAInfoBarSeverity _statusSeverity = FAInfoBarSeverity.Informational;
    private string _statusTitle = string.Empty;
    private string _statusMessage = string.Empty;
    private bool _isStatusOpen;
    private int _pendingCount;
    private int _skippedCount;
    private int _failedCount;

    public MainWindowViewModel() : this(new FileScanner(new RomajiToHiraganaConverter()), new FileRenamer())
    {
    }

    /// <summary>测试或替换实现用的构造函数。</summary>
    public MainWindowViewModel(IFileScanner scanner, IFileRenamer renamer)
    {
        _scanner = scanner;
        _renamer = renamer;

        _refreshCommand = new AsyncRelayCommand(() => RefreshAsync(), () => !IsBusy);
        _renameCommand = new AsyncRelayCommand(RenameAsync, () => !IsBusy);
        _selectAllCommand = new AsyncRelayCommand(() => SetAllIncludedAsync(true), () => !IsBusy);
        _selectNoneCommand = new AsyncRelayCommand(() => SetAllIncludedAsync(false), () => !IsBusy);

        AppendLog("就绪。请选择包含目标文件的文件夹。");
    }

    public ObservableCollection<FileItem> Files { get; } = new();
    public ObservableCollection<string> Logs { get; } = new();

    public ICommand RefreshCommand => _refreshCommand;
    public ICommand RenameCommand => _renameCommand;
    public ICommand SelectAllCommand => _selectAllCommand;
    public ICommand SelectNoneCommand => _selectNoneCommand;

    /// <summary>目标文件夹路径（由视图的文件夹选择器写入）。</summary>
    public string FolderPath
    {
        get => _folderPath;
        set => SetProperty(ref _folderPath, value);
    }

    public FAInfoBarSeverity StatusSeverity
    {
        get => _statusSeverity;
        private set => SetProperty(ref _statusSeverity, value);
    }

    public string StatusTitle
    {
        get => _statusTitle;
        private set => SetProperty(ref _statusTitle, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    /// <summary>与 FAInfoBar.IsOpen 双向绑定：用户手动关闭时视图会写回 false。</summary>
    public bool IsStatusOpen
    {
        get => _isStatusOpen;
        private set => SetProperty(ref _isStatusOpen, value);
    }

    public int TotalCount => Files.Count;
    public int PendingCount => _pendingCount;
    public int SkippedCount => _skippedCount;
    public int FailedCount => _failedCount;

    public string TotalCountDisplay => $"总文件数：{TotalCount}";
    public string PendingCountDisplay => $"待转换：{PendingCount}";
    public string SkippedCountDisplay => $"已跳过：{SkippedCount}";
    public string FailedCountDisplay => $"失败：{FailedCount}";

    /// <summary>是否正在执行扫描或批量重命名（命令的 CanExecute 依据）。</summary>
    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (!SetProperty(ref _isBusy, value))
                return;

            _refreshCommand.RaiseCanExecuteChanged();
            _renameCommand.RaiseCanExecuteChanged();
            _selectAllCommand.RaiseCanExecuteChanged();
            _selectNoneCommand.RaiseCanExecuteChanged();
        }
    }

    /// <summary>重新扫描 <see cref="FolderPath"/> 下的文件。</summary>
    /// <param name="announce">是否弹出“已加载 N 个文件”的提示（批量重命名后调用时传 false，避免覆盖结果摘要）。</param>
    public async Task RefreshAsync(bool announce = true)
    {
        if (string.IsNullOrWhiteSpace(FolderPath))
        {
            ShowStatus(FAInfoBarSeverity.Warning, "请先选择文件夹。");
            return;
        }

        try
        {
            var scanned = await _scanner.ScanAsync(FolderPath);
            Files.Clear();
            foreach (var item in scanned)
            {
                item.PropertyChanged += OnFileItemPropertyChanged;
                Files.Add(item);
            }

            UpdateStats();
            AppendLog($"已扫描 {Files.Count} 个文件（待转换 {PendingCount} 个，已排除 {Files.Count(i => !i.Include)} 个）。");
            if (announce)
                ShowStatus(FAInfoBarSeverity.Success, $"已加载 {Files.Count} 个文件。");
        }
        catch (DirectoryNotFoundException ex)
        {
            ShowStatus(FAInfoBarSeverity.Error, ex.Message);
            AppendLog($"扫描失败：{ex.Message}");
        }
        catch (Exception ex)
        {
            ShowStatus(FAInfoBarSeverity.Error, $"扫描失败：{ex.Message}");
            AppendLog($"扫描失败：{ex.Message}");
        }
    }

    /// <summary>对所有勾选的待转换文件执行重命名。</summary>
    public async Task RenameAsync()
    {
        var pending = Files.Where(i => i.Include && i.Status == RenameStatus.Pending).ToList();
        if (pending.Count == 0)
        {
            ShowStatus(FAInfoBarSeverity.Informational, "没有待转换的文件（已勾选的），请先刷新列表或勾选文件。");
            return;
        }

        IsBusy = true;
        try
        {
            int success = 0, skipped = 0, failed = 0;

            foreach (var item in pending)
            {
                var result = await _renamer.RenameAsync(FolderPath, item);
                switch (result.Outcome)
                {
                    case RenameOutcome.Renamed:
                        item.Status = RenameStatus.Success;
                        success++;
                        AppendLog($"已转换：{item.OriginalFileName} → {item.ConvertedFileName}");
                        break;

                    case RenameOutcome.TargetExists:
                        item.Status = RenameStatus.SkippedTargetExists;
                        skipped++;
                        AppendLog($"跳过（目标已存在）：{item.OriginalFileName} → {item.ConvertedFileName}");
                        break;

                    case RenameOutcome.Unchanged:
                        item.Status = RenameStatus.SkippedNoChange;
                        skipped++;
                        AppendLog($"跳过（无变化）：{item.OriginalFileName}");
                        break;

                    default:
                        item.Status = RenameStatus.Failed;
                        failed++;
                        AppendLog($"失败：{item.OriginalFileName} → {result.ErrorMessage}");
                        break;
                }
            }

            UpdateStats();
            ShowStatus(
                failed == 0
                    ? (success == 0 ? FAInfoBarSeverity.Informational : FAInfoBarSeverity.Success)
                    : FAInfoBarSeverity.Error,
                $"完成：成功 {success}，跳过 {skipped}，失败 {failed}。");

            // 刷新列表以反映磁盘上的新文件名；不弹提示，保留上面的结果摘要。
            await RefreshAsync(announce: false);
        }
        catch (Exception ex)
        {
            ShowStatus(FAInfoBarSeverity.Error, $"重命名失败：{ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void AppendLog(string message) => Logs.Add($"{DateTime.Now:HH:mm:ss}  {message}");

    public void ShowStatus(FAInfoBarSeverity severity, string message)
    {
        StatusSeverity = severity;
        StatusTitle = severity switch
        {
            FAInfoBarSeverity.Success => "操作成功",
            FAInfoBarSeverity.Warning => "注意",
            FAInfoBarSeverity.Error => "出错了",
            _ => "提示",
        };
        StatusMessage = message;
        IsStatusOpen = true;
    }

    private Task SetAllIncludedAsync(bool include)
    {
        foreach (var item in Files)
            item.Include = include;
        return Task.CompletedTask;
    }

    private void OnFileItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FileItem.Include))
            UpdateStats();
    }

    private void UpdateStats()
    {
        SetProperty(ref _pendingCount, Files.Count(i => i.Include && i.Status == RenameStatus.Pending));
        SetProperty(ref _skippedCount, Files.Count(i => i.Status is RenameStatus.SkippedNoChange or RenameStatus.SkippedTargetExists));
        SetProperty(ref _failedCount, Files.Count(i => i.Status == RenameStatus.Failed));
        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(TotalCountDisplay));
        OnPropertyChanged(nameof(PendingCountDisplay));
        OnPropertyChanged(nameof(SkippedCountDisplay));
        OnPropertyChanged(nameof(FailedCountDisplay));
    }
}
