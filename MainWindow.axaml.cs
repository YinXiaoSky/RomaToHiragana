using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Windowing;
using RomaToHiragana.Services;
using RomaToHiragana.ViewModels;

namespace RomaToHiragana;

/// <summary>
/// 主窗口：只负责与平台交互（文件夹选择、日志滚动），
/// 全部展示逻辑位于 <see cref="MainWindowViewModel"/>。
///</summary>
public partial class MainWindow : FAAppWindow
{
    private readonly MainWindowViewModel _viewModel;

    public MainWindow() : this(CreateDefaultViewModel())
    {
    }

    /// <summary>注入 ViewModel 的构造函数，便于测试与替换实现。</summary>
    public MainWindow(MainWindowViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        _viewModel = viewModel;

        InitializeComponent();
        DataContext = _viewModel;
        _viewModel.Logs.CollectionChanged += (_, _) => LogScrollViewer.ScrollToEnd();
    }

    private static MainWindowViewModel CreateDefaultViewModel()
        => new(new FileScanner(new RomajiToHiraganaConverter()), new FileRenamer());

    private async void OnBrowseClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.StorageProvider is not { } provider)
        {
            _viewModel.ShowStatus(FAInfoBarSeverity.Warning, "无法访问存储提供者，请手动输入路径。");
            return;
        }

        var folders = await provider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "选择包含目标文件的文件夹",
            AllowMultiple = false,
        });

        if (folders is { Count: > 0 })
        {
            _viewModel.FolderPath = folders[0].Path.LocalPath;
            _viewModel.AppendLog($"已选择文件夹：{folders[0].Path.LocalPath}");
            await _viewModel.RefreshAsync();
        }
    }
}
