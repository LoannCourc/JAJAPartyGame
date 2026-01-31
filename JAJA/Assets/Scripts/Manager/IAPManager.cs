using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
using System;

public class IAPManager : MonoBehaviour, IDetailedStoreListener
{
    public static IAPManager Instance;
    public string premiumProductID = "com.herocorp.jaja.premium";

    private IStoreController storeController;

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else Destroy(gameObject);
    }

    void Start()
    {
        var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
        builder.AddProduct(premiumProductID, ProductType.NonConsumable);
        UnityPurchasing.Initialize(this, builder);
    }

    public void BuyPremium()
{
    if (storeController == null)
    {
        Debug.LogError("IAP non initialisé ! Vérifie ta connexion internet ou ta clé de licence.");
        return;
    }

    var product = storeController.products.WithID(premiumProductID);
    if (product == null)
    {
        Debug.LogError($"Produit {premiumProductID} introuvable dans le store controller.");
        return;
    }

    if (!product.availableToPurchase)
    {
        Debug.LogError("Le produit est trouvé mais Google dit qu'il n'est pas disponible à l'achat.");
        return;
    }

    Debug.Log("Lancement de l'achat...");
    storeController.InitiatePurchase(premiumProductID);
}
    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        storeController = controller;
        
        // Nouvelle méthode V5 pour vérifier l'achat existant
        var product = storeController.products.WithID(premiumProductID);
        if (product != null)
        {
            // En V5, on vérifie si le produit appartient à l'utilisateur via transactionID ou le reçu
            if (!string.IsNullOrEmpty(product.transactionID))
            {
                Debug.Log("Produit Premium déjà possédé.");
                PremiumManager.Instance.UnlockPremium();
            }
        }
    }

    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
    {
        if (string.Equals(args.purchasedProduct.definition.id, premiumProductID, StringComparison.Ordinal))
        {
            PremiumManager.Instance.UnlockPremium();
        }
        return PurchaseProcessingResult.Complete;
    }

    public void OnInitializeFailed(InitializationFailureReason error) { }
    public void OnInitializeFailed(InitializationFailureReason error, string message) { }
    public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason) { }
    public void OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription) { }
}