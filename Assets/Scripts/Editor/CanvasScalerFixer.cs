using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TreasureLoft.Editor
{
    /// <summary>
    /// 给场景中的 Canvas 补上 CanvasScaler 以实现自适应缩放。
    /// 菜单: 仙阁 → 修复 Canvas 自适应
    /// 用完可删除本文件及 Editor 目录。
    /// </summary>
    public static class CanvasScalerFixer
    {
        [MenuItem("仙阁/修复 Canvas 自适应")]
        public static void FixCanvasScaler()
        {
            // 找到场景中所有 Canvas
            var allCanvases = Object.FindObjectsOfType<Canvas>(true);
            int fixedCount = 0;

            foreach (var canvas in allCanvases)
            {
                // 已有 CanvasScaler 的跳过
                var scaler = canvas.GetComponent<CanvasScaler>();
                if (scaler != null) continue;

                scaler = canvas.gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f; // 宽高等比例兼顾

                EditorUtility.SetDirty(canvas.gameObject);
                fixedCount++;
            }

            if (fixedCount > 0)
            {
                EditorSceneManager.MarkSceneDirty(
                    EditorSceneManager.GetActiveScene());
                Debug.Log(
                    $"[CanvasScalerFixer] 已为 {fixedCount} 个 Canvas 添加自适应缩放 " +
                    $"(基准 1920×1080, Match 0.5)");
            }
            else
            {
                Debug.Log("[CanvasScalerFixer] 所有 Canvas 已具备自适应，无需修复");
            }
        }
    }
}
