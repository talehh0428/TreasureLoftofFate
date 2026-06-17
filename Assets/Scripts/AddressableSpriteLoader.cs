using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

public static class AddressableSpriteLoader
{
    private static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>();

    public static IEnumerator SetImageSpriteRoutine(Image target, string address, Sprite fallback = null)
    {
        yield return SetImageSpriteRoutine(target, address, fallback, null);
    }

    public static IEnumerator SetImageSpriteRoutine(Image target, string address, Sprite fallback, Func<bool> shouldApply)
    {
        if (target == null)
        {
            yield break;
        }

        if (string.IsNullOrWhiteSpace(address))
        {
            ApplySprite(target, fallback, shouldApply);
            yield break;
        }

        string safeAddress = address.Trim();
        if (SpriteCache.TryGetValue(safeAddress, out Sprite cachedSprite))
        {
            ApplySprite(target, cachedSprite, shouldApply);
            yield break;
        }

        ApplySprite(target, fallback, shouldApply);

        AsyncOperationHandle<Sprite> handle = Addressables.LoadAssetAsync<Sprite>(safeAddress);
        yield return handle;

        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogWarning($"[AddressableSpriteLoader] Failed to load sprite: {safeAddress}");
            ApplySprite(target, fallback, shouldApply);
            yield break;
        }

        Sprite sprite = handle.Result;
        SpriteCache[safeAddress] = sprite;
        ApplySprite(target, sprite, shouldApply);
    }

    private static void ApplySprite(Image target, Sprite sprite, Func<bool> shouldApply)
    {
        if (target == null || shouldApply != null && !shouldApply())
        {
            return;
        }

        target.sprite = sprite;
        target.enabled = sprite != null;
    }
}
