using UnityEngine;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance;

    [Header("États des Options")]
    public bool showSips;
    public bool onlyCustomQuestions;
    public bool vibrationsEnabled;

    [Header("UI Toggles (Optionnel)")]
    public Toggle sipsToggle;
    public Toggle customToggle;
    public Toggle vibroToggle;

    [Header("Liens Légaux")]
    // Tu peux modifier ces liens directement dans l'Inspector Unity
    public string privacyPolicyURL = "https://doc-hosting.flycricket.io/jaja-jeux-de-soiree-privacy-policy/ae8ffeef-9381-43fd-b23d-89bc45b80a8e/privacy";
    public string termsOfUseURL = "https://doc-hosting.flycricket.io/jaja-jeux-de-soiree-terms-of-use/fdbc55a6-1f64-4c58-b808-72d4f0a23583/terms";

    [Header("Réseaux Sociaux")]
    // Modifie ces liens directement dans l'Inspector Unity
    public string instagramURL = "https://www.instagram.com/ton_compte/";
    public string tiktokURL = "https://www.tiktok.com/@ton_compte";
    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else Destroy(gameObject);

        LoadSettings();
    }
    public void OpenInstagram()
    {
        if (!string.IsNullOrEmpty(instagramURL))
        {
            Application.OpenURL(instagramURL);
        }
    }

    public void OpenTikTok()
    {
        if (!string.IsNullOrEmpty(tiktokURL))
        {
            Application.OpenURL(tiktokURL);
        }
    }

    // --- SAUVEGARDE ET CHARGEMENT ---
    public void SaveSettings()
    {
        PlayerPrefs.SetInt("ShowSips", showSips ? 1 : 0);
        PlayerPrefs.SetInt("OnlyCustom", onlyCustomQuestions ? 1 : 0);
        PlayerPrefs.SetInt("VibroEnabled", vibrationsEnabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void LoadSettings()
    {
        showSips = PlayerPrefs.GetInt("ShowSips", 1) == 1;
        onlyCustomQuestions = PlayerPrefs.GetInt("OnlyCustom", 0) == 1;
        vibrationsEnabled = PlayerPrefs.GetInt("VibroEnabled", 1) == 1;

        if (sipsToggle) sipsToggle.isOn = showSips;
        if (customToggle) customToggle.isOn = onlyCustomQuestions;
        if (vibroToggle) vibroToggle.isOn = vibrationsEnabled;
    }

    // --- FONCTIONS POUR LES BOUTONS ---
    public void ToggleSips(bool value) { showSips = value; SaveSettings(); }
    public void ToggleCustom(bool value) { onlyCustomQuestions = value; SaveSettings(); }
    public void ToggleVibro(bool value) { vibrationsEnabled = value; SaveSettings(); }

    // Nouvelles fonctions pour ouvrir les liens
    public void OpenPrivacyPolicy()
    {
        if (!string.IsNullOrEmpty(privacyPolicyURL))
        {
            Application.OpenURL(privacyPolicyURL);
        }
    }

    public void OpenTermsOfUse()
    {
        if (!string.IsNullOrEmpty(termsOfUseURL))
        {
            Application.OpenURL(termsOfUseURL);
        }
    }

    // Utilitaire pour la vibration
    public void TriggerVibration()
    {
        if (vibrationsEnabled)
        {
            #if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
            #endif
        }
    }
}