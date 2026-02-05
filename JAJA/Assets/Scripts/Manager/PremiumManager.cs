using UnityEngine;
using UnityEngine.UI; // Pour manipuler le Button
using TMPro;

public class PremiumManager : MonoBehaviour
{
    public static PremiumManager Instance;

    [Header("UI Premium")]
    public Button buyPremiumButton;      // Glisse ton bouton ici dans l'inspecteur
    public TMP_Text buttonText;

    [Header("Réglages Développeur (Éditeur)")]
    [Tooltip("Coche pour simuler le Premium dans l'éditeur Unity.")]
    public bool debugIsPremium = false;

    [Header("Configuration Mode Gratuit")]
    [Tooltip("Nombre MAX de questions par difficulté pour les gratuits.")]
    public int maxFreeQuestionsCap = 30;
    public int maxFreeCustomQuestions = 3;

    public bool IsUserPremium { get; private set; }

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else Destroy(gameObject);

        InitializePremiumStatus();
    }

    private void InitializePremiumStatus()
    {
#if UNITY_EDITOR
            IsUserPremium = debugIsPremium;
#else
        IsUserPremium = PlayerPrefs.GetInt("IsPremium", 0) == 1;
#endif
    }

    public void UnlockPremium()
    {
        IsUserPremium = true;
        PlayerPrefs.SetInt("IsPremium", 1);
        PlayerPrefs.Save();

        if (GoogleSheetLoader.Instance != null)
            {StartCoroutine(GoogleSheetLoader.Instance.ReloadForPremium());}


        if (buyPremiumButton != null)
        {
            buyPremiumButton.interactable = false; // Désactive le bouton
        }

        if (buttonText != null)
        {
            buttonText.text = "Mode Premium débloqué !"; // Change le texte
        }    
    }

    public void ResetPurchase()
    {
        PlayerPrefs.DeleteKey("IsPremium");
        IsUserPremium = false;
    }
}