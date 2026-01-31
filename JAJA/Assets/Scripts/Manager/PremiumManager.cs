using UnityEngine;

public class PremiumManager : MonoBehaviour
{
    public static PremiumManager Instance;

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
            StartCoroutine(GoogleSheetLoader.Instance.ReloadForPremium());
    }

    public void ResetPurchase()
    {
        PlayerPrefs.DeleteKey("IsPremium");
        IsUserPremium = false;
    }
}