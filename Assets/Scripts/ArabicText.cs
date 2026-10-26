using System.Collections.Generic;
using System.Text;

/// <summary>
/// Minimal Arabic shaping and right-to-left ordering for Unity's legacy (IMGUI) text,
/// which renders characters left to right without joining. Letters are mapped to
/// Unicode presentation forms (FE70–FEFF) and each line is reordered for display,
/// keeping Latin words and numbers in reading order. Diacritics are dropped.
/// </summary>
public static class ArabicText
{
    private const char Tatweel = 'ـ';
    // Isolated presentation form per letter. Dual-joining letters have 4 consecutive forms
    // (isolated, final, initial, medial); right-joining letters have 2 (isolated, final).
    private static readonly Dictionary<char, int> Isolated = new Dictionary<char, int>();
    private static readonly HashSet<char> Dual = new HashSet<char>();
    private static readonly HashSet<char> NonJoining = new HashSet<char> { 'ء' };

    static ArabicText()
    {
        Isolated['ء'] = 0xFE80;
        void R(char c, int form) => Isolated[c] = form;
        void D(char c, int form) { Isolated[c] = form; Dual.Add(c); }
        R('آ', 0xFE81); R('أ', 0xFE83); R('ؤ', 0xFE85); R('إ', 0xFE87);
        D('ئ', 0xFE89); R('ا', 0xFE8D); D('ب', 0xFE8F); R('ة', 0xFE93);
        D('ت', 0xFE95); D('ث', 0xFE99); D('ج', 0xFE9D); D('ح', 0xFEA1);
        D('خ', 0xFEA5); R('د', 0xFEA9); R('ذ', 0xFEAB); R('ر', 0xFEAD);
        R('ز', 0xFEAF); D('س', 0xFEB1); D('ش', 0xFEB5); D('ص', 0xFEB9);
        D('ض', 0xFEBD); D('ط', 0xFEC1); D('ظ', 0xFEC5); D('ع', 0xFEC9);
        D('غ', 0xFECD); D('ف', 0xFED1); D('ق', 0xFED5); D('ك', 0xFED9);
        D('ل', 0xFEDD); D('م', 0xFEE1); D('ن', 0xFEE5); D('ه', 0xFEE9);
        R('و', 0xFEED); R('ى', 0xFEEF); D('ي', 0xFEF1);
    }

    public static bool ContainsArabic(string text)
    {
        foreach (var c in text) if (IsArabic(c)) return true;
        return false;
    }

    /// <summary>Returns text ready to draw left to right in an RTL paragraph.</summary>
    public static string Visual(string logical)
    {
        if (string.IsNullOrEmpty(logical) || !ContainsArabic(logical)) return logical;
        var lines = logical.Split('\n');
        for (var i = 0; i < lines.Length; i++) lines[i] = ReorderLine(Shape(lines[i]));
        return string.Join("\n", lines);
    }

    /// <summary>Joins letters into contextual presentation forms, in logical order.</summary>
    public static string Shape(string text)
    {
        var plain = new StringBuilder(text.Length);
        foreach (var c in text)
            if (!IsDiacritic(c)) plain.Append(c);
        var t = plain.ToString();
        var result = new StringBuilder(t.Length);
        for (var i = 0; i < t.Length; i++)
        {
            var c = t[i];
            int isolated;
            if (!Isolated.TryGetValue(c, out isolated)) { result.Append(c); continue; }
            var joinsPrevious = i > 0 && JoinsNext(t[i - 1]);
            int ligature;
            if (c == 'ل' && i + 1 < t.Length && LamAlef(t[i + 1], out ligature))
            {
                result.Append((char)(joinsPrevious ? ligature + 1 : ligature));
                i++;
                continue;
            }
            if (NonJoining.Contains(c)) { result.Append((char)isolated); continue; }
            var joinsNext = Dual.Contains(c) && i + 1 < t.Length && AcceptsPrevious(t[i + 1]);
            int form;
            if (Dual.Contains(c)) form = joinsPrevious ? (joinsNext ? 3 : 1) : (joinsNext ? 2 : 0);
            else form = joinsPrevious ? 1 : 0;
            result.Append((char)(isolated + form));
        }
        return result.ToString();
    }

    private static string ReorderLine(string shaped)
    {
        // Split into left-to-right runs (Latin letters, digits and the neutrals between them)
        // and right-to-left runs; then lay the runs out from right to left.
        var runs = new List<KeyValuePair<bool, string>>();
        var i = 0;
        while (i < shaped.Length)
        {
            var start = i;
            if (IsStrongLtr(shaped[i]))
            {
                while (i < shaped.Length && (IsStrongLtr(shaped[i]) || (!IsArabic(shaped[i]) && NextStrongIsLtr(shaped, i)))) i++;
                runs.Add(new KeyValuePair<bool, string>(true, shaped.Substring(start, i - start)));
            }
            else
            {
                while (i < shaped.Length && !IsStrongLtr(shaped[i])) i++;
                runs.Add(new KeyValuePair<bool, string>(false, shaped.Substring(start, i - start)));
            }
        }
        var visual = new StringBuilder(shaped.Length);
        for (var r = runs.Count - 1; r >= 0; r--)
        {
            if (runs[r].Key) { visual.Append(runs[r].Value); continue; }
            var run = runs[r].Value;
            for (var k = run.Length - 1; k >= 0; k--) visual.Append(Mirror(run[k]));
        }
        return visual.ToString();
    }

    private static bool NextStrongIsLtr(string text, int index)
    {
        for (var i = index; i < text.Length; i++)
        {
            if (IsStrongLtr(text[i])) return true;
            if (IsArabic(text[i])) return false;
        }
        return false;
    }

    private static bool LamAlef(char next, out int ligature)
    {
        switch (next)
        {
            case 'آ': ligature = 0xFEF5; return true;
            case 'أ': ligature = 0xFEF7; return true;
            case 'إ': ligature = 0xFEF9; return true;
            case 'ا': ligature = 0xFEFB; return true;
            default: ligature = 0; return false;
        }
    }

    private static bool JoinsNext(char c) => Dual.Contains(c) || c == Tatweel;
    private static bool AcceptsPrevious(char c) => c == Tatweel || (Isolated.ContainsKey(c) && !NonJoining.Contains(c));
    private static bool IsDiacritic(char c) => (c >= 'ً' && c <= 'ٟ') || c == 'ٰ';
    private static bool IsArabic(char c) =>
        (c >= '؀' && c <= 'ۿ') || (c >= 'ﭐ' && c <= '﷿') || (c >= 'ﹰ' && c <= '﻿');
    private static bool IsStrongLtr(char c) => char.IsLetterOrDigit(c) && !IsArabic(c);

    private static char Mirror(char c)
    {
        switch (c)
        {
            case '(': return ')';
            case ')': return '(';
            case '[': return ']';
            case ']': return '[';
            case '<': return '>';
            case '>': return '<';
            case '«': return '»';
            case '»': return '«';
            default: return c;
        }
    }
}
