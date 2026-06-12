using System;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class EconomyBuffSystem : MonoBehaviour, IShopDiscountModifier, IShopRarityWeightModifier
{
    private const int BuffCount = 5;
    private const int MinimumLevel = 1;
    private const int MaximumLevel = 5;

    public static EconomyBuffSystem Instance { get; private set; }

    [Header("Upgrade Flow")]
    [SerializeField] private int[] spiritStoneThresholds =
    {
        100, 300, 1000, 2000, 3500,
        5000, 7500, 10000, 15000, 20000,
        30000, 45000, 60000, 80000, 100000,
        130000, 160000, 200000, 250000, 300000
    };

    [Header("Main Scene UI")]
    [SerializeField] private TMP_Text[] levelTexts = new TMP_Text[BuffCount];
    [SerializeField] private TMP_Text nextUpgradeRequirementText;

    [Header("Upgrade Selection UI")]
    [SerializeField] private GameObject upgradePanelRoot;
    [SerializeField] private Button[] upgradeButtons = new Button[BuffCount];

    [Header("Rarity Weights By Level")]
    [SerializeField] private EconomyBuffRarityWeights[] rarityWeightsByLevel =
    {
        new EconomyBuffRarityWeights(50f, 30f, 15f, 5f, 0f),
        new EconomyBuffRarityWeights(40f, 40f, 15f, 5f, 0f),
        new EconomyBuffRarityWeights(30f, 45f, 20f, 5f, 0f),
        new EconomyBuffRarityWeights(20f, 40f, 30f, 10f, 0f),
        new EconomyBuffRarityWeights(20f, 30f, 30f, 15f, 5f),
    };

    [Header("Discount By Level")]
    [SerializeField] private float[] minimumDiscountByLevel = { 0.05f, 0.05f, 0.05f, 0.05f, 0.05f };
    [SerializeField] private float[] maximumDiscountByLevel = { 0.5f, 0.6f, 0.7f, 0.8f, 0.9f };
    [SerializeField] private float[] discountLambdaByLevel = { 4f, 3.2f, 2.5f, 1.9f, 1.4f };

    [Header("Trade By Level")]
    [SerializeField] private int[] maxTradeItemCountByLevel = { 1, 2, 3, 4, 5 };
    [SerializeField] private float[] sellEfficiencyByLevel = { 1f, 1.15f, 1.3f, 1.5f, 1.75f };

    [Header("End Round Income By Level")]
    [SerializeField] private float[] endRoundIncomeRateByLevel = { 0f, 0.03f, 0.06f, 0.1f, 0.15f };

    private readonly int[] levels = new int[BuffCount];
    private UnityAction[] buttonHandlers;
    private int nextThresholdIndex;
    private int lastProcessedIncomeRound = -1;
    private int lastTriggeredUpgradeRound = -1;
    private bool isWaitingForUpgradeChoice;
    private bool completedUpgradeSelection;

    public int CurrentMaxTradeItemCount => GetValue(maxTradeItemCountByLevel, GetLevel(EconomyBuffType.TradeItemCount), 1);

    public float CurrentSellEfficiency => GetValue(sellEfficiencyByLevel, GetLevel(EconomyBuffType.SellEfficiency), 1f);

    public float CurrentMaximumDiscount => GetClampedDiscountValue(maximumDiscountByLevel, GetLevel(EconomyBuffType.Discount), 0.5f);

    public float CurrentMinimumDiscount => GetClampedDiscountValue(minimumDiscountByLevel, GetLevel(EconomyBuffType.Discount), 0.05f);

    public float CurrentDiscountLambda => Mathf.Max(0.01f, GetValue(discountLambdaByLevel, GetLevel(EconomyBuffType.Discount), 4f));

    public static int GetCurrentMaxTradeItemCount(int fallback)
    {
        return Instance == null ? Mathf.Max(1, fallback) : Instance.CurrentMaxTradeItemCount;
    }

    public static float GetCurrentSellEfficiency(float fallback)
    {
        return Instance == null ? Mathf.Max(0f, fallback) : Instance.CurrentSellEfficiency;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[EconomyBuffSystem] Multiple instances found. The newest instance will be used.", this);
        }

        Instance = this;
        ResetLevels();
        HideUpgradePanel();
        RefreshAllUi();
    }

    public EconomyBuffSaveData CaptureSaveData()
    {
        EconomyBuffSaveData data = new EconomyBuffSaveData
        {
            levels = levels.ToList(),
            nextThresholdIndex = nextThresholdIndex,
            lastProcessedIncomeRound = lastProcessedIncomeRound,
            lastTriggeredUpgradeRound = lastTriggeredUpgradeRound,
        };

        return data;
    }

    public void RestoreSaveData(EconomyBuffSaveData data)
    {
        ResetLevels();

        if (data != null && data.levels != null)
        {
            for (int index = 0; index < Mathf.Min(levels.Length, data.levels.Count); index++)
            {
                levels[index] = Mathf.Clamp(data.levels[index], MinimumLevel, MaximumLevel);
            }

            nextThresholdIndex = Mathf.Max(0, data.nextThresholdIndex);
            lastProcessedIncomeRound = data.lastProcessedIncomeRound;
            lastTriggeredUpgradeRound = data.lastTriggeredUpgradeRound;
        }

        HideUpgradePanel();
        RefreshAllUi();
    }

    public void ResetRuntimeState()
    {
        ResetLevels();
        HideUpgradePanel();
        RefreshAllUi();
    }

    private void OnEnable()
    {
        ShopWallet.MoneyChanged += HandleMoneyChanged;
        BindUpgradeButtons();
        RefreshAllUi();
    }

    private void OnDisable()
    {
        ShopWallet.MoneyChanged -= HandleMoneyChanged;
        UnbindUpgradeButtons();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnValidate()
    {
        ClampConfiguredValues();
        RefreshAllUi();
    }

    public IEnumerator ProcessEndRoundRoutine(int endingRound)
    {
        ApplyEndRoundIncomeOnce(endingRound);
        RefreshNextUpgradeRequirementText();

        if (!CanTriggerUpgradeEvent(endingRound))
        {
            yield break;
        }

        lastTriggeredUpgradeRound = endingRound;
        completedUpgradeSelection = false;
        yield return ShowUpgradePanelRoutine();

        if (completedUpgradeSelection)
        {
            nextThresholdIndex++;
        }

        RefreshAllUi();
    }

    public int GetLevel(EconomyBuffType buffType)
    {
        int index = Mathf.Clamp((int)buffType, 0, BuffCount - 1);
        return Mathf.Clamp(levels[index], MinimumLevel, MaximumLevel);
    }

    public void SelectUpgrade(int buffIndex)
    {
        if (!isWaitingForUpgradeChoice)
        {
            return;
        }

        EconomyBuffType buffType = (EconomyBuffType)Mathf.Clamp(buffIndex, 0, BuffCount - 1);
        if (!TryIncreaseLevel(buffType))
        {
            RefreshUpgradeButtonStates();
            return;
        }

        completedUpgradeSelection = true;
        isWaitingForUpgradeChoice = false;
    }

    public void SelectUpgrade(EconomyBuffType buffType)
    {
        SelectUpgrade((int)buffType);
    }

    public void ModifyDiscountSettings(ShopDiscountSettings settings)
    {
        if (settings == null)
        {
            return;
        }

        float min = CurrentMinimumDiscount;
        float max = Mathf.Max(min, CurrentMaximumDiscount);
        settings.SetMinimumDiscount(min);
        settings.SetMaximumDiscount(max);
        settings.SetGuaranteedDiscount(max);
        settings.SetLambda(CurrentDiscountLambda);
    }

    public float ModifyWeight(ShopItemDefinition itemDefinition, float currentWeight)
    {
        if (itemDefinition == null)
        {
            return 0f;
        }

        EconomyBuffRarityWeights weights = GetCurrentRarityWeights();
        return Mathf.Max(0f, weights.GetWeight(itemDefinition.Rarity));
    }

    private void ApplyEndRoundIncomeOnce(int endingRound)
    {
        if (lastProcessedIncomeRound == endingRound)
        {
            return;
        }

        lastProcessedIncomeRound = endingRound;
        float rate = GetValue(endRoundIncomeRateByLevel, GetLevel(EconomyBuffType.EndRoundIncome), 0f);
        int bonus = Mathf.FloorToInt(ShopWallet.CurrentMoney * Mathf.Max(0f, rate));
        if (bonus > 0)
        {
            ShopWallet.AddMoney(bonus);
        }
    }

    private bool CanTriggerUpgradeEvent(int endingRound)
    {
        if (lastTriggeredUpgradeRound == endingRound ||
            nextThresholdIndex < 0 ||
            spiritStoneThresholds == null ||
            nextThresholdIndex >= spiritStoneThresholds.Length ||
            !HasAnyUpgradeableBuff())
        {
            return false;
        }

        return ShopWallet.CurrentMoney >= Mathf.Max(0, spiritStoneThresholds[nextThresholdIndex]);
    }

    private IEnumerator ShowUpgradePanelRoutine()
    {
        if (upgradePanelRoot == null)
        {
            Debug.LogWarning("[EconomyBuffSystem] Upgrade panel is not assigned. Upgrade event is waiting for UI setup.", this);
            yield break;
        }

        if (!HasAnyUsableUpgradeButton())
        {
            Debug.LogWarning("[EconomyBuffSystem] Upgrade buttons are not assigned. Upgrade event is waiting for UI setup.", this);
            yield break;
        }

        isWaitingForUpgradeChoice = true;
        upgradePanelRoot.SetActive(true);
        RefreshUpgradeButtonStates();

        while (isWaitingForUpgradeChoice)
        {
            yield return null;
        }

        HideUpgradePanel();
    }

    private bool TryIncreaseLevel(EconomyBuffType buffType)
    {
        int index = Mathf.Clamp((int)buffType, 0, BuffCount - 1);
        if (levels[index] >= MaximumLevel)
        {
            return false;
        }

        levels[index]++;
        RefreshAllUi();
        return true;
    }

    private bool HasAnyUpgradeableBuff()
    {
        for (int index = 0; index < levels.Length; index++)
        {
            if (levels[index] < MaximumLevel)
            {
                return true;
            }
        }

        return false;
    }

    private bool HasAnyUsableUpgradeButton()
    {
        if (upgradeButtons == null)
        {
            return false;
        }

        for (int index = 0; index < Mathf.Min(BuffCount, upgradeButtons.Length); index++)
        {
            if (upgradeButtons[index] != null && levels[index] < MaximumLevel)
            {
                return true;
            }
        }

        return false;
    }

    private void ResetLevels()
    {
        for (int index = 0; index < levels.Length; index++)
        {
            levels[index] = MinimumLevel;
        }

        nextThresholdIndex = 0;
        lastProcessedIncomeRound = -1;
        lastTriggeredUpgradeRound = -1;
        isWaitingForUpgradeChoice = false;
        completedUpgradeSelection = false;
    }

    private void BindUpgradeButtons()
    {
        EnsureButtonHandlers();
        if (upgradeButtons == null)
        {
            return;
        }

        for (int index = 0; index < Mathf.Min(BuffCount, upgradeButtons.Length); index++)
        {
            Button button = upgradeButtons[index];
            if (button == null)
            {
                continue;
            }

            button.onClick.RemoveListener(buttonHandlers[index]);
            button.onClick.AddListener(buttonHandlers[index]);
        }
    }

    private void UnbindUpgradeButtons()
    {
        if (upgradeButtons == null || buttonHandlers == null)
        {
            return;
        }

        for (int index = 0; index < Mathf.Min(BuffCount, upgradeButtons.Length); index++)
        {
            if (upgradeButtons[index] != null)
            {
                upgradeButtons[index].onClick.RemoveListener(buttonHandlers[index]);
            }
        }
    }

    private void EnsureButtonHandlers()
    {
        if (buttonHandlers != null && buttonHandlers.Length == BuffCount)
        {
            return;
        }

        buttonHandlers = new UnityAction[BuffCount];
        for (int index = 0; index < BuffCount; index++)
        {
            int capturedIndex = index;
            buttonHandlers[index] = () => SelectUpgrade(capturedIndex);
        }
    }

    private void RefreshAllUi()
    {
        RefreshLevelTexts();
        RefreshNextUpgradeRequirementText();
        RefreshUpgradeButtonStates();
    }

    private void RefreshLevelTexts()
    {
        if (levelTexts == null)
        {
            return;
        }

        for (int index = 0; index < Mathf.Min(BuffCount, levelTexts.Length); index++)
        {
            if (levelTexts[index] != null)
            {
                levelTexts[index].text = GetLevel((EconomyBuffType)index).ToString();
            }
        }
    }

    private void RefreshNextUpgradeRequirementText()
    {
        if (nextUpgradeRequirementText == null)
        {
            return;
        }

        int remaining = 0;
        if (spiritStoneThresholds != null && nextThresholdIndex >= 0 && nextThresholdIndex < spiritStoneThresholds.Length)
        {
            remaining = Mathf.Max(0, spiritStoneThresholds[nextThresholdIndex] - ShopWallet.CurrentMoney);
        }

        nextUpgradeRequirementText.text = remaining.ToString();
    }

    private void RefreshUpgradeButtonStates()
    {
        if (upgradeButtons == null)
        {
            return;
        }

        for (int index = 0; index < Mathf.Min(BuffCount, upgradeButtons.Length); index++)
        {
            if (upgradeButtons[index] != null)
            {
                upgradeButtons[index].interactable = isWaitingForUpgradeChoice && levels[index] < MaximumLevel;
            }
        }
    }

    private void HideUpgradePanel()
    {
        if (upgradePanelRoot != null)
        {
            upgradePanelRoot.SetActive(false);
        }

        isWaitingForUpgradeChoice = false;
        RefreshUpgradeButtonStates();
    }

    private void HandleMoneyChanged(int currentMoney)
    {
        RefreshNextUpgradeRequirementText();
    }

    private EconomyBuffRarityWeights GetCurrentRarityWeights()
    {
        EconomyBuffRarityWeights fallback = new EconomyBuffRarityWeights(50f, 30f, 15f, 5f, 0f);
        return GetValue(rarityWeightsByLevel, GetLevel(EconomyBuffType.RarityWeight), fallback);
    }

    private static T GetValue<T>(T[] values, int level, T fallback)
    {
        if (values == null || values.Length == 0)
        {
            return fallback;
        }

        int index = Mathf.Clamp(level - 1, 0, values.Length - 1);
        T value = values[index];
        return value == null ? fallback : value;
    }

    private static int GetValue(int[] values, int level, int fallback)
    {
        if (values == null || values.Length == 0)
        {
            return fallback;
        }

        int index = Mathf.Clamp(level - 1, 0, values.Length - 1);
        return Mathf.Max(1, values[index]);
    }

    private static float GetValue(float[] values, int level, float fallback)
    {
        if (values == null || values.Length == 0)
        {
            return fallback;
        }

        int index = Mathf.Clamp(level - 1, 0, values.Length - 1);
        return values[index];
    }

    private static float GetClampedDiscountValue(float[] values, int level, float fallback)
    {
        return Mathf.Clamp(GetValue(values, level, fallback), 0f, 0.99f);
    }

    private void ClampConfiguredValues()
    {
        ClampFloatArray(minimumDiscountByLevel, 0f, 0.99f);
        ClampFloatArray(maximumDiscountByLevel, 0f, 0.99f);
        ClampFloatArray(discountLambdaByLevel, 0.01f, float.MaxValue);
        ClampFloatArray(sellEfficiencyByLevel, 0f, float.MaxValue);
        ClampFloatArray(endRoundIncomeRateByLevel, 0f, float.MaxValue);

        if (maxTradeItemCountByLevel != null)
        {
            for (int index = 0; index < maxTradeItemCountByLevel.Length; index++)
            {
                maxTradeItemCountByLevel[index] = Mathf.Max(1, maxTradeItemCountByLevel[index]);
            }
        }

        if (spiritStoneThresholds != null)
        {
            for (int index = 0; index < spiritStoneThresholds.Length; index++)
            {
                spiritStoneThresholds[index] = Mathf.Max(0, spiritStoneThresholds[index]);
            }
        }
    }

    private static void ClampFloatArray(float[] values, float min, float max)
    {
        if (values == null)
        {
            return;
        }

        for (int index = 0; index < values.Length; index++)
        {
            values[index] = Mathf.Clamp(values[index], min, max);
        }
    }
}

[Serializable]
public class EconomyBuffRarityWeights
{
    [SerializeField] [Min(0f)] private float commonWeight;
    [SerializeField] [Min(0f)] private float fineWeight;
    [SerializeField] [Min(0f)] private float superiorWeight;
    [SerializeField] [Min(0f)] private float epicWeight;
    [SerializeField] [Min(0f)] private float immortalWeight;

    public EconomyBuffRarityWeights(float commonWeight, float fineWeight, float superiorWeight, float epicWeight, float immortalWeight)
    {
        this.commonWeight = Mathf.Max(0f, commonWeight);
        this.fineWeight = Mathf.Max(0f, fineWeight);
        this.superiorWeight = Mathf.Max(0f, superiorWeight);
        this.epicWeight = Mathf.Max(0f, epicWeight);
        this.immortalWeight = Mathf.Max(0f, immortalWeight);
    }

    public float GetWeight(ShopItemRarity rarity)
    {
        switch (rarity)
        {
            case ShopItemRarity.Common:
                return commonWeight;
            case ShopItemRarity.Fine:
                return fineWeight;
            case ShopItemRarity.Superior:
                return superiorWeight;
            case ShopItemRarity.Epic:
                return epicWeight;
            case ShopItemRarity.Immortal:
                return immortalWeight;
            default:
                return 0f;
        }
    }
}
