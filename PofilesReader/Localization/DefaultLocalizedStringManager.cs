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

        public DefaultLocalizedStringManager(string directoryPath)
        {
            _filesPath = Directory.EnumerateFiles(directoryPath).ToArray();
        }

        public DefaultLocalizedStringManager(params string[] filesPaths)
        {
            //_cacheManager = cacheManager;
            _filesPath = filesPaths;
        }

        // This will translate a string into a string in the target cultureName.
        // The scope portion is optional, it amounts to the location of the file containing 
        // the string in case it lives in a view, or the namespace name if the string lives in a binary.
        // If the culture doesn't have a translation for the string, it will fallback to the 
        // parent culture as defined in the .net culture hierarchy. e.g. fr-FR will fallback to fr.
        // In case it's not found anywhere, the text is returned as is.
        public string GetLocalizedString(string scope, string text)
        {
            var culture = LoadAndGetCulture(CultureInfo.CurrentUICulture);

            string scopedKey = (scope + "|" + text).ToLowerInvariant();
            if (culture.ContainsKey(scopedKey))
            {
                return culture[scopedKey].Value;
            }

            string genericKey = ("|" + text).ToLowerInvariant();
            if (culture.ContainsKey(genericKey))
            {
                return culture[genericKey].Value;
            }

            return GetParentTranslation(scope, text, CultureInfo.CurrentUICulture.Name);
        }



        private string GetParentTranslation(string scope, string text, string cultureName)
        {
            string scopedKey = (scope + "|" + text).ToLowerInvariant();
            string genericKey = ("|" + text).ToLowerInvariant();
            try
            {
                CultureInfo cultureInfo = CultureInfo.GetCultureInfo(cultureName);
                CultureInfo parentCultureInfo = cultureInfo.Parent;
                if (parentCultureInfo.IsNeutralCulture)
                {
                    CultureDictionary culture = LoadAndGetCulture(parentCultureInfo);
                    if (culture.ContainsKey(scopedKey))
                    {
                        return culture[scopedKey].Value;
                    }
                    if (culture.ContainsKey(genericKey))
                    {
                        return culture[genericKey].Value;
                    }
                    return text;
                }
            }
            catch (CultureNotFoundException) { }

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

        // TODO: Add culture name in the po file name to facilitate usage.
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
                string poLine, id, scope;
                id = scope = string.Empty;
                while ((poLine = reader.ReadLine()) != null)
                {
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

                    if (poLine.StartsWith("msgid"))
                    {
                        id = ParseId(poLine);
                        continue;
                    }

                    if (poLine.StartsWith("msgstr"))
                    {
                        string translation = ParseTranslation(poLine);
                        // ignore incomplete localizations (empty msgid or msgstr)
                        if (!string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(translation))
                        {
                            string scopedKey = (scope + "|" + id).ToLowerInvariant();
                            yield return new CultureValue { Key = scopedKey, Value = translation };
                        }
                        id = scope = string.Empty;
                    }
                }
            }
        }

        private static string ParseTranslation(string poLine)
        {
            return Unescape(poLine.Substring(6).Trim().Trim('"'));
        }

        private static string ParseId(string poLine)
        {
            return Unescape(poLine.Substring(5).Trim().Trim('"'));
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

            public string Value { get; set; }
            public string PluralValue { get; set; }

            /// <summary>
            /// such as
            /// msgstr[0] "s'ha trobat %d error fatal"
            ///msgstr[1] "s'han trobat %d errors fatals"
            /// </summary>
            public string[] PluralNValues { get; set; }
        }
    }
}

