namespace RomaToHiragana.Services;

/// <summary>
/// 罗马字 → 平假名 转换器抽象。
/// 实现类应只负责字符转换，不感知文件名、扩展名、文件系统等概念。
///</summary>
public interface IRomajiConverter
{
    /// <summary>
    /// 将罗马字字符串转换为平假名。空输入返回空字符串。
    /// 未识别的字符保持原样（包含数字、汉字、假名等非拉丁字母字符）。
    ///</summary>
    string Convert(string? input);
}
