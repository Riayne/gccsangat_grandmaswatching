// Sets the Game view to a portrait phone resolution (1080x1920) using Unity's internal GameViewSizes API.
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class GameViewSize
{
    public static string Portrait()
    {
        var asm = typeof(Editor).Assembly;
        var sizesType = asm.GetType("UnityEditor.GameViewSizes");
        var instance = sizesType.BaseType.GetProperty("instance", BindingFlags.Public | BindingFlags.Static).GetValue(null);
        var group = sizesType.GetProperty("currentGroup").GetValue(instance);
        var groupType = group.GetType();
        int total = (int)groupType.GetMethod("GetTotalCount").Invoke(group, null);
        var texts = (string[])groupType.GetMethod("GetDisplayTexts").Invoke(group, null);
        int index = Array.FindIndex(texts, t => t.Contains("1080x1920"));
        if (index < 0)
        {
            var sizeType = asm.GetType("UnityEditor.GameViewSize");
            var sizeKind = asm.GetType("UnityEditor.GameViewSizeType");
            var ctor = sizeType.GetConstructor(new[] { sizeKind, typeof(int), typeof(int), typeof(string) });
            var size = ctor.Invoke(new object[] { Enum.Parse(sizeKind, "FixedResolution"), 1080, 1920, "Phone Portrait" });
            groupType.GetMethod("AddCustomSize").Invoke(group, new[] { size });
            index = total;
        }
        var gvType = asm.GetType("UnityEditor.GameView");
        var gv = EditorWindow.GetWindow(gvType);
        gvType.GetMethod("SizeSelectionCallback", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
              .Invoke(gv, new object[] { index, null });
        return "selected " + index + " of " + texts.Length;
    }
}
