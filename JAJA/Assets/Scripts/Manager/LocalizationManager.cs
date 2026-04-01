using UnityEngine;
using System.Collections.Generic;

public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance;

    [Header("Configuration")]
    public string currentLang = "FR";

    private Dictionary<string, string> uiTexts = new Dictionary<string, string>();

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); return; }

        currentLang = PlayerPrefs.GetString("Language", Application.systemLanguage == SystemLanguage.French ? "FR" : "EN");
    }

    public void SetLanguage(string lang)
    {
        currentLang = lang;
        PlayerPrefs.SetString("Language", lang);
    }

    public void LoadInterfaceTexts(Dictionary<string, string> texts)
    {
        uiTexts = texts;
    }

    public string GetText(string key)
    {
        if (string.IsNullOrEmpty(key)) return "";
        if (uiTexts.ContainsKey(key)) return uiTexts[key];
        return key; // Retourne la clé par défaut si non trouvée
    }

    public void RefreshAllTexts()
    {
        LocalizedText[] allTexts = FindObjectsOfType<LocalizedText>(true);
        foreach (var t in allTexts) t.UpdateText();
    }
}