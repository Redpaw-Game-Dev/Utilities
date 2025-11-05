using System.Runtime.CompilerServices;
using UnityEngine.UIElements;

namespace LazyRedpaw.Utilities.Editor
{
    public static class VisualElementExtensions
    {
        private static readonly StyleEnum<DisplayStyle> StyleDisplayFlex = new (DisplayStyle.Flex);
        private static readonly StyleEnum<DisplayStyle> StyleDisplayNone = new (DisplayStyle.None);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Show(this VisualElement element) => element.style.display = StyleDisplayFlex;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Hide(this VisualElement element) => element.style.display = StyleDisplayNone;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetPadding(this VisualElement element, Length top = default, Length right = default, Length bottom = default, Length left = default)
        {
            element.style.paddingTop = new StyleLength(top);
            element.style.paddingRight = new StyleLength(right);
            element.style.paddingBottom = new StyleLength(bottom);
            element.style.paddingLeft = new StyleLength(left);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetPadding(this VisualElement element, Length length)
        {
            element.style.paddingTop = new StyleLength(length);
            element.style.paddingRight = new StyleLength(length);
            element.style.paddingBottom = new StyleLength(length);
            element.style.paddingLeft = new StyleLength(length);
        }
                
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetBorderWidth(this VisualElement element, float top = 0f, float right = 0f, float bottom = 0f, float left = 0f)
        {
            element.style.borderTopWidth = new StyleFloat(top);
            element.style.borderRightWidth = new StyleFloat(right);
            element.style.borderBottomWidth = new StyleFloat(bottom);
            element.style.borderLeftWidth = new StyleFloat(left);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetBorderWidth(this VisualElement element, float width)
        {
            element.style.borderTopWidth = new StyleFloat(width);
            element.style.borderRightWidth = new StyleFloat(width);
            element.style.borderBottomWidth = new StyleFloat(width);
            element.style.borderLeftWidth = new StyleFloat(width);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetBorderRadius(this VisualElement element, Length topRight = default, Length bottomRight = default, Length bottomLeft = default, Length topLeft = default)
        {
            element.style.borderTopRightRadius = new StyleLength(topRight);
            element.style.borderBottomRightRadius = new StyleLength(bottomRight);
            element.style.borderBottomLeftRadius = new StyleLength(bottomLeft);
            element.style.borderTopLeftRadius = new StyleLength(topLeft);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetBorderRadius(this VisualElement element, Length length)
        {
            element.style.borderTopRightRadius = new StyleLength(length);
            element.style.borderBottomRightRadius = new StyleLength(length);
            element.style.borderBottomLeftRadius = new StyleLength(length);
            element.style.borderTopLeftRadius = new StyleLength(length);
        }
    }
}