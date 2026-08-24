namespace RomaToHiragana.Services;

/// <summary>
/// 默认的 <see cref="IFileRenamer"/> 实现：使用 <see cref="File.Move(string,string)"/> 完成重命名。
/// 自动处理以下情况：
///   - 源路径与目标路径相同：跳过（返回失败描述）
///   - 目标文件已存在：跳过（返回失败描述）
///</summary>
public sealed class FileRenamer : IFileRenamer
{
    public Task<RenameResult> RenameAsync(string folderPath, IFileInfo fileInfo, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fileInfo);

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrEmpty(fileInfo.ConvertedFileName))
                return Task.FromResult(new RenameResult(false, "目标文件名为空。"));

            var destPath = Path.Combine(folderPath, fileInfo.ConvertedFileName);

            if (string.Equals(fileInfo.OriginalPath, destPath, StringComparison.OrdinalIgnoreCase))
                return Task.FromResult(new RenameResult(false, "源与目标路径相同。"));

            if (File.Exists(destPath))
                return Task.FromResult(new RenameResult(false, "目标文件已存在。"));

            File.Move(fileInfo.OriginalPath, destPath);
            return Task.FromResult(new RenameResult(true, null, destPath));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new RenameResult(false, ex.Message));
        }
    }
}
