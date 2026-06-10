using UnityEngine;

[CreateAssetMenu(fileName = "ShopItem", menuName = "TreasureLoftOfFate/Shop Item")]
public class ShopItemDefinition : ScriptableObject
{
    [SerializeField] private string itemId;
    [SerializeField] private string displayName;
    [SerializeField] private int price = 100;
    [SerializeField] private Sprite icon;
    [SerializeField] [TextArea] private string description;
    [SerializeField] private bool unlockedByDefault;
    [SerializeField] private ShopItemRarity rarity = ShopItemRarity.Common;
    [SerializeField] private int attack;
    [SerializeField] private int defense;
    [SerializeField] private int movementSpeed;

    public string ItemId => string.IsNullOrWhiteSpace(itemId) ? name : itemId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public int Price => Mathf.Max(0, price);
    public Sprite Icon => icon;
    public string Description => description;
    public bool UnlockedByDefault => unlockedByDefault;
    public ShopItemRarity Rarity => rarity;
    public int Attack => attack;
    public int Defense => defense;
    public int MovementSpeed => movementSpeed;

    // Setter methods for Editor script usage
    public void SetItemId(string value) { itemId = value; }
    public void SetDisplayName(string value) { displayName = value; }
    public void SetPrice(int value) { price = Mathf.Max(0, value); }
    public void SetIcon(Sprite value) { icon = value; }
    public void SetDescription(string value) { description = value; }
    public void SetUnlockedByDefault(bool value) { unlockedByDefault = value; }
    public void SetRarity(ShopItemRarity value) { rarity = value; }
    public void SetAttack(int value) { attack = value; }
    public void SetDefense(int value) { defense = value; }
    public void SetMovementSpeed(int value) { movementSpeed = value; }

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            itemId = name;
        }

        price = Mathf.Max(0, price);
    }
}
