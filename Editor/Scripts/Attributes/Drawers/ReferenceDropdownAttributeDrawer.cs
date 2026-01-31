using System;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace LazyRedpaw.Utilities
{
    [CustomPropertyDrawer(typeof(ReferenceDropdownAttribute))]
    public class ReferenceDropdownAttributeDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var typeSplitName = property.managedReferenceFieldTypename.Split(' ');
            var assemblyQualifiedName = $"{typeSplitName[1]}, {typeSplitName[0]}";
            var requiredType = Type.GetType(assemblyQualifiedName);
            var types = requiredType.GetSubclasses();
            var typeNames = types.Select(i => i.Name).ToList();
            
            string currentTypeName;
            if(property.managedReferenceValue != null) currentTypeName = property.managedReferenceValue.GetType().Name;
            else
            {
                currentTypeName = typeNames[0];
                CreateNewTypeInstance(property, types[0]);
            }
            
            var root = new VisualElement();
            
            var dropdown = new DropdownField(typeNames, currentTypeName);
            dropdown.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue != evt.previousValue)
                {
                    var newType = types.First(type => type.Name == evt.newValue);
                    CreateNewTypeInstance(property, newType);
                }
            });
            root.Add(dropdown);
            
            if (property.managedReferenceValue != null)
            {
                var propField = new PropertyField(property);
                root.Add(propField);
            }
            
            return root;
        }

        private void CreateNewTypeInstance(SerializedProperty property, Type newType)
        {
            property.managedReferenceValue = Activator.CreateInstance(newType);
            property.serializedObject.ApplyModifiedProperties();
            property.serializedObject.Update();
            EditorUtility.SetDirty(property.serializedObject.targetObject);
            AssetDatabase.Refresh();
        }
    }
}