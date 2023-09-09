using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Steam;

namespace MoreWidgets {
    public static class TooltipHandler2 {
        private static readonly Dictionary<int, ActiveExpandedTip> activeTips = 
            new Dictionary<int, ActiveExpandedTip>();

        private static int frame = 0;

        private static readonly List<int> dyingTips = new List<int>(32);

        private const float SpaceBetweenTooltips = 2f;

        private static readonly List<ActiveExpandedTip> drawingTips = 
            new List<ActiveExpandedTip>();

        public static void ClearTooltipsFrom(Rect rect) {
            if (Event.current.type != EventType.Repaint || !Mouse.IsOver(rect)) {
                return;
            }

            dyingTips.Clear();
            foreach (var tip in activeTips) {
                if (tip.Value.lastTriggerFrame == frame) {
                    dyingTips.Add(tip.Key);
                }
            }

            for (int i = 0; i < dyingTips.Count; i++) {
                activeTips.Remove(dyingTips[i]);
            }
        }

        public static void TipRegion(Rect rect, Action<Rect> draw, Func<Vector2> size, int uniqueId) {
            TipRegion(rect, new ExpandedTip(draw, size, uniqueId));
        }

        public static void TipRegion(Rect rect, ExpandedTip tip) {
            if (Event.current.type == EventType.Repaint
                && (Mouse.IsOver(rect) || DebugViewSettings.drawTooltipEdges)
                && (tip.size != null || tip.draw != null)
                && !SteamDeck.KeyboardShowing) {

                if (DebugViewSettings.drawTooltipEdges) {
                    Widgets.DrawBox(rect);
                }

                if (!activeTips.ContainsKey(tip.uniqueId)) {
                    var value = new ActiveExpandedTip(tip);
                    activeTips.Add(tip.uniqueId, value);
                    activeTips[tip.uniqueId].firstTriggerTime = Time.realtimeSinceStartup;
                }

                activeTips[tip.uniqueId].lastTriggerFrame = frame;
            }
        }

        public static void DoTooltipGUI() {
            if (!CellInspectorDrawer.active) {
                DrawActiveTips();
                if (Event.current.type == EventType.Repaint) {
                    CleanActiveTooltips();
                    frame++;
                }
            }
        }

        private static void DrawActiveTips() {
            if (activeTips.Count == 0) {
                return;
            }

            drawingTips.Clear();
            foreach (var value in activeTips.Values) {
                if (Time.realtimeSinceStartup > value.firstTriggerTime + value.tip.delay) {
                    drawingTips.Add(value);
                }
            }

            if (drawingTips.Any()) {
                drawingTips.SortStable(CompareTooltipsByPriority);
                Vector2 pos = CalculateInitialTipPosition(drawingTips);
                for (int i = 0; i < drawingTips.Count; i++) {
                    pos.y += drawingTips[i].DrawTooltip(pos);
                    pos.y += SpaceBetweenTooltips;
                }

                drawingTips.Clear();
            }
        }

        private static void CleanActiveTooltips() {
            dyingTips.Clear();
            foreach (KeyValuePair<int, ActiveExpandedTip> activeTip in activeTips) {
                if (activeTip.Value.lastTriggerFrame != frame) {
                    dyingTips.Add(activeTip.Key);
                }
            }

            for (int i = 0; i < dyingTips.Count; i++) {
                activeTips.Remove(dyingTips[i]);
            }
        }

        private static Vector2 CalculateInitialTipPosition(List<ActiveExpandedTip> drawingTips) {
            float height = 0f;
            float width = 0f;
            for (int i = 0; i < drawingTips.Count; i++) {
                Rect tipRect = drawingTips[i].TipRect;
                height += tipRect.height;
                width = Mathf.Max(width, tipRect.width);
                if (i != drawingTips.Count - 1) {
                    height += SpaceBetweenTooltips;
                }
            }

            return GenUI.GetMouseAttachedWindowPos(width, height);
        }

        private static int CompareTooltipsByPriority(ActiveExpandedTip A, ActiveExpandedTip B) {
            int num = 0 - A.tip.priority;
            int value = 0 - B.tip.priority;
            return num.CompareTo(value);
        }
    }

    public class ExpandedTip {
        public readonly Action<Rect> draw;
        public readonly Func<Vector2> size;
        public readonly int uniqueId;
        public TooltipPriority priority = TooltipPriority.Default;
        public float delay = 0.45f;

        public ExpandedTip(Action<Rect> draw, Func<Vector2> size, int uniqueId) {
            this.draw = draw;
            this.size = size;
            this.uniqueId = uniqueId;
        }
    }

    internal class ActiveExpandedTip {
        private const float TipMargin = 4f;

        public ExpandedTip tip;
        public double firstTriggerTime;
        public int lastTriggerFrame;

        public Rect TipRect
            => new Rect(default, tip.size()).ContractedBy(-TipMargin).RoundedCeil();

        public ActiveExpandedTip(ExpandedTip tip) {
            this.tip = tip;
        }

        public ActiveExpandedTip(ActiveExpandedTip cloneSource) {
            tip = cloneSource.tip;
            firstTriggerTime = cloneSource.firstTriggerTime;
            lastTriggerFrame = cloneSource.lastTriggerFrame;
        }

        public float DrawTooltip(Vector2 pos) {
            Text.Font = GameFont.Small;
            Rect bgRect = TipRect;
            bgRect.position = pos;
            if (!LongEventHandler.AnyEventWhichDoesntUseStandardWindowNowOrWaiting) {
                Find.WindowStack.ImmediateWindow(153 * tip.uniqueId + 62346,
                                                 bgRect,
                                                 WindowLayer.Super,
                                                 () => DrawInner(bgRect.AtZero()),
                                                 doBackground: false);
            } else {
                Widgets.DrawShadowAround(bgRect);
                Widgets.DrawWindowBackground(bgRect);
                DrawInner(bgRect);
            }

            return bgRect.height;
        }

        private void DrawInner(Rect bgRect) {
            Widgets.DrawAtlas(bgRect, ActiveTip.TooltipBGAtlas);
            tip.draw(bgRect.ContractedBy(TipMargin));
        }
    }

    [HarmonyPatch]
    public static class Patch_DoTooltipGUI {
        [HarmonyTargetMethods]
        public static IEnumerable<MethodInfo> Targets() {
            yield return AccessTools.Method(typeof(LongEventHandler), nameof(LongEventHandler.LongEventsOnGUI));
            yield return AccessTools.Method(typeof(UIRoot), nameof(UIRoot.UIRootOnGUI));
        }

        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> orig) {
            var method = AccessTools.Method(typeof(TooltipHandler), nameof(TooltipHandler.DoTooltipGUI));
            foreach (var instr in orig) {
                yield return instr;
                if (instr.Calls(method)) {
                    yield return CodeInstruction.Call(typeof(TooltipHandler2),
                                                      nameof(TooltipHandler2.DoTooltipGUI));
                }
            }
        }
    }
}