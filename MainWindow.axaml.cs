using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Windowing;
using RomaToHiragana.Services;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace RomaToHiragana;

/// <summary>
/// 主窗口：仅依赖服务层接口（IFileScanner / IFileRenamer），不直接 new 任何实现，
/// 也不感知转换算法细节。所有模块通过接口注入，UI 层只负责交互。
///</summary>
public partial class MainWindow : FAAppWindow
{
    private readonly IFileScanner _scanner;
    private readonly IFileRenamer _renamer;

    private readonly ObservableCollection<FileItem> _items = new();
    private readonly ObservableCollection<string> _logs = new();

    public MainWindow() : this(new FileScanner(new RomajiToHiraganaConverter()), new FileRenamer())
    {
    }

    /// <summary>
    /// 设计用于单元测试或替换实现的构造函数。
    ///</summary>
    public MainWindow(IFileScanner scanner, IFileRenamer renamer)
    {
        ArgumentNullException.ThrowIfNull(scanner);
        ArgumentNullException.ThrowIfNull(renamer);

        _scanner = scanner;
        _renamer = renamer;

        InitializeComponent();
        FileListBox.ItemsSource = _items;
        LogItemsControl.ItemsSource = _logs;

        AppendLog("就绪。请选择包含目标文件的文件夹。");
    }

    private async void OnBrowseClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.StorageProvider is not { } provider)
        {
            ShowStatus(FAInfoBarSeverity.Warning, "无法访问存储提供者，请手动输入路径。");
            return;
        }

        var folders = await provider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "选择包含目标文件的文件夹",
            AllowMultiple = false,
        });

        if (folders is { Count: > 0 })
        {
            FolderPathTextBox.Text = folders[0].Path.LocalPath;
            AppendLog($"已选择文件夹：{folders[0].Path.LocalPath}");
            await RefreshFileListAsync();
        }
    }

    private async void OnRefreshClick(object? sender, RoutedEventArgs e)
    {
        await RefreshFileListAsync();
    }

    private async Task RefreshFileListAsync()
    {
        string folder = FolderPathTextBox.Text ?? string.Empty;
        if (string.IsNullOrWhiteSpace(folder))
        {
            ShowStatus(FAInfoBarSeverity.Warning, "请先选择文件夹。");
            return;
        }

        try
        {
            var result = await _scanner.ScanAsync(folder);
            _items.Clear();
            foreach (var info in result)
            {
                if (info is FileItem fi)
                {
                    fi.PropertyChanged += OnFileItemPropertyChanged;
                    _items.Add(fi);
                }
            }
            UpdateStats();
            AppendLog($"已扫描 {_items.Count} 个文件（待转换 {PendingCount} 个，已排除 {_items.Count(i => !i.Include)} 个）。");
            ShowStatus(FAInfoBarSeverity.Success, $"已加载 {_items.Count} 个文件。");
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

    private async void OnRenameClick(object? sender, RoutedEventArgs e)
    {
        string folder = FolderPathTextBox.Text ?? string.Empty;
        if (string.IsNullOrWhiteSpace(folder))
        {
            ShowStatus(FAInfoBarSeverity.Warning, "请先选择文件夹。");
            return;
        }

        var pending = _items.Where(i => i.Include && i.Status == RenameStatus.Pending).ToList();
        if (pending.Count == 0)
        {
            ShowStatus(FAInfoBarSeverity.Informational, "没有待转换的文件（已勾选的），请先刷新列表或勾选文件。");
            return;
        }

        RenameButton.IsEnabled = false;
        int success = 0, skipped = 0, failed = 0;

        foreach (var item in pending)
        {
            var result = await _renamer.RenameAsync(folder, item);
            if (result.Success)
            {
                item.Status = RenameStatus.Success;
                success++;
                AppendLog($"已转换：{item.OriginalFileName} → {item.ConvertedFileName}");
            }
            else if (result.ErrorMessage == "目标文件已存在。")
            {
                item.Status = RenameStatus.SkippedTargetExists;
                skipped++;
                AppendLog($"跳过（目标已存在）：{item.OriginalFileName} → {item.ConvertedFileName}");
            }
            else
            {
                item.Status = RenameStatus.Failed;
                failed++;
                AppendLog($"失败：{item.OriginalFileName} → {result.ErrorMessage}");
            }
        }

        RenameButton.IsEnabled = true;
        UpdateStats();

        ShowStatus(
            failed == 0
                ? (success == 0 ? FAInfoBarSeverity.Informational : FAInfoBarSeverity.Success)
                : FAInfoBarSeverity.Error,
            $"完成：成功 {success}，跳过 {skipped}，失败 {failed}。");

        await RefreshFileListAsync();
    }

    private int PendingCount =>
        _items.Count(i => i.Include && i.Status == RenameStatus.Pending);

    private void UpdateStats()
    {
        TotalCountText.Text = $"总文件数：{_items.Count}";
        ConvertibleCountText.Text = $"待转换：{PendingCount}";
        SkippedCountText.Text = $"已跳过：{_items.Count(i => i.Status is RenameStatus.SkippedNoChange or RenameStatus.SkippedTargetExists)}";
        ErrorCountText.Text = $"失败：{_items.Count(i => i.Status == RenameStatus.Failed)}";
    }

    private void OnFileItemPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FileItem.Include))
            UpdateStats();
    }

    private void OnSelectAllClick(object? sender, RoutedEventArgs e)
    {
        foreach (var item in _items)
            item.Include = true;
    }

    private void OnSelectNoneClick(object? sender, RoutedEventArgs e)
    {
        foreach (var item in _items)
            item.Include = false;
    }

    private void ShowStatus(FAInfoBarSeverity severity, string message)
    {
        StatusInfoBar.Severity = severity;
        StatusInfoBar.Title = severity switch
        {
            FAInfoBarSeverity.Success => "操作成功",
            FAInfoBarSeverity.Warning => "注意",
            FAInfoBarSeverity.Error => "出错了",
            _ => "提示",
        };
        StatusInfoBar.Message = message;
        StatusInfoBar.IsOpen = true;
    }

    private void AppendLog(string message)
    {
        _logs.Add($"{DateTime.Now:HH:mm:ss}  {message}");
        try { LogScrollViewer.ScrollToEnd(); } catch { /* 滚动失败不影响主流程 */ }
    }
}
