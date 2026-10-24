namespace RomaToHiragana.Services;

/// <summary>重命名操作的结果类别。</summary>
public enum RenameOutcome
{
    /// <summary>已成功重命名。</summary>
    Renamed,

    /// <summary>源路径与目标路径相同，无需操作。</summary>
    Unchanged,

    /// <summary>目标文件已存在，跳过。</summary>
    TargetExists,

    /// <summary>发生错误，详见 <see cref="Services.RenameResult.ErrorMessage"/>。</summary>
    Failed,
}

/// <summary>
/// 重命名操作的结果。
///</summary>
/// <param name="Outcome">结果类别。</param>
/// <param name="ErrorMessage">失败时的错误描述（其余情况为 null）。</param>
/// <param name="NewPath">成功后的新路径（其余情况为 null）。</param>
public sealed record RenameResult(RenameOutcome Outcome, string? ErrorMessage = null, string? NewPath = null)
{
    public static RenameResult Renamed(string newPath) => new(RenameOutcome.Renamed, NewPath: newPath);

    public static RenameResult Unchanged() => new(RenameOutcome.Unchanged);

    public static RenameResult TargetExists() => new(RenameOutcome.TargetExists);

    public static RenameResult Failed(string message) => new(RenameOutcome.Failed, ErrorMessage: message);
}
