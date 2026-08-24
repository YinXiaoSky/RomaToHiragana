using System.Text;

namespace RomaToHiragana.Services;

/// <summary>
/// 将罗马字（拉丁字母拼写的日语）转换为平假名。
///
/// 支持的规则：
///   - 基本五十音：清音、浊音、半浊音（a・ka・shi・tsu・ga・pa 等）
///   - 拗音：kya・sho・nyu・gyo 等
///   - 长音：元音重复（aa/ii/uu/ee/oo）与 "ou" 形式（kou → こう）
///   - 促音：双写辅音（kitto → きっと，gakkou → がっこう）
///   - 拨音："n" 在词尾或辅音前 → ん；"n" + 元音 → な行
///   - 兼容训令式（si/ti/tu/hu/zi/sya/tya...）与旧式ヘボン（mb/mp → ん）
///   - 无法识别的字符原样保留（数字、标点、汉字、假名等不受影响）
/// </summary>
public sealed class RomajiToHiraganaConverter : IRomajiConverter
{
    private const int MaxKeyLength = 4;

    private static readonly Dictionary<string, string> KanaMap = new(StringComparer.Ordinal)
    {
        // ---- 母音 ----
        ["a"] = "あ", ["i"] = "い", ["u"] = "う", ["e"] = "え", ["o"] = "お",

        // ---- 清音：か行 / さ行 / た行 ----
        ["ka"] = "か", ["ki"] = "き", ["ku"] = "く", ["ke"] = "け", ["ko"] = "こ",
        ["sa"] = "さ", ["shi"] = "し", ["su"] = "す", ["se"] = "せ", ["so"] = "そ",
        ["ta"] = "た", ["chi"] = "ち", ["tsu"] = "つ", ["te"] = "て", ["to"] = "と",

        // ---- 清音：な行 / は行 ----
        ["na"] = "な", ["ni"] = "に", ["nu"] = "ぬ", ["ne"] = "ね", ["no"] = "の",
        ["ha"] = "は", ["hi"] = "ひ", ["fu"] = "ふ", ["he"] = "へ", ["ho"] = "ほ",

        // ---- 清音：ま行 / や行 / ら行 / わ行 ----
        ["ma"] = "ま", ["mi"] = "み", ["mu"] = "む", ["me"] = "め", ["mo"] = "も",
        ["ya"] = "や", ["yu"] = "ゆ", ["yo"] = "よ",
        ["ra"] = "ら", ["ri"] = "り", ["ru"] = "る", ["re"] = "れ", ["ro"] = "ろ",
        ["wa"] = "わ", ["wo"] = "を",

        // ---- 浊音：が行 / ざ行 / だ行 / ば行 ----
        ["ga"] = "が", ["gi"] = "ぎ", ["gu"] = "ぐ", ["ge"] = "げ", ["go"] = "ご",
        ["za"] = "ざ", ["ji"] = "じ", ["zu"] = "ず", ["ze"] = "ぜ", ["zo"] = "ぞ",
        ["da"] = "だ", ["di"] = "ぢ", ["du"] = "づ", ["de"] = "で", ["do"] = "ど",
        ["ba"] = "ば", ["bi"] = "び", ["bu"] = "ぶ", ["be"] = "べ", ["bo"] = "ぼ",

        // ---- 半浊音：ぱ行 ----
        ["pa"] = "ぱ", ["pi"] = "ぴ", ["pu"] = "ぷ", ["pe"] = "ぺ", ["po"] = "ぽ",

        // ---- 拗音（清音）----
        ["kya"] = "きゃ", ["kyu"] = "きゅ", ["kyo"] = "きょ",
        ["sha"] = "しゃ", ["shu"] = "しゅ", ["sho"] = "しょ",
        ["cha"] = "ちゃ", ["chu"] = "ちゅ", ["cho"] = "ちょ",
        ["nya"] = "にゃ", ["nyu"] = "にゅ", ["nyo"] = "にょ",
        ["hya"] = "ひゃ", ["hyu"] = "ひゅ", ["hyo"] = "ひょ",
        ["mya"] = "みゃ", ["myu"] = "みゅ", ["myo"] = "みょ",
        ["rya"] = "りゃ", ["ryu"] = "りゅ", ["ryo"] = "りょ",

        // ---- 拗音（浊音・半浊音）----
        ["gya"] = "ぎゃ", ["gyu"] = "ぎゅ", ["gyo"] = "ぎょ",
        ["ja"] = "じゃ", ["ju"] = "じゅ", ["jo"] = "じょ",
        ["bya"] = "びゃ", ["byu"] = "びゅ", ["byo"] = "びょ",
        ["pya"] = "ぴゃ", ["pyu"] = "ぴゅ", ["pyo"] = "ぴょ",

        // ---- 促音：tch → っ + ち系（matchi → まっち）----
        ["tcha"] = "っちゃ", ["tchi"] = "っち", ["tchu"] = "っちゅ", ["tcho"] = "っちょ",
        ["tch"] = "っち",

        // ---- 拨音 ----
        ["n"] = "ん",

        // ---- 训令式等其它拼写（兼容）----
        ["si"] = "し", ["sya"] = "しゃ", ["syu"] = "しゅ", ["syo"] = "しょ",
        ["ti"] = "ち", ["tya"] = "ちゃ", ["tyu"] = "ちゅ", ["tyo"] = "ちょ",
        ["tu"] = "つ",
        ["hu"] = "ふ",
        ["zi"] = "じ", ["zya"] = "じゃ", ["zyu"] = "じゅ", ["zyo"] = "じょ",
        ["shya"] = "しゃ", ["shyu"] = "しゅ", ["shyo"] = "しょ",
        ["chya"] = "ちゃ", ["chyu"] = "ちゅ", ["chyo"] = "ちょ",
        ["jya"] = "じゃ", ["jyu"] = "じゅ", ["jyo"] = "じょ",
    };

    /// <inheritdoc />
    public string Convert(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        var sb = new StringBuilder(input.Length * 2);
        string text = input.ToLowerInvariant();
        bool prevKeptAsIs = false;

        for (int i = 0; i < text.Length;)
        {
            char c = text[i];

            // 非罗马字字符（数字、标点、汉字、假名等）原样保留
            if (c is < 'a' or > 'z')
            {
                sb.Append(c);
                i++;
                prevKeptAsIs = false;
                continue;
            }

            // 促音：同一个辅音连续出现（nn 除外，nn 表示 んん）
            if (c != 'n' && IsConsonant(c) && i + 1 < text.Length && text[i + 1] == c)
            {
                sb.Append('っ');
                i++;
                prevKeptAsIs = false;
                continue;
            }

            // 旧式ヘボン式：mb / mp 前的 m 表示 ん（shimbun → しんぶん）
            if (c == 'm' && i + 1 < text.Length && text[i + 1] is 'b' or 'p')
            {
                sb.Append('ん');
                i++;
                prevKeptAsIs = false;
                continue;
            }

            // 最长匹配查找（4 → 1 位）
            bool matched = false;
            for (int len = MaxKeyLength; len >= 1; len--)
            {
                if (i + len > text.Length)
                    continue;

                if (KanaMap.TryGetValue(text.Substring(i, len), out string? kana))
                {
                    sb.Append(kana);
                    i += len;
                    matched = true;
                    break;
                }
            }

            if (matched)
            {
                prevKeptAsIs = false;
                continue;
            }

            // 无法识别时保持原样。若紧跟在未转换字符之后的元音，
            // 也一并原样保留（避免 "file" 被转成 "fいlえ"）。
            if (IsVowel(c) && prevKeptAsIs)
            {
                sb.Append(c);
                i++;
                prevKeptAsIs = true;
                continue;
            }

            sb.Append(c);
            i++;
            prevKeptAsIs = true;
        }

        return sb.ToString();
    }

    private static bool IsConsonant(char c) => !IsVowel(c);

    private static bool IsVowel(char c) =>
        c is 'a' or 'i' or 'u' or 'e' or 'o';
}
