using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace PixelDefense.EditorTools
{
    /// <summary>Selects portrait phone resolutions in the Game view (Unity has no public API for this).</summary>
    public static class GameViewTools
    {
        [MenuItem("Pixel Defense/Game View/Portrait 1080x1920 (16:9)", priority = 40)]
        public static void Portrait169()
        {
            SetResolution(1080, 1920, "Pixel Defense 1080x1920");
        }

        [MenuItem("Pixel Defense/Game View/Portrait 1080x2340 (19.5:9)", priority = 41)]
        public static void Portrait195()
        {
            SetResolution(1080, 2340, "Pixel Defense 1080x2340");
        }

        [MenuItem("Pixel Defense/Game View/Tablet 1536x2048 (4:3)", priority = 42)]
        public static void Tablet()
        {
            SetResolution(1536, 2048, "Pixel Defense 1536x2048");
        }

        public static string SetResolution(int width, int height, string label)
        {
            Assembly editor = typeof(Editor).Assembly;
            Type sizesType = editor.GetType("UnityEditor.GameViewSizes");
            Type singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            object sizes = singleton.GetProperty("instance").GetValue(null);
            object group = sizesType.GetProperty("currentGroup").GetValue(sizes);
            Type groupType = group.GetType();

            int count = (int)groupType.GetMethod("GetTotalCount").Invoke(group, null);
            int index = -1;
            for (int i = 0; i < count; i++)
            {
                object size = groupType.GetMethod("GetGameViewSize").Invoke(group, new object[] { i });
                Type sizeType = size.GetType();
                if ((int)sizeType.GetProperty("width").GetValue(size) == width &&
                    (int)sizeType.GetProperty("height").GetValue(size) == height)
                {
                    index = i;
                    break;
                }
            }

            if (index < 0)
            {
                Type gameViewSize = editor.GetType("UnityEditor.GameViewSize");
                Type sizeKind = editor.GetType("UnityEditor.GameViewSizeType");
                ConstructorInfo constructor = gameViewSize.GetConstructor(new[] { sizeKind, typeof(int), typeof(int), typeof(string) });
                object custom = constructor.Invoke(new object[] { Enum.Parse(sizeKind, "FixedResolution"), width, height, label });
                groupType.GetMethod("AddCustomSize").Invoke(group, new[] { custom });
                index = (int)groupType.GetMethod("GetTotalCount").Invoke(group, null) - 1;
            }

            Type gameViewType = editor.GetType("UnityEditor.GameView");
            EditorWindow gameView = EditorWindow.GetWindow(gameViewType);
            MethodInfo select = gameViewType.GetMethod("SizeSelectionCallback", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            select.Invoke(gameView, new object[] { index, null });
            return "Game view set to " + width + "x" + height + " (index " + index + ")";
        }
    }
}
