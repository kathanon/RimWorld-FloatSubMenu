using UnityEngine;

namespace MoreWidgets {
    public static class ExtensionMethods {
        public static void StepX(this ref Rect rect, float margin)
            => rect.x += rect.width + margin;

        public static void StepY(this ref Rect rect, float margin)
            => rect.y += rect.height + margin;
    }
}