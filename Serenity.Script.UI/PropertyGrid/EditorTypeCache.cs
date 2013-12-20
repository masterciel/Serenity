﻿using jQueryApi;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Serenity
{
    [Imported, Serializable]
    public class EditorTypeInfo
    {
        public Type Type;
        public EditorAttribute Attribute;
    }

    public class EditorTypeCache
    {
        private static JsDictionary<string, bool> visited;
        private static JsDictionary<string, EditorTypeInfo> registeredTypes;

        private static void RegisterTypesInNamespace(string ns)
        {
            Type nsObj = Type.GetType(ns);
            if (nsObj == null)
                return;

            foreach (var k in Object.Keys(nsObj))
            {
                var obj = nsObj.As<JsDictionary>()[k];

                if (obj == null)
                    continue;

                string name = ns + "." + k;

                visited[name] = true;

                var type = Type.GetType(name);
                if (type == null)
                    continue;

                if (Script.TypeOf(obj) == "function")
                {
                    var attr = type.GetCustomAttributes(typeof(EditorAttribute), false);
                    if (attr != null && attr.Length > 0)
                        RegisterType(type, attr[0] as EditorAttribute);
                }
                else
                {
                    RegisterTypesInNamespace(name);
                    continue;
                }
            }
        }

        private static void RegisterType(Type type, EditorAttribute attr)
        {
            string name = type.FullName;
            var idx = name.IndexOf('.');
            if (idx >= 0)
                name = name.Substr(idx + 1);

            registeredTypes[name] = new EditorTypeInfo
            {
                Type = type,
                Attribute = attr
            };
        }

        public static JsDictionary<string, EditorTypeInfo> RegisteredTypes
        {
            get
            {
                if (registeredTypes == null)
                {
                    visited = new JsDictionary<string, bool>();
                    registeredTypes = new JsDictionary<string, EditorTypeInfo>();

                    foreach (var ns in Q.Config.RootNamespaces)
                        RegisterTypesInNamespace(ns);
                }

                return registeredTypes;
            }
        }
    }
}