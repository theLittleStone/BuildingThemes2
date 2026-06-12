using System;
using System.Collections.Generic;
using System.IO;
using ColossalFramework.Globalization;

namespace BuildingThemes.TranslationFramework
{
    /// <summary>
    /// Handles localisation for a mod. Loads all language files from the mod's
    /// Locale folder and serves translations for the game's current language,
    /// falling back to <see cref="fallbackLanguage"/> when no match exists.
    /// </summary>
    public class LocalizationManager
    {
        protected List<ILanguage> _languages = new List<ILanguage>();
        protected ILanguage _currentLanguage = null;
        protected bool _languagesLoaded = false;
        protected bool _loadLanguageAutomatically = true;
        private string fallbackLanguage;
        private ILanguageDeserializer languageDeserializer;
        private Type modType;

        public LocalizationManager(Type modType, ILanguageDeserializer languageDeserializer,
            bool loadLanguageAutomatically = true, string fallbackLanguage = "en")
        {
            this.languageDeserializer = languageDeserializer;
            this._loadLanguageAutomatically = loadLanguageAutomatically;
            this.fallbackLanguage = fallbackLanguage;
            this.modType = modType;
            LocaleManager.eventLocaleChanged += SetCurrentLanguage;
        }

        private void SetCurrentLanguage()
        {
            if (_languages == null || _languages.Count == 0 || !LocaleManager.exists)
            {
                return;
            }
            _currentLanguage = _languages.Find(l => l.LocaleName() == LocaleManager.instance.language) ??
                               _languages.Find(l => l.LocaleName() == fallbackLanguage);
        }

        /// <summary>
        /// Loads all languages up if not already loaded.
        /// </summary>
        public void LoadLanguages()
        {
            if (!_languagesLoaded && _loadLanguageAutomatically)
            {
                _languagesLoaded = true;
                RefreshLanguages();
                SetCurrentLanguage();
            }
        }

        /// <summary>
        /// Forces a reload of the languages, even if they're already loaded.
        /// </summary>
        public void RefreshLanguages()
        {
            _languages.Clear();

            string basePath = TranslationUtil.AssemblyPath(modType);

            if (basePath != "")
            {
                string languagePath = basePath + Path.DirectorySeparatorChar + "Locale";

                if (Directory.Exists(languagePath))
                {
                    string[] languageFiles = Directory.GetFiles(languagePath);

                    foreach (string languageFile in languageFiles)
                    {
                        ILanguage loadedLanguage = null;
                        try
                        {
                            loadedLanguage = languageDeserializer.DeserialiseLanguage(languageFile);
                        }
                        catch (Exception e)
                        {
                            UnityEngine.Debug.LogError(
                                "Error happened when deserializing language file " + languageFile);
                            UnityEngine.Debug.LogException(e);
                        }
                        if (loadedLanguage != null)
                        {
                            _languages.Add(loadedLanguage);
                        }
                    }
                }
                else
                {
                    UnityEngine.Debug.LogWarning("Can't load any localisation files");
                }
            }
        }

        private bool HasTranslation(string translationId)
        {
            LoadLanguages();

            if (translationId == null)
            {
                return true;
            }

            return _currentLanguage != null && _currentLanguage.HasTranslation(translationId);
        }

        /// <summary>
        /// Gets a translation for a specific translation ID. Returns the ID itself
        /// when no translation is available so missing keys stay readable in the UI.
        /// </summary>
        public string GetTranslation(string translationId)
        {
            LoadLanguages();

            if (translationId == null)
            {
                return "null";
            }

            string translatedText = translationId;

            if (_currentLanguage != null)
            {
                if (HasTranslation(translationId))
                {
                    translatedText = _currentLanguage.GetTranslation(translationId);
                }
                else
                {
                    UnityEngine.Debug.LogWarning("Returned translation for language \"" + _currentLanguage.LocaleName() + "\" doesn't contain a suitable translation for \"" + translationId + "\"");
                }
            }
            else
            {
                UnityEngine.Debug.LogWarning("Can't get a translation for \"" + translationId + "\" as there is not a language defined");
            }

            return translatedText;
        }
    }
}
