using System;
using System.Reflection;
using UnityEditor;

namespace LazyRedpaw.Utilities.Editor
{
    public static class SerializedPropertyExtensions
    {
        public static Type GetManagedReferenceFieldType(this SerializedProperty property)
        {
            var typename = property.managedReferenceFieldTypename;
            if (string.IsNullOrEmpty(typename)) return null;

            var spaceIndex = typename.IndexOf(' ');
            if (spaceIndex < 0) return null;

            var assemblyName = typename[..spaceIndex];
            var typeName = typename[(spaceIndex + 1)..];

            var type = Type.GetType($"{typeName}, {assemblyName}");
            if (type != null) return type;

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = asm.GetType(typeName);
                if (type != null) return type;
            }

            return null;
        }
        
        public static Type GetFieldType(this SerializedProperty property)
        {
            var parentType = property.serializedObject.targetObject.GetType();
            var path = property.propertyPath.Replace(".Array.data[", "[").Split('.');
            FieldInfo field = null;
            var type = parentType;

            foreach (var element in path)
            {
                if (element.Contains("["))
                {
                    var elementName = element[..element.IndexOf('[')];
                    field = type.GetField(elementName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                    type = field.FieldType.GetArrayElementType() ?? field.FieldType;
                }
                else
                {
                    field = type.GetField(element, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                    if (field == null) break;
                    type = field.FieldType;
                }
            }
            return type;
        }
    }
}