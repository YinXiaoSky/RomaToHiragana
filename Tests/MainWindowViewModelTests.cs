using FluentAvalonia.UI.Controls;
using RomaToHiragana.Services;
using RomaToHiragana.ViewModels;
using Xunit;

namespace RomaToHiragana.Tests;

public class MainWindowViewModelTests
{
    private sealed class FakeScanner(List<FileItem> items) : IFileScanner
    {
        public Task<IReadOnlyList<FileItem>> ScanAsync(string folderPath, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<FileItem>>(items);
    }

    private sealed class FakeRenamer(RenameResult result) : IFileRenamer
    {
        public List<FileItem> Calls { get; } = new();

        public Task<RenameResult> RenameAsync(string folderPath, FileItem file, CancellationToken cancellationToken = default)
        {
            Calls.Add(file);
            return Task.FromResult(result);
        }
    }

    /// <summary>构造一个已扫描完 items 的 ViewModel（走真实扫描路径，事件订阅随之建立）。</summary>
    private static async Task<MainWindowViewModel> CreateVmAsync(List<FileItem> items, IFileRenamer renamer)
    {
        var vm = new MainWindowViewModel(new FakeScanner(items), renamer) { FolderPath = @"C:\f" };
        await vm.RefreshAsync();
        return vm;
    }

    private static List<FileItem> SinglePendingList() => [new(1, @"C:\f\a.txt", "a.txt", "あ.txt", ".txt", RenameStatus.Pending)];

    [Fact]
    public async Task RenameAsync_TargetExists_MapsToSkippedStatus()
    {
        var items = SinglePendingList();
        var renamer = new FakeRenamer(RenameResult.TargetExists());
        var vm = await CreateVmAsync(items, renamer);

        await vm.RenameAsync();

        Assert.Equal(RenameStatus.SkippedTargetExists, items[0].Status);
        Assert.Equal(1, vm.SkippedCount);
        Assert.Equal(0, vm.FailedCount);
        Assert.Single(renamer.Calls);
    }

    [Fact]
    public async Task RenameAsync_Renamed_MapsToSuccessStatus()
    {
        var items = SinglePendingList();
        var renamer = new FakeRenamer(RenameResult.Renamed(@"C:\f\あ.txt"));
        var vm = await CreateVmAsync(items, renamer);

        await vm.RenameAsync();

        Assert.Equal(RenameStatus.Success, items[0].Status);
        Assert.Equal(0, vm.FailedCount);
    }

    [Fact]
    public async Task RenameAsync_Failure_MapsToFailedStatusAndShowsError()
    {
        var items = SinglePendingList();
        var renamer = new FakeRenamer(RenameResult.Failed("被占用。"));
        var vm = await CreateVmAsync(items, renamer);

        await vm.RenameAsync();

        Assert.Equal(RenameStatus.Failed, items[0].Status);
        Assert.Equal(1, vm.FailedCount);
        Assert.Equal(FAInfoBarSeverity.Error, vm.StatusSeverity);
    }

    [Fact]
    public async Task RenameAsync_NoPendingFiles_ShowsInfoAndCallsNothing()
    {
        var renamer = new FakeRenamer(RenameResult.Renamed(@"C:\f\あ.txt"));
        var vm = await CreateVmAsync([], renamer);

        await vm.RenameAsync();

        Assert.Empty(renamer.Calls);
        Assert.True(vm.IsStatusOpen);
    }

    [Fact]
    public async Task RefreshAsync_EmptyFolder_ShowsWarning()
    {
        var vm = new MainWindowViewModel(new FakeScanner([]), new FakeRenamer(RenameResult.Unchanged()));

        await vm.RefreshAsync();

        Assert.True(vm.IsStatusOpen);
        Assert.Equal(FAInfoBarSeverity.Warning, vm.StatusSeverity);
        Assert.Empty(vm.Files);
    }

    [Fact]
    public async Task RefreshAsync_LoadsFilesAndStats()
    {
        var items = new List<FileItem>
        {
            new(1, @"C:\f\a.txt", "a.txt", "あ.txt", ".txt", RenameStatus.Pending),
            new(2, @"C:\f\123.log", "123.log", "123.log", ".log", RenameStatus.SkippedNoChange),
        };
        var vm = await CreateVmAsync(items, new FakeRenamer(RenameResult.Unchanged()));

        await vm.RefreshAsync();

        Assert.Equal(2, vm.TotalCount);
        Assert.Equal(1, vm.PendingCount);
        Assert.Equal(1, vm.SkippedCount);
    }

    [Fact]
    public async Task SelectAll_TogglesIncludeAndStats()
    {
        var items = new List<FileItem>
        {
            new(1, @"C:\f\a.txt", "a.txt", "あ.txt", ".txt", RenameStatus.Pending),
            new(2, @"C:\f\b.txt", "b.txt", "び.txt", ".txt", RenameStatus.Pending),
        };
        var vm = await CreateVmAsync(items, new FakeRenamer(RenameResult.Unchanged()));

        foreach (var item in items)
            item.Include = false;
        Assert.Equal(0, vm.PendingCount);

        foreach (var item in items)
            item.Include = true;
        Assert.Equal(2, vm.PendingCount);
    }
}
