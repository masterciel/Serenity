﻿using System;

namespace Serenity.Reflection
{
    public static class GeneratorUtils
    {
        public static bool IsSimpleType(Type type)
        {
            if (type == typeof(String) ||
                type == typeof(Int32) ||
                type == typeof(Int64) ||
                type == typeof(Int16) ||
                type == typeof(Double) ||
                type == typeof(Decimal) ||
                type == typeof(DateTime) ||
                type == typeof(Boolean) ||
                type == typeof(TimeSpan))
                return true;

            return false;
        }

        public static bool GetFirstDerivedOfGenericType(Type type, Type genericType, out Type derivedType)
        {
            if (type.GetIsGenericType() && type.GetGenericTypeDefinition() == genericType)
            {
                derivedType = type;
                return true;
            }

            if (type.GetBaseType() != null)
                return GetFirstDerivedOfGenericType(type.GetBaseType(), genericType, out derivedType);

            derivedType = null;
            return false;
        }
    }
}