﻿
namespace Serenity.Localization
{
    using Extensibility;
    using Serenity.Abstractions;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Reflection;

    /// <summary>
    /// Contains helper methods for registration of permission keys in nested static classes.
    /// These classes contains string contants containing permission keys. 
    /// Display name for permission keys can be set by [DisplayName] attribute.
    /// Display name for groups can be set by [Description] attribute on classes themselves.
    /// For group display name to work, all constants in a class must start with same group prefix.
    /// </summary>
    public static class NestedPermissionKeyRegistration
    {
        /// <summary>
        /// Gets permission keys and adds texts if any from static nested permission key classes marked with NestedPermissionKeys attribute.
        /// </summary>
        public static HashSet<string> AddNestedPermissions(this ILocalTextRegistry registry, IEnumerable<Assembly> assemblies = null)
        {
            assemblies = assemblies ?? ExtensibilityHelper.SelfAssemblies;

            if (assemblies == null)
                throw new ArgumentNullException("assemblies");

            var permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var assembly in assemblies)
                foreach (var type in assembly.GetTypes())
                {
                    var attr = type.GetCustomAttribute<NestedPermissionKeysAttribute>();
                    if (attr != null)
                        AddKeysFrom(permissions, registry, type, attr.LanguageID ?? LocalText.InvariantLanguageID);
                }

            return permissions;
        }

        private static void AddKeysFrom(HashSet<string> permissions, ILocalTextRegistry registry, Type type, string languageID)
        {
            var thisKeys = new List<string>();

            foreach (var member in type.GetFields(BindingFlags.Static | BindingFlags.DeclaredOnly |
                BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (member.FieldType != typeof(String))
                    continue;

                var key = member.GetValue(null) as string;
                if (key == null)
                    continue;

                DescriptionAttribute descr;

                if (key.EndsWith(":"))
                {
                    if (registry != null)
                    {
                        descr = member.GetCustomAttribute<DescriptionAttribute>();
                        registry.Add(languageID, "Permission." + key, descr != null ? descr.Description : member.Name);
                    }

                    continue;
                }

                thisKeys.Add(key);

                if (permissions != null)
                    permissions.Add(key);

                if (registry != null)
                {
                    descr = member.GetCustomAttribute<DescriptionAttribute>();
                    registry.Add(languageID, "Permission." + key, descr != null ? descr.Description : member.Name);
                }
            }

            if (registry != null && thisKeys.Count > 0)
            {
                var displayName = type.GetCustomAttribute<DisplayNameAttribute>();

                var lastColonIndex = thisKeys[0].LastIndexOf(":");
                if (displayName != null &&
                    lastColonIndex > 0 &&
                    lastColonIndex < thisKeys[0].Length - 1 &&
                    thisKeys.TrueForAll(x => x.LastIndexOf(":") == lastColonIndex))
                {
                    registry.Add(languageID, "Permission." + thisKeys[0].Substring(0, lastColonIndex + 1), displayName.DisplayName);
                }
            }

            foreach (var nested in type.GetNestedTypes(BindingFlags.Public | BindingFlags.DeclaredOnly))
                AddKeysFrom(permissions, registry, nested, languageID);
        }
    }
}