using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace LazyRedpaw.Utilities
{
    [CustomPropertyDrawer(typeof(InlinedList<>))]
    public class InlinedListDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var sProp = property.FindPropertyRelative("_s");
            return new PropertyField(sProp) { label = property.displayName };
        }
    }
}
