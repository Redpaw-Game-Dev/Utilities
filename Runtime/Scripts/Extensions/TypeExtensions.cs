using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace LazyRedpaw.Utilities
{
    public static class TypeExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<Type> GetSubclasses(this Type baseType)
        {
            var output = new List<Type>();

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (var i = 0; i < assemblies.Length; i++)
            {
                var types = assemblies[i].GetTypes();
                for (var j = 0; j < types.Length; j++)
                {
                    var type = types[j];
                    if (type.IsClass && !type.IsAbstract && baseType.IsAssignableFrom(type))
                    {
                        output.Add(type);
                    }
                }
            }
            
            return output;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsList(this Type type) => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Type GetArrayElementType(this Type type)
        {
            if (type.IsArray) return type.GetElementType();
            if (type.IsList()) return type.GetGenericArguments()[0];
            return null;
        }
    }
}