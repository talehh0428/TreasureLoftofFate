using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class ImportShopItemsFromJson
{
    private const string JsonPath = "Assets/Text/商品数据.json";
    private const string AssetFolder = "Assets/Resources/ShopItem";
    private const string IconFolder = "Assets/Images/ShopItemImage";

    [MenuItem("Tools/Temp/Import Shop Items From Json")]
    public static void Import()
    {
        if (!File.Exists(JsonPath))
        {
            Debug.LogError($"找不到商品数据 JSON: {JsonPath}");
            return;
        }

        EnsureFolder(AssetFolder);

        List<ShopItemJson> items = LoadItems();
        Dictionary<string, ShopItemDefinition> existingById = LoadExistingItems();
        List<string> updated = new List<string>();
        List<string> created = new List<string>();

        foreach (ShopItemJson item in items)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.itemId))
            {
                continue;
            }

            string itemId = item.itemId.Trim();
            bool hasExisting = existingById.TryGetValue(itemId, out ShopItemDefinition definition);
            if (!hasExisting)
            {
                definition = ScriptableObject.CreateInstance<ShopItemDefinition>();
                string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{AssetFolder}/ShopItem_{SanitizeFileName(itemId)}.asset");
                AssetDatabase.CreateAsset(definition, assetPath);
                existingById[itemId] = definition;
                created.Add(itemId);
            }
            else
            {
                updated.Add(itemId);
            }

            ApplyItem(definition, item, itemId);
            EditorUtility.SetDirty(definition);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            $"商品导入完成。覆盖 {updated.Count} 个: {FormatList(updated)}\n" +
            $"新建 {created.Count} 个: {FormatList(created)}");
    }

    private static List<ShopItemJson> LoadItems()
    {
        string json = File.ReadAllText(JsonPath);
        json = json.Replace("\"Unlocked By Default\"", "\"unlockedByDefault\"");
        ShopItemJsonWrapper wrapper = JsonUtility.FromJson<ShopItemJsonWrapper>("{\"items\":" + json + "}");
        return wrapper != null && wrapper.items != null
            ? wrapper.items.ToList()
            : new List<ShopItemJson>();
    }

    private static Dictionary<string, ShopItemDefinition> LoadExistingItems()
    {
        string[] guids = AssetDatabase.FindAssets("t:ShopItemDefinition", new[] { AssetFolder });
        Dictionary<string, ShopItemDefinition> items = new Dictionary<string, ShopItemDefinition>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ShopItemDefinition definition = AssetDatabase.LoadAssetAtPath<ShopItemDefinition>(path);
            if (definition == null || string.IsNullOrWhiteSpace(definition.ItemId))
            {
                continue;
            }

            items[definition.ItemId] = definition;
        }

        return items;
    }

    private static void ApplyItem(ShopItemDefinition definition, ShopItemJson item, string itemId)
    {
        definition.name = $"ShopItem_{itemId}";
        definition.SetItemId(itemId);
        definition.SetDisplayName(item.displayName);
        definition.SetPrice(item.price);
        definition.SetIcon(LoadIcon(itemId));
        definition.SetDescription(item.description);
        definition.SetUnlockedByDefault(item.unlockedByDefault);
        definition.SetRarity(ParseRarity(item.rarity));
        definition.SetAttack(item.attack);
        definition.SetDefense(item.defense);
        definition.SetMovementSpeed(item.movementSpeed);
    }

    private static Sprite LoadIcon(string itemId)
    {
        string iconPath = $"{IconFolder}/{itemId}.png";
        Sprite icon = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
        if (icon == null)
        {
            Debug.LogWarning($"未找到商品图标: {iconPath}");
        }

        return icon;
    }

    private static ShopItemRarity ParseRarity(string value)
    {
        return Enum.TryParse(value, true, out ShopItemRarity rarity)
            ? rarity
            : ShopItemRarity.Common;
    }

    private static void EnsureFolder(string folder)
    {
        string[] parts = folder.Split('/');
        string current = parts[0];
        for (int index = 1; index < parts.Length; index++)
        {
            string next = $"{current}/{parts[index]}";
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[index]);
            }

            current = next;
        }
    }

    private static string SanitizeFileName(string value)
    {
        char[] invalidChars = Path.GetInvalidFileNameChars();
        return new string(value.Select(character => invalidChars.Contains(character) ? '_' : character).ToArray());
    }

    private static string FormatList(List<string> values)
    {
        return values.Count == 0 ? "无" : string.Join(", ", values);
    }

    [Serializable]
    private class ShopItemJsonWrapper
    {
        public ShopItemJson[] items;
    }

    [Serializable]
    private class ShopItemJson
    {
        public string itemId;
        public string displayName;
        public bool unlockedByDefault;
        public int attack;
        public int defense;
        public int movementSpeed;
        public string rarity;
        public string description;
        public int price;
    }
}
