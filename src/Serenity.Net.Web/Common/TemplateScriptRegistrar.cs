﻿using System;
using System.Collections.Generic;
using System.IO;

namespace Serenity.Web
{
    public class TemplateScriptRegistrar
    {
        private static readonly string[] TemplateSuffixes = new[] { ".Template.html", ".ts.html" };

        private ConcatenatedScript bundle;
        private Dictionary<string, TemplateScript> scriptByKey = new Dictionary<string, TemplateScript>(StringComparer.OrdinalIgnoreCase);

        private static string GetKey(string rootPath, string filename)
        {
            string key = Path.GetFileName(filename);
            bool isModulesFolder = rootPath.EndsWith(Path.DirectorySeparatorChar + "Modules" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);

            foreach (var suffix in TemplateSuffixes)
                if (key.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    key = key.Substring(0, key.Length - suffix.Length);

                    if (isModulesFolder && filename.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
                    {
                        filename = filename.Substring(rootPath.Length);

                        var moduleEnd = filename.IndexOf(Path.DirectorySeparatorChar);
                        if (moduleEnd >= 0)
                        {
                            var module = filename.Substring(0, moduleEnd);
                            if (!key.StartsWith(module + ".", StringComparison.OrdinalIgnoreCase))
                                return module + "." + key;
                        }
                    }

                    return key;
                }

            return null;
        }

        private void Changed(string rootPath, string name)
        {
            string key = GetKey(rootPath, rootPath + name);
            if (key == null)
                return;

            TemplateScript ts;
            if (scriptByKey.TryGetValue(key, out ts))
                ts.Changed();

            if (bundle != null)
                bundle.Changed();
        }

        public void Initialize(IDynamicScriptManager manager, string[] paths, bool watchForChanges = true)
        {
            var bundleList = new List<Func<string>>();

            foreach (var p in paths)
            {
                var path = p;
                if (!path.EndsWith(Path.DirectorySeparatorChar.ToString()))
                    path = path + Path.DirectorySeparatorChar;

                if (!Directory.Exists(path))
                    continue;

                foreach (var file in Directory.EnumerateFiles(path, "*.html", SearchOption.AllDirectories))
                {
                    var key = GetKey(path, file);
                    if (key == null)
                        continue;

                    var script = new TemplateScript(key, () => File.ReadAllText(file));
                    manager.Register(script);
                    scriptByKey[key.ToLowerInvariant()] = script;
                    bundleList.Add(script.GetScript);
                }

                if (watchForChanges)
                {
                    var watcher = new FileWatcher(path, "*.html");
                    watcher.Changed += name => Changed(path, name);
                }
            }

            bundle = new ConcatenatedScript(bundleList);
            manager.Register("TemplateBundle", bundle);
        }
    }
}