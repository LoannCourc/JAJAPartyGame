using UnityEngine;
using System.Collections.Generic;

public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance;

    [Header("Configuration")]
    public string currentLang = "FR";
    public event System.Action OnLanguageRefreshed;

    // Dictionnaire pour l'interface : Dictionary<Langue, Dictionary<Clé, Texte>>
    private Dictionary<string, Dictionary<string, string>> allTranslations
        = new Dictionary<string, Dictionary<string, string>>();

    // Cache des composants LocalizedText pour éviter FindObjectsOfType à répétition
    private LocalizedText[] _cachedLocalizedTexts;
    private bool _cacheStale = true;

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); return; }

        if (PlayerPrefs.HasKey("Language"))
            currentLang = PlayerPrefs.GetString("Language");
    }

    public void SetLanguage(string lang)
    {
        currentLang = lang.ToUpper();
        PlayerPrefs.SetString("Language", currentLang);
        PlayerPrefs.Save();

        RefreshAllTexts();

        if (GameplayManager.Instance != null)
            GameplayManager.Instance.RefreshCurrentQuestion();
    }

    public void LoadAllInterfaceTexts(Dictionary<string, Dictionary<string, string>> data)
    {
        allTranslations = data;
        _cacheStale = true;  // Invalider le cache des composants
        RefreshAllTexts();
    }

    public string GetText(string key)
    {
        if (string.IsNullOrEmpty(key)) return "";

        if (allTranslations.TryGetValue(currentLang, out var langDict) && langDict.TryGetValue(key, out var text))
            return text;

        return key;
    }

    /// <summary>
    /// Expose les traductions brutes pour la sauvegarde dans le cache local.
    /// </summary>
    public Dictionary<string, Dictionary<string, string>> GetAllTranslations()
        => allTranslations;

    public void RefreshAllTexts()
    {
        // Invalider le cache si les objets de la scène ont changé
        if (_cacheStale)
        {
            _cachedLocalizedTexts = FindObjectsOfType<LocalizedText>(true);
            _cacheStale = false;
        }

        foreach (var t in _cachedLocalizedTexts)
        {
            if (t != null) t.UpdateText();
        }
        OnLanguageRefreshed?.Invoke();
    }

    /// <summary>
    /// Appeler depuis un LocalizedText quand il est détruit (OnDestroy)
    /// pour invalider le cache automatiquement.
    /// </summary>
    public void InvalidateTextCache() => _cacheStale = true;
}