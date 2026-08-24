namespace RomaToHiragana.Services;

/// <summary>
/// 单个待处理文件的数据模型。由 <see cref="IFileScanner"/> 创建，
/// 在 UI 展示和由 <see cref="IFileRenamer"/> 执行重命名时使用。
///</summary>
public interface IFileInfo
{
    int Index { get; }
    string OriginalPath { get; }
    string OriginalFileName { get; }
    string ConvertedFileName { get; }
    string Extension { get; }
    RenameStatus Status { get; set; }
}

public enum RenameStatus
{
    Pending,
    SkippedNoChange,
    SkippedTargetExists,
    Success,
    Failed,
}

public static class RenameStatusExtensions
{
    public static string ToDisplay(this RenameStatus status) => status switch
    {
        RenameStatus.Pending => "待转换",
        RenameStatus.SkippedNoChange => "跳过（无变化）",
        RenameStatus.SkippedTargetExists => "跳过（目标已存在）",
        RenameStatus.Success => "成功",
        RenameStatus.Failed => "失败",
        _ => status.ToString(),
    };
}
