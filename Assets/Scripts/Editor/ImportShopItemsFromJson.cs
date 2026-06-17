using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class ImportShopItemsFromJson
{
    private const string JsonPath = "Assets/Text/修仙作品物品数据.json";
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
            if (item == null || string.IsNullOrWhiteSpace(item.ItemId))
            {
                continue;
            }

            string itemId = item.ItemId.Trim();
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
        json = NormalizeJsonKeys(json);
        ShopItemJsonWrapper wrapper = JsonUtility.FromJson<ShopItemJsonWrapper>("{\"items\":" + json + "}");
        return wrapper != null && wrapper.items != null
            ? wrapper.items.Where(item => item != null).ToList()
            : new List<ShopItemJson>();
    }

    private static string NormalizeJsonKeys(string json)
    {
        json = json.Replace("\"ItemId\"", "\"itemId\"");
        json = json.Replace("\"ItemID\"", "\"itemId\"");
        json = json.Replace("\"itemID\"", "\"itemId\"");
        json = json.Replace("\"DisplayName\"", "\"displayName\"");
        json = json.Replace("\"Price\"", "\"price\"");
        json = json.Replace("\"Icon\"", "\"icon\"");
        json = json.Replace("\"Description\"", "\"description\"");
        json = json.Replace("\"Unlocked By Default\"", "\"unlockedByDefault\"");
        json = json.Replace("\"UnlockedByDefault\"", "\"unlockedByDefault\"");
        json = json.Replace("\"Rarity\"", "\"rarity\"");
        json = json.Replace("\"Attack\"", "\"attack\"");
        json = json.Replace("\"Defense\"", "\"defense\"");
        json = json.Replace("\"MovementSpeed\"", "\"movementSpeed\"");
        return json;
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
        definition.SetDisplayName(item.DisplayName);
        definition.SetPrice(item.Price);
        definition.SetIcon(LoadIcon(itemId, item.Icon, definition.Icon));
        definition.SetDescription(item.Description);
        definition.SetUnlockedByDefault(item.UnlockedByDefault);
        definition.SetRarity(ParseRarity(item.Rarity));
        definition.SetAttack(item.Attack);
        definition.SetDefense(item.Defense);
        definition.SetMovementSpeed(item.MovementSpeed);
    }

    private static Sprite LoadIcon(string itemId, string jsonIcon, Sprite fallbackIcon)
    {
        Sprite icon = LoadIconByName(itemId);
        if (icon != null)
        {
            return icon;
        }

        if (!string.IsNullOrWhiteSpace(jsonIcon))
        {
            icon = LoadIconByName(Path.GetFileNameWithoutExtension(jsonIcon.Trim()));
            if (icon != null)
            {
                return icon;
            }
        }

        Debug.LogWarning($"未找到商品图标: {IconFolder}/{itemId}.*");
        return fallbackIcon;
    }

    private static Sprite LoadIconByName(string iconName)
    {
        string[] guids = AssetDatabase.FindAssets($"{iconName} t:Sprite", new[] { IconFolder });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path) != iconName)
            {
                continue;
            }

            Sprite icon = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (icon != null)
            {
                return icon;
            }
        }

        return null;
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
#pragma warning disable 0649
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
        public string icon;
        public string description;
        public int price;

        public string ItemId => itemId;
        public string DisplayName => displayName;
        public bool UnlockedByDefault => unlockedByDefault;
        public int Attack => attack;
        public int Defense => defense;
        public int MovementSpeed => movementSpeed;
        public string Rarity => rarity;
        public string Icon => icon;
        public string Description => description;
        public int Price => price;
    }
#pragma warning restore 0649
}
