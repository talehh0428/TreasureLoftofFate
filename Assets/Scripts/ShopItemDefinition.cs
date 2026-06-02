/*  
商品静态配置资源 
*/

using UnityEngine;

[CreateAssetMenu(fileName = "ShopItem", menuName = "TreasureLoftOfFate/Shop Item")]
public class ShopItemDefinition : ScriptableObject
{
    [SerializeField] private string itemId;
    [SerializeField] private string displayName;
    [SerializeField] private int price = 100;
    [SerializeField] private Sprite icon;
    [SerializeField] [TextArea] private string description;
    [SerializeField] private ShopItemRarity rarity = ShopItemRarity.Common;
    [SerializeField] private int attack;
    [SerializeField] private int defense;
    [SerializeField] private int movementSpeed;

    public string ItemId => string.IsNullOrWhiteSpace(itemId) ? name : itemId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public int Price => Mathf.Max(0, price);
    public Sprite Icon => icon;
    public string Description => description;
    public ShopItemRarity Rarity => rarity;
    public int Attack => attack;
    public int Defense => defense;
    public int MovementSpeed => movementSpeed;

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            itemId = name;
        }

        price = Mathf.Max(0, price);
    }
}