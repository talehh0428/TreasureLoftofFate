using UnityEngine;

[CreateAssetMenu(fileName = "ShopItem", menuName = "TreasureLoftOfFate/Shop Item")]
public class ShopItemDefinition : ScriptableObject
{
    [SerializeField] private string itemId;
    [SerializeField] private string displayName;
    [SerializeField] private int price = 100;
    [SerializeField] private Sprite icon;
    [SerializeField] [TextArea] private string description;

    public string ItemId
    {
        get
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return name;
            }

            return itemId;
        }
    }

    public string DisplayName
    {
        get
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                return name;
            }

            return displayName;
        }
    }

    public int Price => Mathf.Max(0, price);

    public Sprite Icon => icon;

    public string Description => description;

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            itemId = name;
        }

        price = Mathf.Max(0, price);
    }
}
