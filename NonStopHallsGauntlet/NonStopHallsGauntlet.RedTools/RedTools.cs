using UnityEngine;
using System.Reflection;
using System.Collections.Generic;

public static class RedTools
{
    static MethodInfo _getEquippedTools;

    static MethodInfo GetEquippedToolsMethod()
    {
        if (_getEquippedTools == null)
        {
            _getEquippedTools = typeof(ToolItemManager).GetMethod(
                "GetCurrentEquippedTools",
                BindingFlags.NonPublic | BindingFlags.Static);
        }
        return _getEquippedTools;
    }

    public static List<ToolItem> GetEquippedRedTools()
    {
        var method = GetEquippedToolsMethod();
        if (method == null)
            return null;

        var tools = method.Invoke(null, null) as List<ToolItem>;
        if (tools == null)
            return null;

        List<ToolItem> result = new List<ToolItem>();
        foreach (var tool in tools)
        {
            if (tool == null)
                continue;
            if (tool != null && RedToolNames.RedTools.Contains(tool.name))
                result.Add(tool);
        }
        return result;
    }

    public static Sprite GetInventorySprite(ToolItem tool)
    {
        if (tool == null)
            return null;

        return tool.GetPopupIcon();
    }

    public static bool ToolNeedsReplenish(ToolItem tool)
    {
        var data = PlayerData.instance.GetToolData(tool.name);
        int storage = ToolItemManager.GetToolStorageAmount(tool);
        return data.AmountLeft < storage;
    }

    public static void ReplenishTool(ToolItem tool)
    {
        if (tool == null)
            return;

        var toolData = PlayerData.instance.GetToolData(tool.name);
        int storageAmount = ToolItemManager.GetToolStorageAmount(tool);

        if (toolData.AmountLeft < storageAmount)
        {
            toolData.AmountLeft++;
            PlayerData.instance.SetToolData(tool.name, toolData);
        }

        ToolItemManager.ReportAllBoundAttackToolsUpdated();
        ToolItemManager.SendEquippedChangedEvent(force: true);
    }
}
