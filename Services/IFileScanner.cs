namespace RomaToHiragana.Services;

/// <summary>
/// 扫描文件夹并为每个文件生成 <see cref="IFileInfo"/>。
/// 实现类应仅依赖 <see cref="IRomajiConverter"/> 完成文件名转换，
/// 不应直接处理文件系统以外的副作用。
///</summary>
public interface IFileScanner
{
    Task<IReadOnlyList<IFileInfo>> ScanAsync(string folderPath, CancellationToken cancellationToken = default);
}
