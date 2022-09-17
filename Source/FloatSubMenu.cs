using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;

namespace CraftWithColor
{
    public class FloatSubMenu : FloatMenuOption
    {
        private readonly List<FloatMenuOption> subOptions;
        private readonly string subTitle;
        private readonly float extraPartWidthOuter;
        private readonly Func<Rect, bool> extraPartOnGUIOuter;

        private FloatSubMenuInner subMenu = null;
        private Action parentCloseCallback = null;
        private bool parentSetUp = false;
        private bool open = false;
        private bool subMenuOptionChosen = false;
        private Rect extraGUIRect = new Rect(-1f, -1f, 0f, 0f);

        private static readonly Vector2 MenuOffset = new Vector2(-1f, 0f);
        //private static readonly Texture2D ArrowIcon = ContentFinder<Texture2D>.Get("Arrow");
        private const float ArrowExtraWidth = 16f;
        private const float ArrowOffset = 4f;
        private const float ArrowAlpha = 0.6f;

        public FloatSubMenu(string label, List<FloatMenuOption> subOptions, string subTitle = null, MenuOptionPriority priority = MenuOptionPriority.Default, Thing revalidateClickTarget = null, float extraPartWidth = 0, Func<Rect, bool> extraPartOnGUI = null, WorldObject revalidateWorldClickTarget = null, bool playSelectionSound = true, int orderInPriority = 0) 
            : base(label, NoAction, priority, null, revalidateClickTarget, extraPartWidth + ArrowExtraWidth, null, revalidateWorldClickTarget, playSelectionSound, orderInPriority)
        {
            this.subOptions = subOptions;
            this.subTitle = subTitle;
            extraPartOnGUIOuter = extraPartOnGUI;
            extraPartWidthOuter = extraPartWidth;
            this.extraPartOnGUI = DrawExtra;
        }

        public FloatSubMenu(string label, List<FloatMenuOption> subOptions, ThingDef shownItemForIcon, string subTitle = null, MenuOptionPriority priority = MenuOptionPriority.Default, Thing revalidateClickTarget = null, float extraPartWidth = 0, Func<Rect, bool> extraPartOnGUI = null, WorldObject revalidateWorldClickTarget = null, bool playSelectionSound = true, int orderInPriority = 0) 
            : base(label, NoAction, shownItemForIcon, priority, null, revalidateClickTarget, extraPartWidth + ArrowExtraWidth, null, revalidateWorldClickTarget, playSelectionSound, orderInPriority)
        {
            this.subOptions = subOptions;
            this.subTitle = subTitle;
            extraPartOnGUIOuter = extraPartOnGUI;
            extraPartWidthOuter = extraPartWidth;
            this.extraPartOnGUI = DrawExtra;
        }

        public FloatSubMenu(string label, List<FloatMenuOption> subOptions, Texture2D itemIcon, Color iconColor, string subTitle = null, MenuOptionPriority priority = MenuOptionPriority.Default, Thing revalidateClickTarget = null, float extraPartWidth = 0, Func<Rect, bool> extraPartOnGUI = null, WorldObject revalidateWorldClickTarget = null, bool playSelectionSound = true, int orderInPriority = 0) 
            : base(label, NoAction, itemIcon, iconColor, priority, null, revalidateClickTarget, extraPartWidth + ArrowExtraWidth, null, revalidateWorldClickTarget, playSelectionSound, orderInPriority)
        {
            this.subOptions = subOptions;
            this.subTitle = subTitle;
            extraPartOnGUIOuter = extraPartOnGUI;
            extraPartWidthOuter = extraPartWidth;
            this.extraPartOnGUI = DrawExtra;
        }

        private static void NoAction() { }

        public bool DrawExtra(Rect rect)
        {
            extraGUIRect = rect.RightPartPixels(ArrowExtraWidth);
            extraPartOnGUIOuter?.Invoke(rect.LeftPartPixels(extraPartWidthOuter));
            return false;
        }

        private static void DrawArrow(Rect rect)
        {
            rect.width -= ArrowOffset;

            GameFont font = Text.Font;
            TextAnchor anchor = Text.Anchor;
            Color color = GUI.color;

            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleRight;
            GUI.color = new Color(color.r, color.g, color.b, color.a * ArrowAlpha);
            Widgets.Label(rect, ">");

            Text.Font = font;
            Text.Anchor = anchor;
            GUI.color = color;
        }

        public override bool DoGUI(Rect rect, bool colonistOrdering, FloatMenu floatMenu)
        {
            SetupParent(floatMenu);

            MouseArea mouseArea = FindMouseArea(rect, floatMenu);
            bool inExtraSpace = Mouse.IsOver(extraGUIRect);
            if (mouseArea == (open ? MouseArea.Menu : MouseArea.Option))
            {
                MouseAction(rect, !open);
            }

            // When the sub menu is open, let super implementation know only about
            // mouse movement inside parent menu. Also do not let it know if the
            // mouse is in our extraPartOnGUI space, since it does not highlight
            // option then.
            Vector2 mouse = Event.current.mousePosition;
            if ((open && mouseArea == MouseArea.Outside) || inExtraSpace)
            {
                Event.current.mousePosition = new Vector2(rect.x + 2f, rect.y + 2f);
            }

            base.DoGUI(rect, colonistOrdering, floatMenu);
            DrawArrow(rect);

            // Reset mouse position
            Event.current.mousePosition = mouse;
            
            if (subMenuOptionChosen) { floatMenu.PreOptionChosen(this); }
            return subMenuOptionChosen;
        }

        private void SetupParent(FloatMenu parent)
        {
            if (!(parentSetUp || parent is FloatSubMenuInner))
            {
                parentSetUp = true;
                parentCloseCallback = parent.onCloseCallback;
                parent.onCloseCallback = OnParentClose;
            }
        }

        private void OnParentClose()
        {
            CloseSubMenu();
            // Call any previously set action
            parentCloseCallback?.Invoke();
        }

        private enum MouseArea { Option, Menu, Outside }

        private MouseArea FindMouseArea(Rect option, FloatMenu menu)
        {
            option.height--;
            if (Mouse.IsOver(option)) { return MouseArea.Option; }

            // As the current window being drawn, origo will be relative to menu
            return Mouse.IsOver(menu.windowRect.AtZero()) ? MouseArea.Menu : MouseArea.Outside;
        }

        private void MouseAction(Rect rect, bool enter)
        {
            if (enter)
            {
                Vector2 localPos = new Vector2(rect.xMax, rect.yMin) + MenuOffset;
                OpenSubMenu(localPos);
            }
            else
            {
                CloseSubMenu();
            }
        }

        private void OpenSubMenu(Vector2 localPos)
        {
            if (!open)
            {
                open = true;
                Vector2 mouse = Event.current.mousePosition;
                Vector2 offset = localPos - mouse;
                SoundDef sound = SoundDefOf.FloatMenu_Open;
                SoundDefOf.FloatMenu_Open = null;
                subMenu = new FloatSubMenuInner(this, subOptions, subTitle, offset);
                SoundDefOf.FloatMenu_Open = sound;
                Find.WindowStack.Add(subMenu);
            }
        }

        private void CloseSubMenu()
        {
            if (open)
            {
                open = false;
                Find.WindowStack.TryRemove(subMenu, doCloseSound: false);
                subMenu = null;
            }
        }

        private class FloatSubMenuInner : FloatMenu
        {
            public Vector2 MouseOffset;
            public FloatSubMenu parent;

            public FloatSubMenuInner(FloatSubMenu parent, List<FloatMenuOption> options, string title, Vector2 MouseOffset) : base(options, title, false) 
            {
                this.MouseOffset = MouseOffset;
                this.parent = parent;

                // TODO: support vanishIfMouseDistant = true
                vanishIfMouseDistant = false;
                onlyOneOfTypeAllowed = false;
            }

            protected override void SetInitialSizeAndPosition()
            {
                Vector2 pos = UI.MousePositionOnUIInverted + MouseOffset;
                float x = Mathf.Min(pos.x, UI.screenWidth - InitialSize.x);
                float y = Mathf.Min(pos.y, UI.screenHeight - InitialSize.y);
                windowRect = new Rect(x, y, InitialSize.x, InitialSize.y);
            }

            public override void PreOptionChosen(FloatMenuOption opt)
            {
                parent.subMenuOptionChosen = true;
                base.PreOptionChosen(opt);
            }

            public override void PreClose()
            {
                foreach (FloatSubMenu sub in options.Where(o => o is FloatSubMenu))
                {
                    sub.CloseSubMenu();
                }
            }
        }
    }
}
