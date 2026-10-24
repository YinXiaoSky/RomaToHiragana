namespace RomaToHiragana.Services;

/// <summary>
/// 默认的 <see cref="IFileRenamer"/> 实现：使用 <see cref="File.Move(string,string)"/> 完成重命名。
/// 结果类别见 <see cref="RenameOutcome"/>，调用方据此分支，不解析错误文本。
///</summary>
public sealed class FileRenamer : IFileRenamer
{
    public Task<RenameResult> RenameAsync(string folderPath, FileItem file, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrEmpty(file.ConvertedFileName))
                return Task.FromResult(RenameResult.Failed("目标文件名为空。"));

            var destPath = Path.Combine(folderPath, file.ConvertedFileName);

            if (string.Equals(file.OriginalPath, destPath, StringComparison.OrdinalIgnoreCase))
                return Task.FromResult(RenameResult.Unchanged());

            if (File.Exists(destPath))
                return Task.FromResult(RenameResult.TargetExists());

            File.Move(file.OriginalPath, destPath);
            return Task.FromResult(RenameResult.Renamed(destPath));
        }
        catch (Exception ex)
        {
            return Task.FromResult(RenameResult.Failed(ex.Message));
        }
    }
}
