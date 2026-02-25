using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class PremiumManager : MonoBehaviour
{
    public static PremiumManager Instance;

    [Header("UI Premium")]
    public Button buyPremiumButton;
    public TMP_Text buttonText;

    [Header("Effets Visuels")]
    public ParticleSystem fireworksLeft;  
    public ParticleSystem fireworksRight; 
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

    // NOUVEAU : On utilise Start pour s'assurer que les références UI sont prêtes
    void Start()
    {
        UpdateUIStatus();
    }

    private void InitializePremiumStatus()
    {
#if UNITY_EDITOR
        IsUserPremium = debugIsPremium;
#else
        IsUserPremium = PlayerPrefs.GetInt("IsPremium", 0) == 1;
#endif
    }

    // NOUVEAU : Méthode pour rafraîchir l'UI sans lancer d'effets
    private void UpdateUIStatus()
    {
        if (IsUserPremium)
        {
            if (buyPremiumButton != null) buyPremiumButton.interactable = false;
            if (buttonText != null) buttonText.text = "Mode Premium débloqué !";
        }
    }

    public void UnlockPremium()
    {
        // Si l'utilisateur est déjà premium, on ne fait rien (sécurité)
        if (IsUserPremium) return;

        IsUserPremium = true;
        PlayerPrefs.SetInt("IsPremium", 1);
        PlayerPrefs.Save();

        // On lance les effets uniquement lors de l'achat/déblocage actif
        StartCoroutine(PlayPremiumEffects());

        if (GoogleSheetLoader.Instance != null)
        {
            StartCoroutine(GoogleSheetLoader.Instance.ReloadForPremium());
        }

        // On met à jour l'UI
        UpdateUIStatus();
    }

    private IEnumerator PlayPremiumEffects()
    {
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
        
        // Optionnel : remettre le bouton en état cliquable
        if (buyPremiumButton != null) buyPremiumButton.interactable = true;
        if (buttonText != null) buttonText.text = "Acheter le Premium"; 
    }
}