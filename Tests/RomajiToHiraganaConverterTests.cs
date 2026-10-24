using RomaToHiragana.Services;
using Xunit;

namespace RomaToHiragana.Tests;

public class RomajiToHiraganaConverterTests
{
    private readonly RomajiToHiraganaConverter _converter = new();

    [Theory]
    [InlineData("", "")]
    [InlineData("a", "あ")]
    [InlineData("ka", "か")]
    [InlineData("shi", "し")]
    [InlineData("tsu", "つ")]
    [InlineData("kya", "きゃ")]
    [InlineData("ja", "じゃ")]
    [InlineData("byo", "びょ")]
    [InlineData("konnichiwa", "こんにちわ")]
    [InlineData("arigatou", "ありがとう")]
    [InlineData("toukyou", "とうきょう")]
    [InlineData("nn", "んん")]
    [InlineData("ni", "に")]
    public void Convert_BasicRomaji(string input, string expected)
        => Assert.Equal(expected, _converter.Convert(input));

    [Theory]
    // 促音：双写辅音后紧跟可识别的罗马字
    [InlineData("gakkou", "がっこう")]
    [InlineData("kitto", "きっと")]
    [InlineData("chotto", "ちょっと")]
    [InlineData("kippu", "きっぷ")]
    [InlineData("zasshi", "ざっし")]
    [InlineData("tcha", "っちゃ")]
    public void Convert_Sokuon(string input, string expected)
        => Assert.Equal(expected, _converter.Convert(input));

    [Theory]
    // 训令式等兼容拼写
    [InlineData("sya", "しゃ")]
    [InlineData("ti", "ち")]
    [InlineData("tu", "つ")]
    [InlineData("hu", "ふ")]
    [InlineData("zi", "じ")]
    // 旧式ヘボン：mb 前的 m 为 ん
    [InlineData("shimbun", "しんぶん")]
    public void Convert_AlternativeSpellings(string input, string expected)
        => Assert.Equal(expected, _converter.Convert(input));

    [Theory]
    // 纯英文/数字/汉字：无法识别的部分原样保留
    // （促音误判回归：hello 不得转成 へっお；file 的元音不再被拆开）
    [InlineData("file", "file")]
    [InlineData("hello", "へllo")]
    [InlineData("vva", "vva")]
    [InlineData("123", "123")]
    [InlineData("漢字", "漢字")]
    public void Convert_UnconvertibleTextKeptAsIs(string input, string expected)
        => Assert.Equal(expected, _converter.Convert(input));

    [Fact]
    public void Convert_NullReturnsEmpty()
        => Assert.Equal(string.Empty, _converter.Convert(null));
}
