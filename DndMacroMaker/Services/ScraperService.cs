using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

public class ScraperService
{
    public async Task<Spell?> ScrapeAsync(string url)
    {
        var http = new HttpClient(new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        });
        http.DefaultRequestHeaders.UserAgent.ParseAdd("DndToolsScraper/1.0 (+https://example.local)");

        try
        {
            var html = await http.GetStringAsync(url);
            var text = HtmlToPlainText(html);

            var name = ExtractNameFromTitle(html) ?? ExtractNameFromH2(text);
            var (rulebook, page) = ExtractRulebookAndPage(text);
            var school = ExtractSchool(text);
            var levels = ExtractLevels(text);
            var components = ExtractAfterLabel(text, "Components");
            var castingTime = ExtractAfterLabel(text, "Casting Time");
            var range = ExtractAfterLabel(text, "Range");
            var target = ExtractAfterLabel(text, "Target");
            var area = ExtractAfterLabel(text, "Area");
            var effect = ExtractAfterLabel(text, "Effect");
            var duration = ExtractAfterLabel(text, "Duration");
            var savingThrow = ExtractAfterLabel(text, "Saving Throw");
            var spellResistance = ExtractAfterLabel(text, "Spell Resistance");
            var description = ExtractDescription(text);

            return new Spell(
                Name: name,
                Rulebook: rulebook,
                Page: page,
                School: school,
                Levels: levels,  // np. "Wizard 3, Cleric 2"
                Components: components,
                CastingTime: castingTime,
                Range: range,
                Target: target,
                Area: area,
                Effect: effect,
                Duration: duration,
                SavingThrow: savingThrow,
                SpellResistance: spellResistance,
                SourceUrl: url,
                Description: description,
                DMG: null,       // nowe pole, np. null jeœli brak danych
                DMGScale: null,   // nowe pole, np. null jeœli brak danych
                DMGCap: null
            );
        }
        catch
        {
            return null;
        }
    }

    // --- Spell record ---
    public record Spell(
        string? Name,
        string? Rulebook,
        int? Page,
        string? School,
        string? Levels,
        string? Components,
        string? CastingTime,
        string? Range,
        string? Target,
        string? Area,
        string? Effect,
        string? Duration,
        string? SavingThrow,
        string? SpellResistance,
        string? SourceUrl,
        string? Description,
        string? DMG,        // dodane pole na np. "1d6"
        string? DMGScale,    // dodane pole: "1" = per caster level, "" = jednorazowe
        string? DMGCap // np. "10" dla "max 10d6"
    );

    // --- Parsowanie ---
    static string HtmlToPlainText(string html)
    {
        html = Regex.Replace(html, @"<script[\s\S]*?</script>", "", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"<style[\s\S]*?</style>", "", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"<(br|/p|/div|/h\d)>", "\n", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"<[^>]+>", "");
        var text = WebUtility.HtmlDecode(html);
        text = Regex.Replace(text, "\r\n", "\n");
        text = Regex.Replace(text, "\n{3,}", "\n\n");
        text = Regex.Replace(text, "[\t ]{2,}", " ");
        return text.Trim();
    }

    static string? ExtractNameFromTitle(string html)
    {
        var m = Regex.Match(html, @"<title>\s*(.*?)\s*</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (!m.Success) return null;
        var title = WebUtility.HtmlDecode(m.Groups[1].Value).Trim();
        var split = Regex.Split(title, @"\s*[–-]\s*");
        if (split.Length > 0) return split[0].Trim();
        return title;
    }

    static string? ExtractNameFromH2(string text)
    {
        var m = Regex.Match(text, @"^\s*([A-Z][A-Za-z'\- ]+)\s*$", RegexOptions.Multiline);
        return m.Success ? m.Groups[1].Value.Trim() : null;
    }

    static (string? rulebook, int? page) ExtractRulebookAndPage(string text)
    {
        var m = Regex.Match(text, @"\(([^\)]+?),\s*p\.?\s*(\d+)\)");
        if (m.Success)
        {
            var rb = m.Groups[1].Value.Trim();
            if (int.TryParse(m.Groups[2].Value, out var pg))
                return (rb, pg);
            return (rb, null);
        }
        return (null, null);
    }

    static string? ExtractSchool(string text)
    {
        var m = Regex.Match(text, @"\b(Abjuration|Conjuration|Divination|Enchantment|Evocation|Illusion|Necromancy|Transmutation|Universal)\b", RegexOptions.IgnoreCase);
        if (!m.Success) return null;
        return char.ToUpperInvariant(m.Groups[1].Value[0]) + m.Groups[1].Value.Substring(1).ToLowerInvariant();
    }

    static string? ExtractLevels(string text)
    {
        var m = Regex.Match(text, @"Level:\s*(.+?)(?:\n|\r|Components:)", RegexOptions.Singleline);
        if (!m.Success) return null;

        var payload = m.Groups[1].Value.Trim();
        return string.IsNullOrWhiteSpace(payload) ? null : payload;
    }

    static string? ExtractAfterLabel(string text, string label)
    {
        var m = Regex.Match(text, $@"{Regex.Escape(label)}:\s*(.+?)(?:\n|\r|$)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (!m.Success) return null;
        var val = m.Groups[1].Value.Trim();
        val = Regex.Replace(val, @"[,;]+$", "").Trim();
        return string.IsNullOrWhiteSpace(val) ? null : val;
    }

    static string? ExtractDescription(string text)
    {
        var idx = text.IndexOf("Spell Resistance:", StringComparison.OrdinalIgnoreCase);
        if (idx < 0) idx = text.IndexOf("Saving Throw:", StringComparison.OrdinalIgnoreCase);
        if (idx < 0) idx = text.IndexOf("Duration:", StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return null;
        var after = text[(idx)..];
        var m = Regex.Match(after, @"\n\s*\n([\s\S]+)$");
        if (m.Success)
        {
            var desc = m.Groups[1].Value.Trim();
            desc = TrimAt(desc, "Back to ");
            desc = TrimAt(desc, "Related ");
            return string.IsNullOrWhiteSpace(desc) ? null : desc;
        }
        return null;
    }

    static string TrimAt(string text, string marker)
    {
        var i = text.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        return i >= 0 ? text.Substring(0, i).Trim() : text;
    }

    public string GenerateMacro(Spell spell)
    {
        var url = NormalizeMacroUrl(spell.SourceUrl);
        int? lvl = null;
        if (!string.IsNullOrWhiteSpace(spell.Levels))
        {
            var matches = Regex.Matches(spell.Levels, @"\d+");
            if (matches.Count > 0)
            {
                var levels = matches.Select(m => int.Parse(m.Value));
                lvl = levels.Min();
            }
        }

        var components = string.IsNullOrWhiteSpace(spell.Components)
        ? ""
        : spell.Components
            .Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Aggregate((a, b) => a + "," + b); // "V, S" -> "V,S"

        var schoolKey = (spell.School ?? "").Trim().ToLowerInvariant().Replace(" ", "-");

        var sb = new StringBuilder();
        sb.Append("&{template:DnD35StdRoll}");
        AppendPair(sb, "spellflag", "true");
        AppendPair(sb, "name", "@{character_name} casts [" + spell.Name + "](" + url + ")");
        AppendPair(sb, "School:", (spell.School ?? "") + " ");
        AppendPair(sb, "Level:", lvl?.ToString() ?? "");
        AppendPair(sb, "Compnts:", components);
        AppendPair(sb, "Casting Time:", ShortenCastingTime(spell.CastingTime));
        AppendPair(sb, "Range:", spell.Range);
        AppendPair(sb, "Target:", spell.Target);
        AppendPair(sb, "Duration:", BuildDurationMacro(spell.Duration, lvl));
        AppendPair(sb, "Saving Throw:", BuildSavingThrowMacro(spell.SavingThrow, schoolKey, lvl));
        AppendPair(sb, "Spell Resist.", BuildSRMacro(spell.SpellResistance));

        sb.Append(" {{ Caster level check: = [[ 1d20+@{casterlevel}+@{spellpen} ]] vs spell resistance.}}");


        var dmg = spell.DMG ?? "";
        var dmgPerLevel = spell.DMGScale == "1";
        var dmgCap = spell.DMGCap; // np. "10"

        string dmgText = "";
        if (!string.IsNullOrWhiteSpace(dmg))
        {
            if (dmgPerLevel)
            {
                // np. DMG = "1d6", DMGCap = "10"
                var diceMatch = Regex.Match(dmg, @"(\d*)d(\d+)");
                if (diceMatch.Success)
                {
                    var sides = diceMatch.Groups[2].Value; // np. "6"
                    if (!string.IsNullOrWhiteSpace(dmgCap))
                    {
                        // [[[[{0d0+@{casterlevel},0d0+10}kl1]]d6]]
                        dmgText = $"[[[[{{0d0+@{{casterlevel}},0d0+{dmgCap}}}kl1]]d{sides}]]";
                    }
                    else
                    {
                        // [[(@{casterlevel}d6)]]
                        dmgText = $"[[(@{{casterlevel}}d{sides})]]";
                    }
                }
                else
                {
                    dmgText = "[[" + dmg + "]]";
                }
            }
            else
            {
                dmgText = "[[" + dmg + "]]";
            }

            AppendPair(sb, "DMG:", dmgText);
        }

        var notes = SanitizeNotes(spell.Description) + " [GMR](https://dndmacromaker-production.up.railway.app/Home)";
        AppendPair(sb, "notes", notes);

        return sb.ToString();
    }
    static void AppendPair(StringBuilder sb, string label, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        sb.Append(' ');
        sb.Append("{{");
        sb.Append(label);
        sb.Append('=');
        sb.Append(value);
        sb.Append("}}");
    }

    static string NormalizeMacroUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return string.Empty;
        try
        {
            var u = new Uri(url);
            var builder = new UriBuilder(u)
            {
                Scheme = "https",
                Host = "dndtools.net",
                Port = -1
            };
            var s = builder.Uri.ToString();
            return s.EndsWith('/') ? s : s + "/";
        }
        catch { return url; }
    }

    static string ShortenCastingTime(string? casting)
    {
        if (string.IsNullOrWhiteSpace(casting)) return string.Empty;
        var s = casting.Trim();
        s = Regex.Replace(s, @"\bstandard action\b", "std action", RegexOptions.IgnoreCase);
        s = Regex.Replace(s, @"\bfull-round action\b", "full-round", RegexOptions.IgnoreCase);
        return s;
    }

    static string BuildDurationMacro(string? duration, int? lvl)
    {
        if (string.IsNullOrWhiteSpace(duration) || lvl is null) return duration ?? string.Empty;
        var d = duration.ToLowerInvariant();
        var m = Regex.Match(d, @"(\\d+)\\s*(round|minute|hour|day)s?/level");
        if (m.Success)
        {
            var mult = int.Parse(m.Groups[1].Value);
            var unit = m.Groups[2].Value switch
            {
                "round" => "Rounds",
                "minute" => "Minutes",
                "hour" => "Hours",
                "day" => "Days",
                _ => "Units"
            };
            return "[[ @{casterlevel}*" + mult + " ]] " + unit;
        }
        return duration; // w innych przypadkach zostaw orygina³
    }

    static string BuildSavingThrowMacro(string? savingThrow, string schoolKey, int? lvl)
    {
        if (string.IsNullOrWhiteSpace(savingThrow) || lvl is null)
            return savingThrow ?? string.Empty;

        var m = Regex.Match(savingThrow, @"\\b(Fortitude|Reflex|Will)\\b", RegexOptions.IgnoreCase);
        if (!m.Success) return savingThrow;
        var save = Capitalize(m.Groups[1].Value);
        // Przyk³ad: "Will for Negate DC [[ @{spelldc2} + @{sf-divination} ]]"
        return save + " for Negate DC [[ @{spelldc" + lvl + "} + @{sf-" + schoolKey + "} ]]";
    }

    static string BuildSRMacro(string? sr)
    {
        if (string.IsNullOrWhiteSpace(sr)) return string.Empty;
        return sr.Contains("yes", StringComparison.OrdinalIgnoreCase) ? "Yes" :
               sr.Contains("no", StringComparison.OrdinalIgnoreCase) ? "No" : sr;
    }

    static string SanitizeNotes(string? desc)
    {
        if (string.IsNullOrWhiteSpace(desc)) return string.Empty;
        var s = desc.Replace("\r", " ").Replace("\n", " ");
        s = Regex.Replace(s, @"\s{2,}", " ");
        s = Regex.Replace(s, @"(?<=[\.\!\?])(?=\S)", " "); // brakuj¹ca spacja po kropce
        s = s.Replace("{{", "{ {").Replace("}}", "} }"); // zabezpieczenie makra
        return s.Trim();
    }

    static string Capitalize(string s) => char.ToUpperInvariant(s[0]) + s[1..].ToLowerInvariant();

}
