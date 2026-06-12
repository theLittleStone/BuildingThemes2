using BuildingThemes.LanguageFormat;
using BuildingThemes.TranslationFramework;

namespace BuildingThemes
{
    public static class Localization
    {
        private static readonly LocalizationManager LocalizationManager =
            new LocalizationManager(typeof(BuildingThemesMod), new PlainTextLanguageDeserializer());

        public static string Get(string translationId)
        {
            return LocalizationManager.GetTranslation(translationId);
        }

        public static string Get(string translationId, params object[] args)
        {
            return string.Format(LocalizationManager.GetTranslation(translationId), args);
        }
    }
}
