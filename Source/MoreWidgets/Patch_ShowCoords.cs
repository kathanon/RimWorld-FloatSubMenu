using HarmonyLib;
using RimWorld;
using System;
using UnityEngine;
using Verse;
#if VERSION_1_5
using LudeonTK;
#endif

namespace MoreWidgets {
    [HarmonyPatch]
    public static class Patch_ShowCoords {
        public static bool showMouse = false;
        public static bool showUI = false;

        private static string coordString;
        private static float height;
        private static int frame = -1;


        private static string CoordString {
            get {
                if (Time.frameCount > frame) {
                    frame = Time.frameCount;
                    coordString = UI.MouseCell().ToString();
                }
                return coordString;
            }
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameConditionManager), nameof(GameConditionManager.TotalHeightAt))]
        public static void TotalHeightAt_Post(float width, ref float __result, GameConditionManager __instance) {
            if (showUI && __instance.ownerMap != null) {
                height = Text.CalcHeight(CoordString, width - 6f);
                __result += height + 4f;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameConditionManager), nameof(GameConditionManager.DoConditionsUI))]
        public static void DoConditionsUI_Pre(ref Rect rect, GameConditionManager __instance) {
            if (showUI && __instance.ownerMap != null) {
                Rect mine = rect.TopPartPixels(height);
                mine.width -= 6f;
                var anchor = Text.Anchor;
                Text.Anchor = TextAnchor.UpperRight;
                Widgets.Label(mine, CoordString);
                Text.Anchor = anchor;
                rect.yMin = mine.yMax + 4f;
            }
        }


        private static readonly Vector2 adjust = new Vector2(10f, 20f);

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameComponentUtility), nameof(GameComponentUtility.GameComponentOnGUI))]
        public static void DrawUI_Post() {
            if (showMouse && Find.CurrentMap != null) {
                var pos = new Rect(UI.MousePositionOnUIInverted + adjust, Text.CalcSize(CoordString));
                Widgets.DrawTextHighlight(pos, color: Color.black);
                Widgets.Label(pos, CoordString);
            }
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(DebugTabMenu_Settings), nameof(DebugTabMenu_Settings.InitActions))]
        public static void InitActions_Post(DebugActionNode __result) {
            AddSetting(__result, "Map coords in UI",      () => showUI    = !showUI,    nameof(showUI));
            AddSetting(__result, "Map coords at pointer", () => showMouse = !showMouse, nameof(showMouse));
        }

        private static void AddSetting(DebugActionNode node, string label, Action action, string field) {
            node.AddChild(new DebugActionNode(label,
                action: action) {
                category = "View",
                settingsField = AccessTools.DeclaredField(typeof(Patch_ShowCoords), field),
            });
        }
    }
}
