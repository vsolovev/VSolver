using System;
using System.Reflection;

namespace VSolver
{
    public static class IocHelpers
    {
        public static bool HasAttribute<T>(this Type type) where T : Attribute
        {
            return type.GetCustomAttributes(typeof(T), true).Length > 0;
        }

        public static bool HasAttribute<T>(this PropertyInfo property) where T : Attribute
        {
            return property.GetCustomAttributes(typeof(T), true).Length > 0;
        }

        public static T GetAttribute<T>(this Type type) where T : Attribute
        {
            return type.GetCustomAttributes(typeof(T), true) as T;
        }
    }
}