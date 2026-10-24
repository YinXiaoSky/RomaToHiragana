namespace RomaToHiragana.Services;

/// <summary>
/// 将 <see cref="FileItem"/> 重命名为其 <c>ConvertedFileName</c>。
/// 实现类应只负责文件系统操作，不感知 UI 和转换细节。
///</summary>
public interface IFileRenamer
{
    Task<RenameResult> RenameAsync(string folderPath, FileItem file, CancellationToken cancellationToken = default);
}
