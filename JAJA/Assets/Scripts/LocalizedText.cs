using UnityEngine;
using TMPro;

public class LocalizedText : MonoBehaviour
{
    public string localizationKey; // La clé à taper dans l'inspecteur (ex: btn_next)

    void Start()
    {
        UpdateText();
    }

    // Appelé au démarrage ou quand on change de langue
    public void UpdateText()
    {
        TMP_Text textComponent = GetComponent<TMP_Text>();
        if (textComponent != null && LocalizationManager.Instance != null)
        {
            textComponent.text = LocalizationManager.Instance.GetText(localizationKey);
        }
    }
}