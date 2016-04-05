using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;

namespace PofilesReader.Localization
{
    /// <summary>
    /// either call your resource file core.po in app_data/localization
    /// either specifiy an array of files
    /// </summary>
    public class DefaultLocalizedStringManager : ILocalizedStringManager
    {
        private string[] _filesPath;
        private IDictionary<string, CultureDictionary> _culturesCache = new Dictionary<string, CultureDictionary>();
        public ILogger Logger { get; set; }
        //private readonly ICacheManager _cacheManager;

        public DefaultLocalizedStringManager(IPathsBuilder pathsBuilder)
        {
            var directoryPath = pathsBuilder.GetDirPath();
            if (string.IsNullOrEmpty(directoryPath))
                _filesPath = new string[0];
            else
                _filesPath = Directory.EnumerateFiles(directoryPath, "*.po").ToArray();
        }


        // This will translate a string into a string in the target cultureName.
        // The scope portion is optional, it amounts to the location of the file containing 
        // the string in case it lives in a view, or the namespace name if the string lives in a binary.
        // If the culture doesn't have a translation for the string, it will fallback to the 
        // parent culture as defined in the .net culture hierarchy. e.g. fr-FR will fallback to fr.
        // In case it's not found anywhere, the text is returned as is.
        public string GetLocalizedString(string scope, string text, bool plural = false, int? index = null)
        {
            var culture = LoadAndGetCulture(CultureInfo.CurrentUICulture);

            string scopedKey = (scope + "|" + text).ToLowerInvariant();
            string genericKey = (text + "|" + text).ToLowerInvariant();

            var value = GetValueFallbacks(culture, plural, index.GetValueOrDefault(1), scopedKey, genericKey);
            if (string.IsNullOrEmpty(value))
                value = GetParentTranslation(scope, text, CultureInfo.CurrentUICulture.Name, plural, index);
            if (string.IsNullOrEmpty(value))
                value = text;
            return value;
        }

        /// <summary>
        /// look for values, returns first found
        /// </summary>
        /// <param name="culture"></param>
        /// <param name="keys"></param>
        /// <returns></returns>
        private static string GetValueFallbacks(CultureDictionary culture, bool plural, int? index = null, params string[] keys)
        {
            if (plural && !index.HasValue)
                throw new ArgumentException("if plural, index must have a value");
            foreach (var key in keys)
            {
                if (culture.ContainsKey(key))
                {
                    var value = !plural ? culture[key].Value : culture[key].PluralNValues[index.Value];
                    if (string.IsNullOrEmpty(value))
                        return (!plural) ? key : culture[key].KeyPlural;
                    else
                        return value;
                }
            }
            return string.Empty;
        }


        private string GetParentTranslation(string scope, string text, string cultureName, bool plural = false, int? index = null)
        {
            string scopedKey = (scope + "|" + text).ToLowerInvariant();
            string genericKey = ("|" + text).ToLowerInvariant();
            try
            {
                CultureInfo cultureInfo = CultureInfo.GetCultureInfo(cultureName);
                CultureInfo parentCultureInfo = cultureInfo.Parent;
                if (parentCultureInfo.IsNeutralCulture)
                {
                    var culture = LoadAndGetCulture(parentCultureInfo);
                    var value = GetValueFallbacks(culture, plural, index, scopedKey, genericKey);
                    return value;
                }
            }
            catch (CultureNotFoundException)
            {//TODO no empty catch
            }
            return text;
        }

        private CultureDictionary LoadAndGetCulture(CultureInfo cultureInfo)
        {
            CultureDictionary culture = null;
            if (!_culturesCache.TryGetValue(cultureInfo.Name, out culture))
            {
                culture = LoadCulture(cultureInfo.Name);
                _culturesCache.Add(culture.CultureName, culture);
            }

            return culture;
        }

        // Loads the culture dictionary in memory and caches it.
        // Cache entry will be invalidated any time the directories hosting 
        // the .po files are modified.
        private CultureDictionary LoadCulture(string culture)
        {
            return new CultureDictionary
            {
                CultureName = culture,
                Translations = LoadTranslationsForCulture(culture)
            };
        }

        // Merging occurs from multiple locations:
        // The dictionary entries from po files that live in higher priority locations will
        // override the ones from lower priority locations during loading of dictionaries.
        private IEnumerable<CultureValue> LoadTranslationsForCulture(string culture)
        {
            List<CultureValue> translations = new List<CultureValue>();
            foreach (var path in _filesPath)
            {
                string filepath = string.Format(path, culture);
                var text = File.ReadAllText(filepath);

                if (text != null)
                {
                    var trResult = ParseLocalizationStream(text, false);
                    translations = translations.Union(trResult).ToList();
                }
            }

            return translations;
        }

        private static readonly Dictionary<char, char> _escapeTranslations = new Dictionary<char, char> {
            { 'n', '\n' },
            { 'r', '\r' },
            { 't', '\t' }
        };

        private static string Unescape(string str)
        {
            StringBuilder sb = null;
            bool escaped = false;
            for (var i = 0; i < str.Length; i++)
            {
                var c = str[i];
                if (escaped)
                {
                    if (sb == null)
                    {
                        sb = new StringBuilder(str.Length);
                        if (i > 1)
                        {
                            sb.Append(str.Substring(0, i - 1));
                        }
                    }
                    char unescaped;
                    if (_escapeTranslations.TryGetValue(c, out unescaped))
                    {
                        sb.Append(unescaped);
                    }
                    else
                    {
                        // General rule: \x ==> x
                        sb.Append(c);
                    }
                    escaped = false;
                }
                else
                {
                    if (c == '\\')
                    {
                        escaped = true;
                    }
                    else if (sb != null)
                    {
                        sb.Append(c);
                    }
                }
            }
            return sb == null ? str : sb.ToString();
        }

        private static IEnumerable<CultureValue> ParseLocalizationStream(string text, bool merge)
        {
            using (var reader = new StringReader(text))
            {
                string poLine, id, scope, idplural = string.Empty;
                int? pluralNumbers = null;
                id = scope = string.Empty;
                CultureValue cultureValue = null;
                while ((poLine = reader.ReadLine()) != null)
                {
                    if (poLine.StartsWith("\"Plural-Forms"))
                    {
                        int pluralNbResult = 0;
                        var splits = poLine.Split(':');
                        var splitssemicom = splits.SelectMany(s => s.Split(';'));
                        var nplural = splitssemicom.Single(e => e.Trim().StartsWith("nplurals="));
                        var res = int.TryParse(nplural.Split('=')[1], out pluralNbResult);
                        if (!res)
                        {
                            throw new ArgumentException("plural-forms : incorrect number after nplural");
                        }
                        pluralNumbers = pluralNbResult;
                        continue;
                    }

                    if (cultureValue != null && string.IsNullOrWhiteSpace(poLine))
                    {
                        yield return cultureValue;
                        cultureValue = null; //next translation
                        poLine = id = scope = idplural = string.Empty;
                        continue;
                    }

                    if (poLine.StartsWith("#:"))
                    {
                        scope = ParseScope(poLine);
                        continue;
                    }

                    if (poLine.StartsWith("msgctxt"))
                    {
                        scope = ParseContext(poLine);
                        continue;
                    }

                    if (poLine.StartsWith("msgid "))
                    {
                        id = ParseId(poLine);
                        continue;
                    }
                    if (poLine.StartsWith("msgid_plural"))
                    {
                        idplural = ParseIdPlural(poLine);
                        continue;
                    }
                    if (poLine.StartsWith("msgstr "))
                    {
                        if (!string.IsNullOrEmpty(idplural))
                            throw new ArgumentException($"Wrong format of po file, line '{poLine}' contains msgstr without indexes whereas msgid_plural was set");

                        string translation = ParseTranslation(poLine);
                        // ignore incomplete localizations (empty msgid or msgstr)
                        if (!string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(translation))
                        {
                            string scopedKey = (scope + "|" + id).ToLowerInvariant();
                            cultureValue = new CultureValue { Key = scopedKey, PluralNValues = new string[1], Value = translation, };
                        }
                        id = scope = string.Empty;
                        continue;
                    }
                    if (poLine.StartsWith("msgstr["))
                    {
                        if (string.IsNullOrEmpty(idplural))
                            throw new ArgumentException($"Wrong format of po file, line '{poLine}' contains msgstr[] with indexes whereas msgid_plural was not set");
                        if (!pluralNumbers.HasValue)
                            throw new ArgumentException($"Plural is set in line '{poLine}' whereas no plural line forms in header");
                        string translation = ParsePluralTranslation(poLine);
                        var index = int.Parse(poLine.Substring(7, 1));
                        // ignore incomplete localizations (empty msgid or msgstr)
                        string scopedKey = (scope + "|" + id).ToLowerInvariant();
                        if (cultureValue == null)
                        {
                            cultureValue = new CultureValue { Key = scopedKey, KeyPlural = idplural, PluralNValues = new string[pluralNumbers.Value] };
                        }
                        cultureValue.PluralNValues[index] = translation;
                        id = scope = string.Empty;
                        continue;
                    }
                }
                if (cultureValue != null) // last line
                {
                    yield return cultureValue;
                }
            }
        }

        private static string ParseTranslation(string poLine)
        {
            return Unescape(poLine.Substring(6).Trim().Trim('"'));
        }
        private static string ParsePluralTranslation(string poLine)
        {
            return Unescape(poLine.Substring(9).Trim().Trim('"'));
        }
        private static string ParseId(string poLine)
        {
            return Unescape(poLine.Substring(5).Trim().Trim('"'));
        }

        private static string ParseIdPlural(string poLine)
        {
            return Unescape(poLine.Substring(12).Trim().Trim('"'));
        }

        private static string ParseScope(string poLine)
        {
            return Unescape(poLine.Substring(2).Trim().Trim('"'));
        }

        private static string ParseContext(string poLine)
        {
            return Unescape(poLine.Substring(7).Trim().Trim('"'));
        }

        class CultureDictionary
        {
            public string CultureName { get; set; }

            public bool ContainsKey(string key)
            {
                return Translations.Any(c => c.Key == key);
            }

            public CultureValue this[string key]
            {
                get
                {
                    return Translations.FirstOrDefault(e => e.Key == key);
                }
            }
            public IEnumerable<CultureValue> Translations { get; set; }
        }

        class CultureValue
        {
            public override bool Equals(object obj)
            {
                if (!(obj is CultureValue))
                    throw new ArgumentException("obj should be of culture value type");
                return Key == (obj as CultureValue).Key;
            }
            public override int GetHashCode()
            {
                return Key.GetHashCode();
            }
            public string Key { get; set; }

            public string KeyPlural { get; set; }

            public string Value { get { return PluralNValues[0]; } set { PluralNValues[0] = value; } }

            /// <summary>
            /// such as
            /// msgstr[0] "s'ha trobat %d error fatal"
            ///msgstr[1] "s'han trobat %d errors fatals"
            /// </summary>
            public string[] PluralNValues { get; set; }
        }
    }
}

