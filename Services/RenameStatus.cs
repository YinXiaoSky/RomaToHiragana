namespace RomaToHiragana.Services;

/// <summary>文件在列表中的处理状态。</summary>
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
