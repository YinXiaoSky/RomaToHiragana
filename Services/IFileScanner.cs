namespace RomaToHiragana.Services;

/// <summary>
/// 扫描文件夹并为每个文件生成 <see cref="FileItem"/>。
/// 实现类应仅依赖 <see cref="IRomajiConverter"/> 完成文件名转换，
/// 不应有文件系统以外的副作用。
///</summary>
public interface IFileScanner
{
    Task<IReadOnlyList<FileItem>> ScanAsync(string folderPath, CancellationToken cancellationToken = default);
}
