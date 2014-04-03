﻿using Newtonsoft.Json;
using Serenity.Data;
using Serenity.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

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
                type == typeof(Boolean))
                return true;

            return false;
        }

    }
}