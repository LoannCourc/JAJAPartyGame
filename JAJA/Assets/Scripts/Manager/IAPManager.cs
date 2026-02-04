using UnityEngine;
using UnityEngine.Purchasing;
using System;
using System.Collections.Generic;

// On utilise IStoreListener tout court, IDetailedStoreListener est devenu obsolète
public class IAPManager : MonoBehaviour, IStoreListener
{
    public static IAPManager Instance;
    public string premiumProductID = "com.herocorp.jaja.premium";

    private IStoreController storeController;
    private IExtensionProvider extensionProvider;

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else Destroy(gameObject);
    }

    void Start()
    {
        // V5 : Utilisation de StandardPurchasingModule.Instance() directement
        var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
        builder.AddProduct(premiumProductID, ProductType.NonConsumable);

        // Initialisation V5
        UnityPurchasing.Initialize(this, builder);
    }

    public void BuyPremium()
    {
        if (storeController == null) return;

        var product = storeController.products.WithID(premiumProductID);
        if (product != null && product.availableToPurchase)
        {
            storeController.InitiatePurchase(product);
        }
    }

    // --- RESTAURATION VERSION V5 ---
    public void RestorePurchases()
    {
        if (storeController == null) return;

        if (Application.platform == RuntimePlatform.IPhonePlayer || 
            Application.platform == RuntimePlatform.OSXPlayer)
        {
            // V5 : Nouvelle façon d'appeler les extensions Apple
            var apple = extensionProvider.GetExtension<IAppleExtensions>();
            apple.RestoreTransactions((result, error) => {
                Debug.Log($"Restauration Apple : {result}. " + (error != null ? $"Erreur: {error}" : ""));
            });
        }
        else
        {
            CheckAlreadyOwned();
        }
    }

    // --- CALLBACKS V5 ---

    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        storeController = controller;
        extensionProvider = extensions;
        CheckAlreadyOwned();
    }

    private void CheckAlreadyOwned()
    {
        var product = storeController.products.WithID(premiumProductID);
        // En V5, on vérifie hasReceipt au lieu de transactionID pour plus de fiabilité
        if (product != null && product.hasReceipt)
        {
            PremiumManager.Instance.UnlockPremium();
        }
    }

    // ProcessPurchase n'a pas changé de nom mais les arguments internes si
    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
    {
        var product = args.purchasedProduct;

        if (string.Equals(product.definition.id, premiumProductID, StringComparison.Ordinal))
        {
            PremiumManager.Instance.UnlockPremium();
        }

        return PurchaseProcessingResult.Complete;
    }

    // Gestion des échecs V5
    public void OnInitializeFailed(InitializationFailureReason error) 
    {
        OnInitializeFailed(error, null);
    }

    public void OnInitializeFailed(InitializationFailureReason error, string message)
    {
        Debug.LogError($"IAP Init Failed: {error}. Message: {message}");
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
    {
        Debug.LogError($"Achat échoué pour {product.definition.id} : {failureReason}");
    }
}