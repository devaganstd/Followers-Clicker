using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopItemUI : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI quantityText;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private TextMeshProUGUI productionText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Image iconImage;
    [SerializeField] private Button purchaseButton;

    private string itemId;
    private GameEngine gameEngine;

    private void Awake()
    {
        gameEngine = FindObjectOfType<GameEngine>();

        if (purchaseButton != null)
        {
            purchaseButton.onClick.AddListener(OnPurchaseClicked);
        }
    }

    public void Initialize(string id)
    {
        itemId = id;
    }

    public void SetName(string name)
    {
        if (nameText != null)
            nameText.text = name;
    }

    public void SetQuantity(int quantity)
    {
        if (quantityText != null)
            quantityText.text = quantity.ToString();
    }

    public void SetCost(string cost)
    {
        if (costText != null)
            costText.text = cost;
    }

    public void SetProductionInfo(string info)
    {
        if (productionText != null)
            productionText.text = info;
    }

    public void SetDescription(string description)
    {
        if (descriptionText != null)
            descriptionText.text = description;
    }

    public void SetIcon(Sprite icon)
    {
        if (iconImage != null)
            iconImage.sprite = icon;
    }

    public void SetInteractable(bool interactable)
    {
        if (purchaseButton != null)
            purchaseButton.interactable = interactable;
    }

    private void OnPurchaseClicked()
    {
        if (gameEngine != null && !string.IsNullOrEmpty(itemId))
        {
            gameEngine.PurchaseItem(itemId);
        }
    }
}
