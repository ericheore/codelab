using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace CampfireBugFix2;

/// <summary>
/// 统一焦点补丁：升级 / 删牌 / 变形 / 商店删牌界面在选卡后恢复方向键导航能力。
///
/// 可靠性增强：
/// 1) 目标类型/方法/字段/属性均支持外部文本配置扩展；
/// 2) 反射成员查找带缓存，减少重复反射失败；
/// 3) 多候选字段名与属性名回退，降低版本命名漂移风险。
/// </summary>
[HarmonyPatch]
public static class CampfireNavigationParityPatch
{
    private static readonly string[] DefaultTargetScreenTypeNames =
    {
        "NDeckUpgradeSelectScreen",
        "NDeckRemoveCardSelectScreen",
        "NDeckTransformCardSelectScreen",
        "NShopPurgeCardSelectScreen",
        "NShopRemoveCardSelectScreen",
        "NShopDeckRemoveSelectScreen",
        "NMerchantPurgeCardSelectScreen",
    };

    private static readonly string[] DefaultClickMethodNames =
    {
        "OnCardClicked",
        "OnDeckCardClicked",
        "OnCardSelected",
    };

    private static readonly string[] DefaultFallbackGetterMethodNames =
    {
        "get_DefaultFocusedControl",
        "get_FocusedControlFromTopBar",
    };

    private static readonly string[] DefaultGridFieldNames = { "_grid", "_cardGrid" };
    private static readonly string[] DefaultSelectedCardsFieldNames = { "_selectedCards", "_chosenCards", "_selected" };
    private static readonly string[] DefaultCardHolderPropertyNames = { "CardHolder", "DeckCardHolder" };
    private static readonly string[] DefaultFallbackGridPropertyNames = { "DefaultFocusedControl", "FocusedControlFromTopBar" };

    private static bool _validationLogged;
    private static readonly Dictionary<string, bool> BoolBehaviorCache = new(StringComparer.Ordinal);

    private static readonly Dictionary<string, string[]> ListCache = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, FieldInfo?> FieldCache = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, PropertyInfo?> PropertyCache = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, MethodInfo?> MethodCache = new(StringComparer.Ordinal);

    public static IEnumerable<MethodBase> TargetMethods()
    {
        LogValidationOnce();

        string[] clickMethods = GetClickMethodNames();
        string[] getterMethods = GetFallbackGetterMethodNames();

        foreach (string typeName in GetTargetScreenTypeNames())
        {
            Type? screenType = AccessTools.TypeByName(typeName);
            if (screenType is null)
            {
                continue;
            }

            foreach (string methodName in clickMethods.Concat(getterMethods))
            {
                MethodBase? m = GetMethodCached(screenType, methodName);
                if (m is not null)
                {
                    yield return m;
                }
            }
        }
    }

    public static void Postfix(MethodBase __originalMethod, object __instance, ref object? __result)
    {
        string methodName = __originalMethod.Name;

        if (GetClickMethodNames().Contains(methodName, StringComparer.Ordinal))
        {
            TryReseedNavigationAfterCardClick(__instance);
            return;
        }

        if (__result is not null)
        {
            return;
        }

        TryFallbackFocusedControl(__instance, methodName, ref __result);
    }

    private static void LogValidationOnce()
    {
        if (_validationLogged)
        {
            return;
        }

        _validationLogged = true;

        string[] clickMethods = GetClickMethodNames();
        string[] getterMethods = GetFallbackGetterMethodNames();

        foreach (string typeName in GetTargetScreenTypeNames())
        {
            Type? screenType = AccessTools.TypeByName(typeName);
            if (screenType is null)
            {
                Log($"[WARN] 未找到类型: {typeName}（将跳过该界面补丁）");
                continue;
            }

            foreach (string methodName in clickMethods.Concat(getterMethods))
            {
                MethodBase? method = GetMethodCached(screenType, methodName);
                if (method is null)
                {
                    Log($"[WARN] {typeName} 缺少方法: {methodName}");
                }
                else
                {
                    Log($"[INFO] 将补丁 {typeName}.{methodName}");
                }
            }
        }
    }

    private static void TryReseedNavigationAfterCardClick(object screen)
    {
        try
        {
            object? grid = GetFirstFieldValue(screen, GetGridFieldNames());
            object? selectedCards = GetFirstFieldValue(screen, GetSelectedCardsFieldNames());

            if (grid is null)
            {
                Log($"[WARN] {screen.GetType().Name} 未找到 grid 字段候选，跳过点击后修复");
                return;
            }

            SetFocusBehaviorToInherited(grid);
            TryReseedNeighborLinks(grid, selectedCards);
            TryRefreshActiveScreenContext();
            TryReleaseViewportFocus();
        }
        catch (Exception ex)
        {
            Log($"[WARN] 点击后焦点修复异常: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private static void SetFocusBehaviorToInherited(object grid)
    {
        PropertyInfo? focusBehaviorRecursive = GetPropertyCached(grid.GetType(), "FocusBehaviorRecursive");
        if (focusBehaviorRecursive is null)
        {
            Log("[WARN] grid 缺少 FocusBehaviorRecursive 属性");
            return;
        }

        Type enumType = focusBehaviorRecursive.PropertyType;
        object? inherited = Enum.GetValues(enumType).Cast<object?>().FirstOrDefault(v => v?.ToString() == "Inherited");
        if (inherited is null)
        {
            Log("[WARN] 未找到 FocusBehaviorRecursive.Inherited 枚举值");
            return;
        }

        focusBehaviorRecursive.SetValue(grid, inherited);
    }

    private static void TryReseedNeighborLinks(object grid, object? selectedCards)
    {
        int selectedCount = GetCollectionCount(selectedCards);
        bool reseedOnlyFirstSelection = GetBehaviorFlag("reseed_only_first_selection", defaultValue: true);

        if (reseedOnlyFirstSelection && selectedCount != 1)
        {
            return;
        }

        if (!reseedOnlyFirstSelection && selectedCount < 1)
        {
            return;
        }

        object? cardHolder = GetFirstPropertyValue(grid, GetCardHolderPropertyNames());
        if (cardHolder is null)
        {
            Log("[WARN] 未找到 CardHolder 属性候选，无法重建邻接关系");
            return;
        }

        CallIfExists(cardHolder, "SetFocusNeighborBottom", grid);
        CallIfExists(grid, "SetFocusNeighborTop", cardHolder);
    }

    private static void TryRefreshActiveScreenContext()
    {
        Type? contextType = AccessTools.TypeByName("ActiveScreenContext");
        if (contextType is null)
        {
            Log("[WARN] 未找到 ActiveScreenContext 类型");
            return;
        }

        object? contextInstance = GetPropertyCached(contextType, "Instance")?.GetValue(null);
        MethodInfo? updateMethod = GetMethodCached(contextType, "Update");

        if (updateMethod is null)
        {
            Log("[WARN] 未找到 ActiveScreenContext.Update 方法");
            return;
        }

        updateMethod.Invoke(contextInstance, null);
    }

    private static void TryReleaseViewportFocus()
    {
        Type? viewportType = AccessTools.TypeByName("Viewport");
        if (viewportType is null)
        {
            Log("[WARN] 未找到 Viewport 类型");
            return;
        }

        MethodInfo? releaseFocus = viewportType.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(m => m.Name == "GuiReleaseFocus" && m.GetParameters().Length == 0);

        if (releaseFocus is null)
        {
            Log("[WARN] 未找到 Viewport.GuiReleaseFocus() 静态方法");
            return;
        }

        releaseFocus.Invoke(null, null);
    }

    private static void TryFallbackFocusedControl(object screen, string methodName, ref object? result)
    {
        try
        {
            if (!GetFallbackGetterMethodNames().Contains(methodName, StringComparer.Ordinal))
            {
                return;
            }

            object? grid = GetFirstFieldValue(screen, GetGridFieldNames());
            if (grid is null)
            {
                return;
            }

            foreach (string propertyName in GetFallbackGridPropertyNames())
            {
                object? candidate = GetPropertyCached(grid.GetType(), propertyName)?.GetValue(grid);
                if (candidate is not null)
                {
                    result = candidate;
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            Log($"[WARN] getter 兜底异常: {methodName}: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private static object? GetFirstFieldValue(object instance, IEnumerable<string> candidateFields)
    {
        Type t = instance.GetType();
        foreach (string fieldName in candidateFields)
        {
            FieldInfo? f = GetFieldCached(t, fieldName);
            if (f is not null)
            {
                return f.GetValue(instance);
            }
        }

        return null;
    }

    private static object? GetFirstPropertyValue(object instance, IEnumerable<string> candidateProperties)
    {
        Type t = instance.GetType();
        foreach (string propertyName in candidateProperties)
        {
            PropertyInfo? p = GetPropertyCached(t, propertyName);
            if (p is not null)
            {
                return p.GetValue(instance);
            }
        }

        return null;
    }

    private static int GetCollectionCount(object? collection)
    {
        if (collection is null)
        {
            return 0;
        }

        PropertyInfo? countProp = GetPropertyCached(collection.GetType(), "Count");
        object? countObj = countProp?.GetValue(collection);
        return countObj is int count ? count : 0;
    }

    private static void CallIfExists(object instance, string methodName, params object[] args)
    {
        Type[] argTypes = args.Select(a => a.GetType()).ToArray();
        MethodInfo? method = AccessTools.Method(instance.GetType(), methodName, argTypes);
        if (method is null)
        {
            Log($"[WARN] 未找到方法: {instance.GetType().Name}.{methodName}({string.Join(",", argTypes.Select(t => t.Name))})");
            return;
        }

        method.Invoke(instance, args);
    }

    private static FieldInfo? GetFieldCached(Type type, string fieldName)
    {
        string key = $"{type.FullName}|F|{fieldName}";
        if (!FieldCache.TryGetValue(key, out FieldInfo? value))
        {
            value = AccessTools.Field(type, fieldName);
            FieldCache[key] = value;
        }

        return value;
    }

    private static PropertyInfo? GetPropertyCached(Type type, string propertyName)
    {
        string key = $"{type.FullName}|P|{propertyName}";
        if (!PropertyCache.TryGetValue(key, out PropertyInfo? value))
        {
            value = AccessTools.Property(type, propertyName);
            PropertyCache[key] = value;
        }

        return value;
    }

    private static MethodInfo? GetMethodCached(Type type, string methodName)
    {
        string key = $"{type.FullName}|M|{methodName}";
        if (!MethodCache.TryGetValue(key, out MethodInfo? value))
        {
            value = AccessTools.Method(type, methodName);
            MethodCache[key] = value;
        }

        return value;
    }

    private static string[] GetTargetScreenTypeNames() =>
        LoadListConfig("mod/FocusPatchTargets.txt", DefaultTargetScreenTypeNames);

    private static string[] GetClickMethodNames() =>
        LoadListConfig("mod/FocusPatchClickMethods.txt", DefaultClickMethodNames);

    private static string[] GetFallbackGetterMethodNames() =>
        LoadListConfig("mod/FocusPatchGetterMethods.txt", DefaultFallbackGetterMethodNames);

    private static string[] GetGridFieldNames() =>
        LoadListConfig("mod/FocusPatchGridFields.txt", DefaultGridFieldNames);

    private static string[] GetSelectedCardsFieldNames() =>
        LoadListConfig("mod/FocusPatchSelectedFields.txt", DefaultSelectedCardsFieldNames);

    private static string[] GetCardHolderPropertyNames() =>
        LoadListConfig("mod/FocusPatchCardHolderProps.txt", DefaultCardHolderPropertyNames);

    private static string[] GetFallbackGridPropertyNames() =>
        LoadListConfig("mod/FocusPatchFallbackGridProps.txt", DefaultFallbackGridPropertyNames);


    private static bool GetBehaviorFlag(string key, bool defaultValue)
    {
        if (BoolBehaviorCache.TryGetValue(key, out bool cached))
        {
            return cached;
        }

        string path = "mod/FocusPatchBehavior.txt";
        if (!File.Exists(path))
        {
            BoolBehaviorCache[key] = defaultValue;
            return defaultValue;
        }

        try
        {
            foreach (string raw in File.ReadAllLines(path))
            {
                string line = raw.Trim();
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                {
                    continue;
                }

                string[] pair = line.Split('=', 2, StringSplitOptions.TrimEntries);
                if (pair.Length != 2)
                {
                    continue;
                }

                if (!pair[0].Equals(key, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                bool value = pair[1].Equals("true", StringComparison.OrdinalIgnoreCase);
                BoolBehaviorCache[key] = value;
                Log($"[INFO] 已加载行为配置: {key}={value}");
                return value;
            }
        }
        catch (Exception ex)
        {
            Log($"[WARN] 读取行为配置失败: {ex.GetType().Name}: {ex.Message}");
        }

        BoolBehaviorCache[key] = defaultValue;
        return defaultValue;
    }

    private static string[] LoadListConfig(string path, string[] defaults)
    {
        if (ListCache.TryGetValue(path, out string[]? cached))
        {
            return cached;
        }

        if (!File.Exists(path))
        {
            ListCache[path] = defaults;
            return defaults;
        }

        try
        {
            string[] loaded = File.ReadAllLines(path)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s) && !s.StartsWith("#"))
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            if (loaded.Length == 0)
            {
                ListCache[path] = defaults;
                return defaults;
            }

            Log($"[INFO] 已加载配置: {path} ({loaded.Length} 项)");
            ListCache[path] = loaded;
            return loaded;
        }
        catch (Exception ex)
        {
            Log($"[WARN] 读取配置失败: {path}: {ex.GetType().Name}: {ex.Message}");
            ListCache[path] = defaults;
            return defaults;
        }
    }

    private static void Log(string message)
    {
        Console.WriteLine($"[CampfireNavigationParityPatch] {message}");
    }
}
