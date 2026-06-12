namespace BuildingThemes.TranslationFramework
{
    public interface ILanguage
    {
        bool HasTranslation(string id);

        string GetTranslation(string id);

        string LocaleName();
    }
}
