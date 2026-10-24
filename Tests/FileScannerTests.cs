using RomaToHiragana.Services;
using Xunit;

namespace RomaToHiragana.Tests;

public class FileScannerTests : IDisposable
{
    private readonly DirectoryWrapper _dir = new();

    [Fact]
    public async Task ScanAsync_ConvertsStem_KeepsExtension()
    {
        await File.WriteAllTextAsync(_dir.PathOf("konichiwa.txt"), "x");
        await File.WriteAllTextAsync(_dir.PathOf("123.log"), "x");

        var items = await new FileScanner(new RomajiToHiraganaConverter()).ScanAsync(_dir.FullPath);

        Assert.Equal(2, items.Count);

        var unchanged = items[0];
        Assert.Equal("123.log", unchanged.OriginalFileName);
        Assert.Equal("123.log", unchanged.ConvertedFileName);
        Assert.Equal(".log", unchanged.Extension);
        Assert.Equal(RenameStatus.SkippedNoChange, unchanged.Status);
        Assert.Equal(1, unchanged.Index);

        var pending = items[1];
        Assert.Equal("konichiwa.txt", pending.OriginalFileName);
        Assert.Equal("こにちわ.txt", pending.ConvertedFileName);
        Assert.Equal(RenameStatus.Pending, pending.Status);
        Assert.Equal(2, pending.Index);
    }

    [Fact]
    public async Task ScanAsync_MissingFolder_ThrowsDirectoryNotFound()
        => await Assert.ThrowsAsync<DirectoryNotFoundException>(
            () => new FileScanner(new RomajiToHiraganaConverter()).ScanAsync(_dir.MissingPath));

    public void Dispose() => _dir.Dispose();
}

/// <summary>每个测试实例独享一个临时目录，测试结束自动清理。</summary>
internal sealed class DirectoryWrapper : IDisposable
{
    public DirectoryInfo Info { get; }

    public DirectoryWrapper() => Info = Directory.CreateTempSubdirectory("RomaToHiraganaTests_");

    public string FullPath => Info.FullName;

    public string PathOf(string fileName) => Path.Combine(Info.FullName, fileName);

    /// <summary>一个确定不存在的路径（位于临时目录之下）。</summary>
    public string MissingPath => Path.Combine(Info.FullName, "__no_such__");

    public void Dispose()
    {
        try { Info.Delete(recursive: true); } catch (IOException) { }
    }
}
