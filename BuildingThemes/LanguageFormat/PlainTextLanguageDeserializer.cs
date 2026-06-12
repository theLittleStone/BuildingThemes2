using System.Collections.Generic;
using System.IO;
using BuildingThemes.TranslationFramework;

namespace BuildingThemes.LanguageFormat
{
    /// <summary>
    /// Reads "KEY value" plain-text language files; the locale name is the file
    /// name without extension (e.g. en.txt -> "en").
    /// </summary>
    public class PlainTextLanguageDeserializer : ILanguageDeserializer
    {
        public ILanguage DeserialiseLanguage(string fileName)
        {
            var fileInfo = new FileInfo(fileName);
            var localeName = fileInfo.Name.Replace(".txt", "");
            Debugger.Log("Loading localization file: " + fileName + ". Detected locale name: " + localeName);
            return new LanguageDictionaryWrapper(localeName, Load(fileName));
        }

        private static Dictionary<string, string> Load(string path)
        {
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            if (File.Exists(path))
            {
                foreach (string readAllLine in File.ReadAllLines(path))
                {
                    if (readAllLine != null)
                    {
                        string str = readAllLine.Trim();
                        if (str.Length != 0)
                        {
                            int length = str.IndexOf(' ');
                            if (length > 0 && !dictionary.ContainsKey(str.Substring(0, length)))
                                dictionary.Add(str.Substring(0, length), str.Substring(length + 1).Replace("\\n", "\n"));
                        }
                    }
                }
            }
            else
            {
                Debugger.Log("Localization file: " + path + " doesn't exist!");
            }
            return dictionary;
        }
    }
}
