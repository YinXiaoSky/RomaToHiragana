using RomaToHiragana.Services;
using Xunit;

namespace RomaToHiragana.Tests;

public class FileRenamerTests : IDisposable
{
    private readonly DirectoryWrapper _dir = new();
    private readonly FileRenamer _renamer = new();

    [Fact]
    public async Task RenameAsync_TargetFree_Renames()
    {
        var src = _dir.PathOf("a.txt");
        await File.WriteAllTextAsync(src, "x");

        var result = await _renamer.RenameAsync(_dir.FullPath, MakeItem(src, "a.txt", "b.txt"));

        Assert.Equal(RenameOutcome.Renamed, result.Outcome);
        Assert.Equal(_dir.PathOf("b.txt"), result.NewPath);
        Assert.True(File.Exists(_dir.PathOf("b.txt")));
        Assert.False(File.Exists(src));
    }

    [Fact]
    public async Task RenameAsync_TargetExists_SkipsAndKeepsOriginal()
    {
        await File.WriteAllTextAsync(_dir.PathOf("a.txt"), "x");
        await File.WriteAllTextAsync(_dir.PathOf("b.txt"), "y");

        var result = await _renamer.RenameAsync(_dir.FullPath, MakeItem(_dir.PathOf("a.txt"), "a.txt", "b.txt"));

        Assert.Equal(RenameOutcome.TargetExists, result.Outcome);
        Assert.True(File.Exists(_dir.PathOf("a.txt")));
    }

    [Fact]
    public async Task RenameAsync_SamePath_ReturnsUnchanged()
    {
        var src = _dir.PathOf("same.txt");
        await File.WriteAllTextAsync(src, "x");

        var result = await _renamer.RenameAsync(_dir.FullPath, MakeItem(src, "same.txt", "same.txt"));

        Assert.Equal(RenameOutcome.Unchanged, result.Outcome);
        Assert.True(File.Exists(src));
    }

    [Fact]
    public async Task RenameAsync_NullFile_Throws()
        => await Assert.ThrowsAsync<ArgumentNullException>(
            () => _renamer.RenameAsync(_dir.FullPath, null!));

    private static FileItem MakeItem(string path, string originalName, string convertedName)
        => new(1, path, originalName, convertedName, Path.GetExtension(originalName), RenameStatus.Pending);

    public void Dispose() => _dir.Dispose();
}
