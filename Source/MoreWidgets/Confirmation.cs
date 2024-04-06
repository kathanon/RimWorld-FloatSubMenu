using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace MoreWidgets {
    internal static class Confirmation {
        public static void Ok(string message,
                              Texture2D icon = null,
                              Action ok = null)
            => Custom(message, icon, ("OK", ok));

        public static void OkCancel(string message,
                                    Texture2D icon = null,
                                    Action ok = null,
                                    Action cancel = null)
            => Custom(message, icon, ("OK", ok), ("Cancel", cancel));

        public static void YesNo(string message,
                                 Texture2D icon = null,
                                 Action yes = null,
                                 Action no = null)
            => Custom(message, icon, ("Yes", yes), ("No", no));

        public static void Custom(string message,
                                  Texture2D icon,
                                  params (string label, Action action)[] buttons) 
            => Find.WindowStack.Add(new Dialog(message, icon, buttons));

        private class Dialog : Window {
            private readonly string message;
            private readonly Texture2D icon;
            private readonly List<Button> buttons;

            public Dialog(string message, Texture2D icon, (string label, Action action)[] buttons) {
                this.message = message;
                this.icon = icon;
                this.buttons = buttons.Select(x => new Button(x.label, x.action)).ToList();
            }

            public override void DoWindowContents(Rect inRect) {
                throw new NotImplementedException();
            }

            public override void OnAcceptKeyPressed() {
                base.OnAcceptKeyPressed();
                buttons.FirstOrDefault()?.action();
            }

            public override void OnCancelKeyPressed() {
                base.OnCancelKeyPressed();
                buttons.LastOrDefault()?.action();
            }

            private class Button {
                public readonly string label;
                public readonly Action action;

                public Button(string label, Action action) {
                    this.label = label;
                    this.action = action;
                }
            }
        }
    }
}
