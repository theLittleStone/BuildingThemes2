namespace BuildingThemes.TranslationFramework
{
    public interface ILanguageDeserializer
    {
        ILanguage DeserialiseLanguage(string fileName);
    }
}
