using UnityEngine;
using TMPro;
using System.Collections;

public class LocalizedText : MonoBehaviour
{
    public string localizationKey; // La clé à taper dans l'inspecteur (ex: btn_next)

    IEnumerator Start()
    {
        // OPTIMISATION : On attend uniquement que l'INTERFACE soit prête
        // (plus rapide que d'attendre toutes les questions)
        while (GoogleSheetLoader.Instance == null || !GoogleSheetLoader.Instance.isInterfaceReady)
            yield return null;

        UpdateText();
    }

    public void UpdateText()
    {
        TMP_Text textComponent = GetComponent<TMP_Text>();
        if (textComponent != null && LocalizationManager.Instance != null)
            textComponent.text = LocalizationManager.Instance.GetText(localizationKey);
    }

    void OnDestroy()
    {
        // Invalider le cache de RefreshAllTexts quand un objet est détruit
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.InvalidateTextCache();
    }
}