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

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else Destroy(gameObject);

        LoadSettings();
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

        // Si on a lié les toggles dans l'inspector, on met à jour leur visuel
        if (sipsToggle) sipsToggle.isOn = showSips;
        if (customToggle) customToggle.isOn = onlyCustomQuestions;
        if (vibroToggle) vibroToggle.isOn = vibrationsEnabled;
    }

    // --- FONCTIONS POUR LES BOUTONS ---
    public void ToggleSips(bool value) { showSips = value; SaveSettings(); }
    public void ToggleCustom(bool value) { onlyCustomQuestions = value; SaveSettings(); }
    public void ToggleVibro(bool value) { vibrationsEnabled = value; SaveSettings(); }

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