﻿#if ISSOURCEGENERATOR
using Microsoft.CodeAnalysis;
using TypeReference = Microsoft.CodeAnalysis.ITypeSymbol;
using TypeDefinition = Microsoft.CodeAnalysis.ITypeSymbol;
using PropertyDefinition = Microsoft.CodeAnalysis.IPropertySymbol;
using System.Collections.Immutable;
using System.Threading;
#else
using Mono.Cecil;
#endif
using Serenity.Reflection;

namespace Serenity.CodeGeneration
{
    public abstract class TypingsGeneratorBase : ImportGeneratorBase
    {
        private HashSet<string> visited;
        private Queue<TypeDefinition> generateQueue;
        protected List<TypeDefinition> lookupScripts;
        protected HashSet<string> localTextKeys;
        protected HashSet<string> generatedTypes;
        protected string fileIdentifier;
        protected List<AnnotationTypeInfo> annotationTypes;
        private readonly CancellationToken cancellationToken;

#if ISSOURCEGENERATOR
        protected TypingsGeneratorBase(Compilation compilation, CancellationToken cancellationToken)
        {
            Compilation = compilation ?? throw new ArgumentNullException(nameof(compilation));
            this.cancellationToken = cancellationToken;
        }

        public Compilation Compilation { get; }

        internal class ExportedTypesCollector : SymbolVisitor
        {
            private readonly CancellationToken _cancellationToken;
            private readonly HashSet<INamedTypeSymbol> _exportedTypes;

            public ExportedTypesCollector(CancellationToken cancellation)
            {
                _cancellationToken = cancellation;
                _exportedTypes = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
            }

            public ImmutableArray<INamedTypeSymbol> GetPublicTypes() => _exportedTypes.ToImmutableArray();

            public override void VisitAssembly(IAssemblySymbol symbol)
            {
                _cancellationToken.ThrowIfCancellationRequested();
                symbol.GlobalNamespace.Accept(this);
            }

            public override void VisitNamespace(INamespaceSymbol symbol)
            {
                foreach (INamespaceOrTypeSymbol namespaceOrType in symbol.GetMembers())
                {
                    _cancellationToken.ThrowIfCancellationRequested();
                    namespaceOrType.Accept(this);
                }
            }

            public static bool IsAccessibleOutsideOfAssembly(ISymbol symbol) =>
                symbol.DeclaredAccessibility switch
                {
                    Accessibility.Protected => true,
                    Accessibility.ProtectedOrInternal => true,
                    Accessibility.Public => true,
                    _ => false
                };

            public override void VisitNamedType(INamedTypeSymbol type)
            {
                _cancellationToken.ThrowIfCancellationRequested();

                if (!IsAccessibleOutsideOfAssembly(type) || !_exportedTypes.Add(type))
                    return;

                var nestedTypes = type.GetTypeMembers();

                if (nestedTypes.IsDefaultOrEmpty)
                    return;

                foreach (INamedTypeSymbol nestedType in nestedTypes)
                {
                    _cancellationToken.ThrowIfCancellationRequested();
                    nestedType.Accept(this);
                }
            }
        }
#else
        protected TypingsGeneratorBase(params Assembly[] assemblies)
            : this(TypingsUtils.ToDefinitions(assemblies?.Select(x => x.Location)))
        {
        }

        protected TypingsGeneratorBase(params string[] assemblyLocations)
            : this(TypingsUtils.ToDefinitions(assemblyLocations))
        {
        }

        protected TypingsGeneratorBase(params AssemblyDefinition[] assemblies)
            : base()
        {
            generatedTypes = new HashSet<string>();
            annotationTypes = new List<AnnotationTypeInfo>();

            if (assemblies == null || assemblies.Length == 0)
                throw new ArgumentNullException(nameof(assemblies));

            Assemblies = assemblies;
        }

        public AssemblyDefinition[] Assemblies { get; private set; }
#endif

        protected virtual bool EnqueueType(TypeDefinition type)
        {
            if (visited.Contains(type.FullName()))
                return false;

            visited.Add(type.FullName());
            generateQueue.Enqueue(type);
            return true;
        }

        private void EnqueueMemberType(TypeReference memberType)
        {
            if (memberType == null)
                return;

            var enumType = TypingsUtils.GetEnumTypeFrom(memberType);
            if (enumType != null)
                EnqueueType(enumType);
        }

        protected virtual void EnqueueTypeMembers(TypeDefinition type)
        {
#if ISSOURCEGENERATOR
            foreach (var member in type.GetMembers())
            {
                if (member.IsStatic ||
                    member.DeclaredAccessibility != Accessibility.Public)
                    continue;

                if (member is IFieldSymbol fieldSymbol &&
                    !fieldSymbol.IsStatic)
                    EnqueueMemberType(fieldSymbol.Type);
                else if (member is IPropertySymbol propertySymbol &&
                    TypingsUtils.IsPublicInstanceProperty(propertySymbol))
                    EnqueueMemberType(propertySymbol.Type);
            }

#else
            foreach (var field in type.GetFields())
                if (!field.IsStatic && field.IsPublic)
                    EnqueueMemberType(field.FieldType);

            foreach (var property in type.Properties)
            {
                if (!TypingsUtils.IsPublicInstanceProperty(property))
                    continue;

                EnqueueMemberType(property.PropertyType);
            }
#endif
        }

        protected virtual string GetNamespace(TypeReference type)
        {
            var ns = type.Namespace() ?? "";
#if ISSOURCEGENERATOR
            if (string.IsNullOrEmpty(ns) && type.ContainingType != null)
                ns = type.ContainingType.Namespace();
#else
            if (string.IsNullOrEmpty(ns) && type.IsNested)
                ns = type.DeclaringType.Namespace;
#endif

            if (ns.EndsWith(".Entities", StringComparison.Ordinal))
                return ns[..^".Entities".Length];

            if (ns.EndsWith(".Endpoints", StringComparison.Ordinal))
                return ns[..^".Endpoints".Length];

            if (ns.EndsWith(".Forms", StringComparison.Ordinal))
                return ns[..^".Forms".Length];

            if (ns.EndsWith(".Columns", StringComparison.Ordinal))
                return ns[..^".Columns".Length];

            return ns;
        }

        protected virtual string GetControllerIdentifier(TypeReference controller)
        {
            string className = controller.Name;

            if (className.EndsWith("Controller", StringComparison.Ordinal))
                className = className[0..^10];

            return className + "Service";
        }

        protected override void Reset()
        {
            base.Reset();

            cw.BraceOnSameLine = true;
            generateQueue = new Queue<TypeDefinition>();
            visited = new HashSet<string>();
            lookupScripts = new List<TypeDefinition>();
            localTextKeys = new HashSet<string>();
        }

        protected override void GenerateAll()
        {
            var visitedForAnnotations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

#if ISSOURCEGENERATOR
            var types = Compilation.GetSymbolsWithName(s => true, SymbolFilter.Type).OfType<ITypeSymbol>();

            foreach (var expType in new ExportedTypesCollector(cancellationToken).GetPublicTypes())
                ScanAnnotationTypeAttributes(expType);
#else
            foreach (var assembly in Assemblies)
            {
                foreach (var module in assembly.Modules)
                {
                    TypeDefinition[] types;
                    try
                    {
                        types = module.Types.ToArray();
                    }
                    catch
                    {
                        // skip assemblies that doesn't like to list its types (e.g. some SignalR reported in #2340)
                        continue;
                    }

                    if (module.HasAssemblyReferences)
                    {
                        foreach (var refAsm in module.AssemblyReferences)
                        { 
                            if (!visitedForAnnotations.Contains(refAsm.Name))
                            {
                                visitedForAnnotations.Add(refAsm.Name);

                                if (SkipPackages.ForAnnotations(refAsm.Name))
                                    continue;

                                if (Assemblies.Any(x => string.Equals(x.Name.Name, 
                                    refAsm.Name, StringComparison.OrdinalIgnoreCase)))
                                    continue;

                                try
                                {
                                    
                                    var refDef = module.AssemblyResolver.Resolve(refAsm);
                                    if (refDef != null)
                                    {
                                        foreach (var refMod in refDef.Modules)
                                        {
                                            foreach (var refType in refMod.GetTypes())
                                            {
                                                ScanAnnotationTypeAttributes(refType);
                                            }
                                        }
                                    }
                                }
                                catch
                                {
                                }
                            }
                        }
                    }
#endif

                    TypeDefinition[] emptyTypes = Array.Empty<TypeDefinition>();

                    foreach (var fromType in types)
                    {
                        var nestedLocalTexts = TypingsUtils.GetAttr(fromType, "Serenity.Extensibility",
                            "NestedLocalTextsAttribute", emptyTypes) ??
                            TypingsUtils.GetAttr(fromType, "Serenity.ComponentModel", 
                                "NestedLocalTextsAttribute", emptyTypes);
                        if (nestedLocalTexts != null)
                        {
                            string prefix = null;
#if ISSOURCEGENERATOR
                            prefix = nestedLocalTexts.NamedArguments.FirstOrDefault(x => x.Key == "Prefix").Value.Value as string;
#else
                            if (nestedLocalTexts.HasProperties)
                                prefix = nestedLocalTexts.Properties.FirstOrDefault(x => x.Name == "Prefix").Argument.Value as string;
#endif

                            AddNestedLocalTexts(fromType, prefix ?? "");
                        }

                        ScanAnnotationTypeAttributes(fromType);

                        if (fromType.IsAbstract ||
                            TypingsUtils.GetAttr(fromType, "Serenity.ComponentModel", "ScriptSkipAttribute") != null)
                            continue;

                        var baseClasses = TypingsUtils.EnumerateBaseClasses(fromType).ToArray();

                        if (TypingsUtils.Contains(baseClasses, "Serenity.Services", "ServiceRequest") ||
                            TypingsUtils.Contains(baseClasses, "Serenity.Services", "ServiceResponse") ||
                            TypingsUtils.Contains(baseClasses, "Serenity.Data", "Row") ||
                            TypingsUtils.Contains(baseClasses, "Serenity.Data", "Row`1") ||
                            TypingsUtils.Contains(baseClasses, "Serenity.Services", "ServiceEndpoint") ||
                            TypingsUtils.GetAttr(fromType, "Serenity.ComponentModel", "ScriptIncludeAttribute", baseClasses) != null ||
                            TypingsUtils.GetAttr(fromType, "Serenity.ComponentModel", "FormScriptAttribute", baseClasses) != null ||
                            TypingsUtils.GetAttr(fromType, "Serenity.ComponentModel", "ColumnsScriptAttribute", baseClasses) != null ||
                            TypingsUtils.GetAttr(fromType, "Serenity.Extensibility", "NestedPermissionKeysAttribute", emptyTypes) != null ||
                            TypingsUtils.GetAttr(fromType, "Serenity.ComponentModel", "NestedPermissionKeysAttribute", emptyTypes) != null ||
                            ((TypingsUtils.Contains(baseClasses, "Microsoft.AspNetCore.Mvc", "Controller") ||
                              TypingsUtils.Contains(baseClasses, "System.Web.Mvc", "Controller")) && // backwards compability
                             fromType.Namespace()?.EndsWith(".Endpoints", StringComparison.Ordinal) == true))
                        {
                            EnqueueType(fromType);
                            continue;
                        }

                        if (TypingsUtils.GetAttr(fromType, "Serenity.ComponentModel", "LookupScriptAttribute", baseClasses) != null)
                            lookupScripts.Add(fromType);
                    }
#if !ISSOURCEGENERATOR
        }
            }
#endif

            while (generateQueue.Count > 0)
            {
                var typeDef = generateQueue.Dequeue();

#if ISSOURCEGENERATOR
                if (!typeDef.Locations.Any(x => !x.IsInMetadata))
                    continue;
#else
                if (!Assemblies.Any(x => x.FullName == typeDef.Module.Assembly.FullName))
                    continue;
#endif

                var ns = GetNamespace(typeDef);
                fileIdentifier = typeDef.Name;

                GenerateCodeFor(typeDef);

                AddFile(RemoveRootNamespace(ns, fileIdentifier + ".ts"));
            }
        }

        private void ScanAnnotationTypeAttributes(TypeDefinition fromType)
        {
            var annotationTypeAttrs = TypingsUtils.GetAttrs(
#if ISSOURCEGENERATOR
                fromType.GetAttributes(),
#else
                fromType.CustomAttributes,
#endif
                "Serenity.ComponentModel", "AnnotationTypeAttribute", null);

            if (!annotationTypeAttrs.Any())
                return;

            var typeInfo = new AnnotationTypeInfo(fromType);
            foreach (var attr in annotationTypeAttrs)
            {
                var attrInfo = new AnnotationTypeInfo.AttributeInfo();
#if ISSOURCEGENERATOR
                if (attr.ConstructorArguments.FirstOrDefault(x =>
#else
                if (attr.ConstructorArguments?.FirstOrDefault(x =>
#endif
                    x.Type.FullName() == "System.Type").Value is not TypeReference annotatedType)
                    continue;

#if ISSOURCEGENERATOR
                attrInfo.AnnotatedType = annotatedType;
#else
                attrInfo.AnnotatedType = annotatedType.Resolve();
#endif

#if ISSOURCEGENERATOR
                if (attr.NamedArguments.Any())
                {
                }
#else
                if (attr.HasProperties)
                {
                    attrInfo.Inherited = attr.Properties.FirstOrDefault(x =>
                        x.Name == "Inherited").Argument.Value as bool? ?? true;

                    attrInfo.Namespaces = attr.Properties.FirstOrDefault(x =>
                        x.Name == "Namespaces").Argument.Value as string[];

                    attrInfo.Properties = attr.Properties.FirstOrDefault(x =>
                        x.Name == "Properties").Argument.Value as string[];
                }
#endif
                else
                    attrInfo.Inherited = true;
                typeInfo.Attributes.Add(attrInfo);
            }

            if (typeInfo.Attributes.Count > 0)
                annotationTypes.Add(typeInfo);
        }

        protected List<AnnotationTypeInfo> GetAnnotationTypesFor(TypeDefinition type)
        {
            var list = new List<AnnotationTypeInfo>();
            TypeReference[] baseClasses = null;
            foreach (var annotationType in annotationTypes)
            {
                var annotationMatch = false;

                foreach (var attr in annotationType.Attributes)
                {
                    baseClasses ??= TypingsUtils.EnumerateBaseClasses(type).ToArray();

                    if (TypingsUtils.IsOrSubClassOf(attr.AnnotatedType, "System", "Attribute"))
                    {
                        if (TypingsUtils.GetAttr(type, attr.AnnotatedType.Namespace(), 
                            attr.AnnotatedType.Name, baseClasses) == null) 
                            continue;
                    }
                    else if (attr.Inherited ||
#if ISSOURCEGENERATOR
                        attr.AnnotatedType.TypeKind == TypeKind.Interface)
#else
                        attr.AnnotatedType.IsInterface)
#endif
                    {
                        if (!TypingsUtils.IsAssignableFrom(attr.AnnotatedType, type))
                            continue;
                    }
#if ISSOURCEGENERATOR
                    else if (!type.Equals(attr.AnnotatedType, SymbolEqualityComparer.Default))
#else
                    else if (type != attr.AnnotatedType)
#endif
                        continue;

                    if (attr.Namespaces != null && attr.Namespaces.Length > 0)
                    {
                        bool namespaceMatch = false;
                        foreach (var ns in attr.Namespaces)
                        {
                            if (type.Namespace() == ns)
                            {
                                namespaceMatch = true;
                                break;
                            }

                            if (ns.Length > 2 &&
                                ns.EndsWith(".*", StringComparison.OrdinalIgnoreCase) &&
                                type.Namespace != null)
                            {
                                if (type.Namespace() == ns[0..^2] ||
                                    type.Namespace().StartsWith(ns[0..^1], StringComparison.OrdinalIgnoreCase))
                                {
                                    namespaceMatch = true;
                                    break;
                                }
                            }
                        }

                        if (!namespaceMatch)
                            continue;
                    }

                    if (attr.Properties != null &&
                        attr.Properties.Length > 0 &&
                        attr.Properties.Any(name => !type.GetProperties().Any(p =>
                            p.Name == name && TypingsUtils.IsPublicInstanceProperty(p))))
                        continue;

                    annotationMatch = true;
                    break;
                }

                if (annotationMatch)
                    list.Add(annotationType);
            }

            return list;
        }


        protected abstract void HandleMemberType(TypeReference memberType, string codeNamespace, StringBuilder sb = null);

        public static bool CanHandleType(TypeDefinition memberType)
        {
#if ISSOURCEGENERATOR
            if (memberType.TypeKind == TypeKind.Interface)
#else
            if (memberType.IsInterface)
#endif
                return false;

            if (memberType.IsAbstract)
                return false;

            if (TypingsUtils.IsOrSubClassOf(memberType, "System", "Delegate"))
                return false;

            return true;
        }

        public virtual string ShortenNamespace(TypeReference type, string codeNamespace)
        {
            string ns = GetNamespace(type);

            if (ns == "Serenity.Services" ||
                ns == "Serenity.ComponentModel")
            {
                if (IsUsingNamespace("Serenity"))
                    return "";
                else
                    return "Serenity";
            }

            if ((codeNamespace != null && (ns == codeNamespace)) ||
                (codeNamespace != null && codeNamespace.StartsWith(ns + ".", StringComparison.Ordinal)))
            {
                return "";
            }

            if (IsUsingNamespace(ns))
                return "";

            if (codeNamespace != null)
            {
                var idx = codeNamespace.IndexOf('.', StringComparison.Ordinal);
                if (idx >= 0 && ns.StartsWith(codeNamespace[..(idx + 1)], StringComparison.Ordinal))
                    return ns[(idx + 1)..];
            }

            return ns;
        }

        protected virtual bool IsUsingNamespace(string ns)
        {
            return false;
        }

        protected virtual string ShortenNamespace(ExternalType type, string codeNamespace)
        {
            string ns = type.Namespace ?? "";

            if ((codeNamespace != null && (ns == codeNamespace)) ||
                (codeNamespace != null && codeNamespace.StartsWith(ns + ".", StringComparison.Ordinal)))
            {
                return "";
            }

            if (IsUsingNamespace(ns))
                return "";

            if (codeNamespace != null)
            {
                var idx = codeNamespace.IndexOf('.', StringComparison.Ordinal);
                if (idx >= 0 && ns.StartsWith(codeNamespace[..(idx + 1)], StringComparison.Ordinal))
                    return ns[(idx + 1)..];
            }

            return ns;
        }

        protected virtual string MakeFriendlyName(TypeReference type, string codeNamespace, StringBuilder sb = null)
        {
            sb ??= this.sb;

            if (type.IsGenericInstance())
            {
                var name = type.Name;
                var idx = name.IndexOf('`', StringComparison.Ordinal);
                if (idx >= 0)
                    name = name[..idx];

                sb.Append(name);
                sb.Append('<');

                int i = 0;
#if ISSOURCEGENERATOR
                var nt = (INamedTypeSymbol)type;
                foreach (var argument in nt.TypeArguments)
#else
                foreach (var argument in (type as GenericInstanceType).GenericArguments)
#endif
                {
                    if (i++ > 0)
                        sb.Append(", ");

                    HandleMemberType(argument, codeNamespace, sb);
                }

                sb.Append('>');

                return name + "`" +
#if ISSOURCEGENERATOR
                    nt.TypeArguments.Length;
#else
                    (type as GenericInstanceType).GenericArguments.Count;
#endif
            }
#if ISSOURCEGENERATOR
            else if (type is INamedTypeSymbol nt2 && nt2.TypeParameters.Length > 0)
#else
            else if (type.HasGenericParameters)
#endif
            {
                var name = type.Name;
                var idx = name.IndexOf('`', StringComparison.Ordinal);
                if (idx >= 0)
                    name = name[..idx];

                sb.Append(name);
                sb.Append('<');

                int i = 0;
#if ISSOURCEGENERATOR
                foreach (var argument in nt2.TypeParameters)
#else
                foreach (var argument in type.GenericParameters)
#endif
                {
                    if (i++ > 0)
                        sb.Append(", ");

                    sb.Append(argument.Name);
                }

                sb.Append('>');

                return name + "`" +
#if ISSOURCEGENERATOR
                    nt2.TypeParameters.Length;
#else
                    type.GenericParameters.Count;
#endif
            }
            else
            {
                sb.Append(type.Name);
                return type.Name;
            }
        }

        protected virtual void MakeFriendlyReference(TypeReference type, string codeNamespace, StringBuilder sb = null)
        {
            sb ??= this.sb;

            string ns;

#if ISSOURCEGENERATOR
            if (type is INamedTypeSymbol nt1 && nt1.IsGenericType())
#else
            if (type.IsGenericInstance)
#endif
            {
                ns = ShortenNamespace(type, codeNamespace);

                if (!string.IsNullOrEmpty(ns))
                {
                    sb.Append(ns);
                    sb.Append('.');
                }

                var name = type.Name;
                var idx = name.IndexOf('`', StringComparison.Ordinal);
                if (idx >= 0)
                    name = name[..idx];

                sb.Append(name);
                sb.Append('<');

                int i = 0;
                foreach (var argument in (type as GenericInstanceType).GenericArguments)
                {
                    if (i++ > 0)
                        sb.Append(", ");

                    HandleMemberType(argument, codeNamespace);
                }

                sb.Append('>');
                return;
            }

            if (codeNamespace != null)
            {
                ns = ShortenNamespace(type, codeNamespace);
                if (!string.IsNullOrEmpty(ns))
                    sb.Append(ns + "." + type.Name);
                else
                    sb.Append(type.Name);
            }
            else
                sb.Append(type.Name);
        }

        protected static TypeReference GetBaseClass(TypeDefinition type)
        {
            foreach (var t in TypingsUtils.SelfAndBaseClasses(type))
            {
                if (t.BaseType != null &&
                    t.BaseType.IsGenericInstance &&
                    (t.BaseType as GenericInstanceType).ElementType.Namespace == "Serenity.Services")
                {
                    var n = (t.BaseType as GenericInstanceType).ElementType.Name;
                    if (n == "ListResponse`1" || n == "RetrieveResponse`1" || n == "SaveRequest`1")
                        return t.BaseType;
                }

                if (t.Namespace != "Serenity.Services")
                    continue;

                if (t.Name == "ListRequest" ||
                    t.Name == "RetrieveRequest" ||
                    t.Name == "DeleteRequest" ||
                    t.Name == "DeleteResponse" ||
                    t.Name == "UndeleteRequest" ||
                    t.Name == "UndeleteResponse" ||
                    t.Name == "SaveResponse" ||
                    t.Name == "ServiceRequest" ||
                    t.Name == "ServiceResponse")
                    return t;
            }

            return null;
        }

        protected abstract void GenerateCodeFor(TypeDefinition type);

        protected static bool IsPublicServiceMethod(MethodDefinition method, out TypeReference requestType, out TypeReference responseType,
            out string requestParam)
        {
            if (method == null)
                throw new ArgumentNullException(nameof(method));

            responseType = null;
            requestType = null;
            requestParam = null;

            if ((TypingsUtils.FindAttr(method.CustomAttributes, "System.Web.Mvc", "NonActionAttribute") ??
                 TypingsUtils.FindAttr(method.CustomAttributes, "Microsoft.AspNetCore.Mvc", "NonActionAttribute") ??
                 TypingsUtils.FindAttr(method.CustomAttributes, "Serenity.ComponentModel", "ScriptSkipAttribute")) != null)
                return false;

            if (!TypingsUtils.IsSubclassOf(method.DeclaringType, "System.Web.Mvc", "Controller") &&
                !TypingsUtils.IsSubclassOf(method.DeclaringType, "Microsoft.AspNetCore.Mvc", "Controller"))
                return false;

            if (method.IsSpecialName && (method.Name.StartsWith("set_", StringComparison.Ordinal) || method.Name.StartsWith("get_", StringComparison.Ordinal)))
                return false;

            var parameters = method.Parameters.Where(x => !x.ParameterType.Resolve().IsInterface &&
                TypingsUtils.FindAttr(x.CustomAttributes, "Microsoft.AspNetCore.Mvc", "FromServicesAttribute") == null).ToArray();

            if (parameters.Length > 1)
                return false;

            if (parameters.Length == 1)
            {
                requestType = parameters[0].ParameterType;
                if (requestType.IsPrimitive || !CanHandleType(requestType.Resolve()))
                    return false;
            }
            else
                requestType = null;

            requestParam = parameters.Length == 0 ? "request" : parameters[0].Name;

            responseType = method.ReturnType;
            if (responseType != null &&
                responseType.IsGenericInstance &&
                (responseType as GenericInstanceType).ElementType.FullName.StartsWith("Serenity.Services.Result`1", StringComparison.Ordinal))
            {
                responseType = (responseType as GenericInstanceType).GenericArguments[0];
                return true;
            }
            else if (responseType != null &&
                responseType.IsGenericInstance &&
                (responseType as GenericInstanceType).ElementType.FullName.StartsWith("System.Threading.Tasks.Task`1", StringComparison.Ordinal))
            {
                responseType = (responseType as GenericInstanceType).GenericArguments[0];
                return true;
            }
            else if (TypingsUtils.IsOrSubClassOf(responseType, "System.Web.Mvc", "ActionResult") ||
                TypingsUtils.IsAssignableFrom("Microsoft.AspNetCore.Mvc.IActionResult", responseType.Resolve()))
                return false;
            else if (responseType == null || TypingsUtils.IsVoid(responseType))
                return false;

            return true;
        }

        protected static string GetServiceUrlFromRoute(TypeDefinition controller)
        {
            if (controller == null)
                throw new ArgumentNullException(nameof(controller));

            var route = TypingsUtils.GetAttr(controller, "System.Web.Mvc", "RouteAttribute") ??
                TypingsUtils.GetAttr(controller, "Microsoft.AspNetCore.Mvc", "RouteAttribute");
            string url = route == null || route.ConstructorArguments.Count == 0 || route.ConstructorArguments[0].Value is not string ? 
                ("Services/HasNoRoute/" + controller.Name) : (route.ConstructorArguments[0].Value as string ?? "");

            url = url.Replace("[controller]", controller.Name[..^"Controller".Length], StringComparison.Ordinal);
            url = url.Replace("/[action]", "", StringComparison.Ordinal);

            if (!url.StartsWith("~/", StringComparison.Ordinal) && !url.StartsWith("/", StringComparison.Ordinal))
                url = "~/" + url;

            while (true)
            {
                var idx1 = url.IndexOf('{', StringComparison.Ordinal);
                if (idx1 <= 0)
                    break;

                var idx2 = url.IndexOf("}", idx1 + 1, StringComparison.Ordinal);
                if (idx2 <= 0)
                    break;

                url = url[..idx1] + url[(idx2 + 1)..];
            }

            if (url.StartsWith("~/Services/", StringComparison.OrdinalIgnoreCase))
                url = url["~/Services/".Length..];

            if (url.Length > 1 && url.EndsWith("/", StringComparison.Ordinal))
                url = url[0..^1];

            return url;
        }

        protected virtual void AddNestedLocalTexts(TypeDefinition type, string prefix)
        {
        }

        protected class AnnotationTypeInfo
        {
            public TypeDefinition AnnotationType { get; private set; }
            public List<AttributeInfo> Attributes { get; private set; }
            public Dictionary<string, PropertyDefinition> PropertyByName { get; private set; }

            public AnnotationTypeInfo(TypeDefinition annotationType)
            {
                AnnotationType = annotationType;
                PropertyByName = new Dictionary<string, PropertyDefinition>();
                Attributes = new List<AttributeInfo>();

                foreach (var property in annotationType.Properties)
                    if (TypingsUtils.IsPublicInstanceProperty(property))
                        PropertyByName[property.Name] = property;
            }

            public class AttributeInfo
            {
                public TypeDefinition AnnotatedType { get; set; }
                public bool Inherited { get; set; }
                public string[] Namespaces { get; set; }
                public string[] Properties { get; set; }
            }
        }
    }
}