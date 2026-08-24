namespace RomaToHiragana.Services;

/// <summary>
/// 默认的 <see cref="IFileScanner"/> 实现：
///   1. 列出目录下所有文件
///   2. 拆分为“文件名主体（stem）+ 扩展名”
///   3. 仅对主体调用 <see cref="IRomajiConverter.Convert"/>，扩展名原样保留
///   4. 组装 <see cref="IFileInfo"/> 返回
///</summary>
public sealed class FileScanner : IFileScanner
{
    private readonly IRomajiConverter _converter;

    public FileScanner(IRomajiConverter converter)
    {
        _converter = converter ?? throw new ArgumentNullException(nameof(converter));
    }

    public Task<IReadOnlyList<IFileInfo>> ScanAsync(string folderPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
            throw new ArgumentException("文件夹路径不能为空。", nameof(folderPath));
        if (!Directory.Exists(folderPath))
            throw new DirectoryNotFoundException($"文件夹不存在：{folderPath}");

        var ordered = Directory.EnumerateFiles(folderPath).OrderBy(p => p, StringComparer.Ordinal);
        var list = new List<IFileInfo>();
        int index = 1;
        foreach (var path in ordered)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var fileName = Path.GetFileName(path);
            if (string.IsNullOrEmpty(fileName)) continue;

            var extension = Path.GetExtension(fileName);
            var stem = Path.GetFileNameWithoutExtension(fileName);
            var convertedStem = _converter.Convert(stem);
            var convertedFileName = string.IsNullOrEmpty(extension)
                ? convertedStem
                : convertedStem + extension;

            var status = string.Equals(fileName, convertedFileName, StringComparison.Ordinal)
                ? RenameStatus.SkippedNoChange
                : RenameStatus.Pending;

            list.Add(new FileItem(
                index++,
                path,
                fileName,
                convertedFileName,
                extension,
                status));
        }

        return Task.FromResult<IReadOnlyList<IFileInfo>>(list);
    }
}
