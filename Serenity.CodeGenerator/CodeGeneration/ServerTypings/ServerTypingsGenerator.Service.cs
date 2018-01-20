﻿using System;
using System.Collections.Generic;
using System.Reflection;

namespace Serenity.CodeGeneration
{
    public partial class ServerTypingsGenerator : ServerImportGeneratorBase
    {
        private void GenerateService(Type type)
        {
            var codeNamespace = GetNamespace(type);

            cw.Indented("export namespace ");
            var identifier = GetControllerIdentifier(type);
            this.fileIdentifier = identifier;
            sb.Append(identifier);
            generatedTypes.Add((codeNamespace.IsEmptyOrNull() ? "" : codeNamespace + ".") + identifier);

            cw.InBrace(delegate
            {
                var serviceUrl = GetServiceUrlFromRoute(type);
                if (serviceUrl == null)
                    serviceUrl = GetNamespace(type).Replace(".", "/");

                cw.Indented("export const baseUrl = '");
                sb.Append(serviceUrl);
                sb.AppendLine("';");
                sb.AppendLine();

                Type responseType;
                Type requestType;
                string requestParam;

                var methodNames = new List<string>();
                foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public))
                {
                    if (methodNames.Contains(method.Name))
                        continue;

                    if (!IsPublicServiceMethod(method, out requestType, out responseType, out requestParam))
                        continue;

                    methodNames.Add(method.Name);

                    cw.Indented("export declare function ");
                    sb.Append(method.Name);

                    sb.Append("(request: ");
                    MakeFriendlyReference(requestType, codeNamespace);

                    sb.Append(", onSuccess?: (response: ");
                    MakeFriendlyReference(responseType, codeNamespace);
                    sb.AppendLine(") => void, opt?: Q.ServiceOptions<any>): JQueryXHR;");
                }

                sb.AppendLine();
                cw.Indented("export declare const enum ");
                sb.Append("Methods");

                cw.InBrace(delegate
                {
                    var inserted = 0;
                    foreach (var methodName in methodNames)
                    {
                        if (inserted > 0)
                            sb.AppendLine(",");

                        cw.Indented(methodName);
                        sb.Append(" = \"");
                        sb.Append(serviceUrl);
                        sb.Append("/");
                        sb.Append(methodName);
                        sb.Append("\"");

                        inserted++;
                    }

                    sb.AppendLine();
                });

                sb.AppendLine();

                if (methodNames.Count > 0)
                {
                    cw.IndentedLine("[");
                    int i = 0;
                    foreach (var methodName in methodNames)
                    {
                        if (i++ > 0)
                            sb.AppendLine(", ");

                        cw.Indented("    '");
                        sb.Append(methodName);
                        sb.Append("'");
                    }
                    if (i > 0)
                        sb.AppendLine();
                    cw.IndentedLine("].forEach(x => {");
                    cw.Block(delegate ()
                    {
                        cw.Indented("(<any>");
                        sb.Append(identifier);
                        sb.AppendLine(")[x] = function (r, s, o) {");
                        cw.IndentedLine("    return Q.serviceRequest(baseUrl + '/' + x, r, s, o);");
                        cw.IndentedLine("};");
                    });
                    cw.IndentedLine("});");
                }
            });
        }
    }
}