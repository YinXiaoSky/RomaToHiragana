namespace RomaToHiragana.Services;

/// <summary>
/// 重命名操作的结果。
///</summary>
/// <param name="Success">是否成功</param>
/// <param name="ErrorMessage">失败时的错误描述（成功时为 null）。</param>
/// <param name="NewPath">成功后的新路径（用于刷新状态，失败时为 null）。</param>
public sealed record RenameResult(bool Success, string? ErrorMessage = null, string? NewPath = null);

/// <summary>
/// 将 <see cref="IFileInfo"/> 重命名为其 <c>ConvertedFileName</c>。
/// 实现类应只负责文件系统操作，不感知 UI 和转换细节。
///</summary>
public interface IFileRenamer
{
    Task<RenameResult> RenameAsync(string folderPath, IFileInfo fileInfo, CancellationToken cancellationToken = default);
}
