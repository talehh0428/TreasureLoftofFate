using System.Collections.Generic;
using System.Text;
using TMPro;
using TMPro.EditorUtilities;
using UnityEditor;
using UnityEngine;

namespace TreasureLoft.Editor
{
    /// <summary>
    /// 扫描场景字符 → 自动填入 Font Asset Creator → 一键生成 SIMFANG SDF
    /// 菜单: 仙阁 → 重新生成 SIMFANG SDF（场景字符）
    /// </summary>
    public static class FontFixer
    {
        private const string SimfangGuid = "ded9b449f00c5054b8f97f59796a394f";

        private static TMP_FontAsset LoadFontAsset(string guid)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) return null;
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        }

        /// <summary>
        /// 扫描场景中使用的字符 → 自动填入 Font Asset Creator → 一键打开生成窗口
        /// 使用 Custom Character List 模式，一次性预生成所有字符，避免运行时动态添加。
        /// </summary>
        [MenuItem("仙阁/重新生成 SIMFANG SDF")]
        public static void RegenerateSimfangFromScene()
        {
            var simfangFont = LoadFontAsset(SimfangGuid);
            if (simfangFont == null)
            {
                Debug.LogError("[FontFixer] 找不到 SIMFANG SDF，请确认 Assets/Fonts/SIMFANG SDF.asset 存在");
                return;
            }

            // 扫描场景中实际使用的字符
            string charList = BuildSceneCharacterList();
            if (string.IsNullOrEmpty(charList))
            {
                Debug.LogError("[FontFixer] 场景中没有找到任何 TMP 文本，请打开场景后重试");
                return;
            }

            int charCount = charList.Length;

            // 根据字符数量选择合适的 Atlas 尺寸
            int atlasSize;
            if (charCount <= 200) atlasSize = 512;
            else if (charCount <= 800) atlasSize = 1024;
            else atlasSize = 2048;

            // 打开 TMP Font Asset Creator 窗口
            TMPro_FontAssetCreatorWindow.ShowFontAtlasCreatorWindow(simfangFont);
            var window = EditorWindow.GetWindow<TMPro_FontAssetCreatorWindow>();
            if (window == null)
            {
                Debug.LogError("[FontFixer] 无法获取 TMP Font Asset Creator 窗口");
                return;
            }

            // 反射设置参数
            // m_CharacterSetSelectionMode: 6 = Custom Characters, 7 = Characters from File
            SetWindowField(window, "m_CharacterSetSelectionMode", 6);
            // m_CustomCharacters: 自定义字符列表字符串
            SetWindowField(window, "m_CustomCharacters", charList);
            SetWindowField(window, "m_AtlasWidth", atlasSize);
            SetWindowField(window, "m_AtlasHeight", atlasSize);

            Debug.Log($"[FontFixer] 已扫描场景字符并填入 Font Asset Creator\n" +
                      $"字符数: {charCount} | Atlas: {atlasSize}x{atlasSize}\n" +
                      $"请点击窗口中的 [Generate Font Atlas] 按钮完成生成。");
        }

        private static void SetWindowField(object target, string fieldName, object value)
        {
            var windowType = typeof(TMPro_FontAssetCreatorWindow);
            var field = windowType.GetField(fieldName,
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Public);

            if (field != null)
            {
                field.SetValue(target, value);
            }
            else
            {
                Debug.LogWarning($"[FontFixer] 无法找到字段: {fieldName}");
            }
        }

        /// <summary>
        /// 扫描场景中所有 TMP 组件使用的字符，返回去重排序后的字符列表字符串
        /// </summary>
        private static string BuildSceneCharacterList()
        {
            var usedChars = new HashSet<char>();
            var allTMP = Object.FindObjectsOfType<TextMeshProUGUI>(true);

            foreach (var tmp in allTMP)
            {
                if (string.IsNullOrEmpty(tmp.text)) continue;
                foreach (char c in tmp.text)
                {
                    usedChars.Add(c);
                }
            }

            if (usedChars.Count == 0) return null;

            var sorted = new List<char>(usedChars);
            sorted.Sort();

            var sb = new StringBuilder();
            foreach (char c in sorted)
            {
                sb.Append(c);
            }

            Debug.Log($"[FontFixer] 场景字符扫描: 共 {usedChars.Count} 个字符");
            return sb.ToString();
        }
    }
}
