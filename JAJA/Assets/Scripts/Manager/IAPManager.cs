using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
using System;

public class IAPManager : MonoBehaviour, IDetailedStoreListener
{
    public static IAPManager Instance;

    [Header("Configuration Store")]
    public string premiumProductID = "com.herocorp.jaja.premium";

    private IStoreController storeController;
    private IExtensionProvider extensionProvider;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        InitializePurchasing();
    }

    private void InitializePurchasing()
    {
        // V5 : Utilisation du module standard
        var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

        // Ajout du produit (Non-Consumable = achat à vie)
        builder.AddProduct(premiumProductID, ProductType.NonConsumable);

        // Lancement de l'initialisation
        UnityPurchasing.Initialize(this, builder);
    }

    // --- ACTIONS UTILISATEUR ---

    public void BuyPremium()
    {
        if (storeController == null)
        {
            Debug.LogError("IAP non initialisé !");
            return;
        }

        var product = storeController.products.WithID(premiumProductID);
        if (product != null && product.availableToPurchase)
        {
            Debug.Log("Lancement de l'achat : " + premiumProductID);
            storeController.InitiatePurchase(product);
        }
    }

    public void RestorePurchases()
    {
        if (storeController == null) return;

        // Condition spécifique pour Apple (Obligatoire pour l'App Store)
        if (Application.platform == RuntimePlatform.IPhonePlayer ||
            Application.platform == RuntimePlatform.OSXPlayer)
        {
            var apple = extensionProvider.GetExtension<IAppleExtensions>();
            apple.RestoreTransactions((result, error) =>
            {
                Debug.Log($"Restauration Apple terminée. Succès : {result}. Erreur : {error}");
            });
        }
        else
        {
            // Sur Android, on revérifie simplement l'état des reçus
            CheckAlreadyOwned();
            Debug.Log("Android : Vérification des achats effectuée.");
        }
    }

    // --- CALLBACKS INITIALISATION ---

    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        storeController = controller;
        extensionProvider = extensions;

        Debug.Log("Unity IAP Initialisé avec succès.");
        CheckAlreadyOwned();
    }

    // Remplace ta fonction CheckAlreadyOwned par celle-ci dans IAPManager.cs :
    private void CheckAlreadyOwned()
    {
        var product = storeController.products.WithID(premiumProductID);

        // En V5, Unity recommande de vérifier l'état de possession via cet état :
        if (product != null && product.hasReceipt)
        {
            // Note: 'hasReceipt' peut encore générer un warning car Unity est en transition.
            // C'est un warning "interne" au package que tu peux ignorer sans risque.
            PremiumManager.Instance.UnlockPremium();
        }
    }

    // --- CALLBACKS DE VENTE ---

    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
    {
        var productID = args.purchasedProduct.definition.id;

        if (string.Equals(productID, premiumProductID, StringComparison.Ordinal))
        {
            Debug.Log("Achat validé pour : " + productID);
            PremiumManager.Instance.UnlockPremium();
        }

        return PurchaseProcessingResult.Complete;
    }

    // C'EST CE CALLBACK QUI CORRIGE TON WARNING iOS
    public void OnPurchaseDeferred(Product product)
    {
        Debug.Log($"Achat en attente (Ask to Buy) pour : {product.definition.id}");
        // Ici, tu pourrais afficher un message : "En attente de validation parentale..."
    }

    // --- GESTION DES ERREURS ---

    public void OnInitializeFailed(InitializationFailureReason error)
    {
        OnInitializeFailed(error, null);
    }

    public void OnInitializeFailed(InitializationFailureReason error, string message)
    {
        Debug.LogError($"Échec Initialisation IAP : {error}. Message : {message}");
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
    {
        Debug.LogWarning($"Échec Achat de {product.definition.id} : {failureReason}");
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription)
    {
        Debug.LogWarning($"Échec Achat détaillé : {failureDescription.message}");
    }
}