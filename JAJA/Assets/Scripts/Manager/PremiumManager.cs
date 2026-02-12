using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections; // Nécessaire pour les Coroutines

public class PremiumManager : MonoBehaviour
{
    public static PremiumManager Instance;

    [Header("UI Premium")]
    public Button buyPremiumButton;
    public TMP_Text buttonText;

    [Header("Effets Visuels")]
    public ParticleSystem fireworksLeft;  // Glisse ton premier système de particules ici
    public ParticleSystem fireworksRight; // Glisse ton deuxième système de particules ici
    [Tooltip("Délai entre les 3 explosions de particules")]
    public float burstDelay = 0.3f;

    [Header("Réglages Développeur (Éditeur)")]
    public bool debugIsPremium = false;

    [Header("Configuration Mode Gratuit")]
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

        // Lancer les feux d'artifice
        StartCoroutine(PlayPremiumEffects());

        if (GoogleSheetLoader.Instance != null)
        {
            StartCoroutine(GoogleSheetLoader.Instance.ReloadForPremium());
        }

        if (buyPremiumButton != null)
        {
            buyPremiumButton.interactable = false;
        }

        if (buttonText != null)
        {
            buttonText.text = "Mode Premium débloqué !";
        }    
    }

    // Coroutine pour jouer les particules 3 fois rapidement
    private IEnumerator PlayPremiumEffects()
    {
        Debug.Log("Je passe icic");

        for (int i = 0; i < 3; i++)
        {
            if (fireworksLeft != null) fireworksLeft.Play();
            if (fireworksRight != null) fireworksRight.Play();

            yield return new WaitForSeconds(burstDelay);
        }
    }

    public void ResetPurchase()
    {
        PlayerPrefs.DeleteKey("IsPremium");
        IsUserPremium = false;
    }
}