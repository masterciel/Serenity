﻿#if !COREFX
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Serenity.CodeGeneration
{
    public class TypeScriptParser
    {
        static LexerSettings tsSettings = new LexerSettings
        {
            InlineComments = new string[] { "//" },
            StringEscapeChar = '\\',
            CultureInfo = CultureInfo.InvariantCulture,
            CompareInfo = CultureInfo.InvariantCulture.CompareInfo,
            DecimalSeparator = ".",
            Options = LexerOptions.StringEscaping,
            IdentChars = new char[] { '$' },
            StringQuotes = new char[] { '\"', '\'' },
            CommentBegin = "/*",
            CommentEnd = "*/"
        };

        private Token prior;
        private Token token;
        private Lexer lexer;
        private TokenType type;
        private string text;
        private Stack<NamespaceInfo> namespaceStack = new Stack<NamespaceInfo>();
        private Stack<TypeInfo> typeStack = new Stack<TypeInfo>();
        private Stack<string> braceStack = new Stack<string>();
        private Stack<string> modifiers = new Stack<string>();

        public TypeScriptParser()
        {
        }

        public void Parse(string input)
        {
            modifiers.Clear();
            namespaceStack.Clear();
            typeStack.Clear();
            namespaceStack.Push(new NamespaceInfo { Name = "", Imports = new Dictionary<string, string>() });
            lexer = new Lexer(input, LexerBehavior.SkipComments | LexerBehavior.PersistTokenText, tsSettings);
            while (Next() && Parse()) ;
        }

        public Stack<NamespaceInfo> NamespaceStack { get { return namespaceStack; } }
        public Stack<TypeInfo> TypeStack { get { return typeStack; } }
        public Stack<string> BraceStack { get { return braceStack; } }

        private Token Peek()
        {
            return lexer.PeekNextToken();
        }

        private bool NextCharIf(string c)
        {
            return NextIf(TokenType.Char, c);
        }

        private bool NextIf(TokenType type)
        {
            var peek = Peek();
            if (peek.Type == type)
            {
                Next();
                return true;
            }

            return false;
        }

        private bool Is(TokenType type, string text)
        {
            return this.type == type && this.text == text;
        }

        private bool NextIf(TokenType type, string text)
        {
            var peek = Peek();
            if (peek.Type == type && peek.Text == text)
            {
                Next();
                return true;
            }

            return false;
        }

        private bool NextIf(Func<Token, bool> check)
        {
            var peek = Peek();
            if (peek.Type != TokenType.End && check(peek))
            {
                Next();
                return true;
            }

            return false;
        }

        private bool Next()
        {
            prior = token;
            token = lexer.GetNextToken();
            type = token.Type;
            text = token.Text;

            if (ReportToken != null)
                ReportToken(token);

            return token.Type != TokenType.End;
        }

        public Action<Token> ReportToken;
        public Action<TypeInfo> ReportType;

        public bool SkipWhitespace()
        {
            var result = false;

            while (Next() && (type == TokenType.WhiteSpace ||
                    type == TokenType.EndOfLine))
            {
                result = true;
            }

            return result;
        }

        public bool IsWhitespace()
        {
            return type == TokenType.WhiteSpace ||
                type == TokenType.EndOfLine;
        }

        public string ParseDottedIdentifier()
        {
            string key = "";
            do
            {
                switch (type)
                {
                    case TokenType.Identifier:
                        if (key == "" ||
                            key.EndsWith("."))
                            key += text;
                        else
                            return null;

                        break;

                    case TokenType.Char:
                        if (text != ".")
                        {
                            if (key.Length == 0 ||
                                key.EndsWith("."))
                                return null;
                        }

                        if (key.EndsWith("."))
                            return null;

                        key += ".";

                        break;

                    default:
                        if (key.Length == 0 ||
                            key.EndsWith("."))
                            return null;

                        return key;
                }
            }
            while (Next());

            return key;
        }

        public string ParseBaseTypeIdentifier()
        {
            string key = "";
            do
            {
                switch (type)
                {
                    case TokenType.Identifier:
                        string ident;
                        if (key != "" || !namespaceStack.Peek().Imports.TryGetValue(text, out ident))
                            ident = text;

                        if (key == "" ||
                            key.EndsWith("."))
                            key += ident;
                        else
                            return null;

                        break;

                    case TokenType.Char:
                        if (text == "<")
                        {
                            if (key == "" || key.EndsWith("."))
                                return null;

                            key += "<";

                            while (true)
                            {
                                SkipWhitespace();

                                var part = ParseBaseTypeIdentifier();
                                if (part == null)
                                    return null;

                                key += part;

                                if (IsWhitespace())
                                    SkipWhitespace();

                                if (type == TokenType.Char &&
                                    text == ">")
                                    break;

                                if (type == TokenType.Char &&
                                    text == ",")
                                {
                                    key += ", ";
                                    continue;
                                }

                                return null;
                            }

                            if (key == "<" || key.EndsWith(", "))
                                return null;

                            key += ">";
                            Next();
                            return key;
                        }
                        else if (text != ".")
                        {
                            if (key.Length == 0 ||
                                key.EndsWith("."))
                                return null;

                            return key;
                        }

                        if (key.EndsWith("."))
                            return null;

                        key += ".";
                        break;

                    default:
                        if (key.Length == 0 ||
                            key.EndsWith("."))
                            return null;

                        return key;
                }
            }
            while (Next());

            return key;
        }

        public string ParseGenericConstraint()
        {
            string key = "";
            do
            {
                switch (type)
                {
                    case TokenType.Identifier:

                        if (key == "")
                            key += text;
                        else
                            return null;

                        break;

                    case TokenType.WhiteSpace:
                    case TokenType.EndOfLine:
                        SkipWhitespace();
                        if (type == TokenType.Identifier &&
                            (text == "extends" ||
                             text == "implements"))
                        {
                            if (key == "")
                                return null;

                            key += " " + text + " ";

                            if (!SkipWhitespace())
                                return null;

                            var part = ParseBaseTypeIdentifier();
                            if (part == null)
                                return null;

                            key += part;
                            return key;
                        }
                        else if (key == "")
                            return null;
                        else
                            return key;

                    default:
                        if (key.Length == 0)
                            return null;

                        return key;
                }
            }
            while (Next());

            return key;
        }

        public string ParseGenericIdentifier()
        {
            string key = "";
            do
            {
                switch (type)
                {
                    case TokenType.Identifier:
                        string ident;
                        if (key != "" || !namespaceStack.Peek().Imports.TryGetValue(text, out ident))
                            ident = text;

                        if (key == "")
                            key += ident;
                        else
                            return null;

                        break;

                    case TokenType.Char:
                        if (text == "<")
                        {
                            if (key == "")
                                return null;

                            key += "<";

                            while (true)
                            {
                                SkipWhitespace();

                                var part = ParseGenericConstraint();
                                if (part == null)
                                    return null;

                                key += part;

                                if (IsWhitespace())
                                    SkipWhitespace();

                                if (type == TokenType.Char &&
                                    text == ">")
                                    break;

                                if (type == TokenType.Char &&
                                    text == ",")
                                {
                                    key += ", ";
                                    continue;
                                }

                                return null;
                            }

                            if (key == "<" || key.EndsWith(", "))
                                return null;

                            key += ">";
                            Next();
                            return key;
                        }
                        else if (key.Length == 0)
                            return null;

                        return key;

                    default:
                        if (key.Length == 0)
                            return null;

                        return key;
                }
            }
            while (Next());

            return key;
        }

        private bool ParseNamespace()
        {
            if (!SkipWhitespace())
                return false;

            var ns = ParseDottedIdentifier();
            if (ns == null)
                return false;

            if (IsWhitespace())
                SkipWhitespace();

            if (!Is(TokenType.Char, "{"))
                return false;

            var parent = namespaceStack.Peek();
            if (parent.Name.Length > 0)
                ns = parent.Name + "." + ns;

            namespaceStack.Push(new NamespaceInfo
            {
                Name = ns,
                Imports = new Dictionary<string, string>(parent.Imports),
                IsDeclaration = parent != null && parent.IsDeclaration ||
                    modifiers.Contains("declare")
            });

            modifiers.Clear();
            braceStack.Push("{namespace");
            return true;
        }

        private void Dump()
        {
        }

        private bool ParseType()
        {
            bool isInterface = text == "interface";

            if (!SkipWhitespace())
                return false;

            if (type != TokenType.Identifier)
                return false;

            var className = ParseGenericIdentifier();
            if (className == null)
                return false;

            typeStack.Push(new TypeInfo
            {
                Modifiers = new List<string>(modifiers),
                Namespace = namespaceStack.Peek(),
                Name = className,
                IsInterface = isInterface,
                Extends = "",
                Implements = "",
                Members = new List<MemberInfo>(),
                IsDeclaration = namespaceStack.Peek().IsDeclaration ||
                    modifiers.Contains("declare")
            });

            modifiers.Clear();
            SkipWhitespace();

            if (Is(TokenType.Identifier, "extends"))
            {
                if (!SkipWhitespace())
                    return false;

                var baseType = ParseBaseTypeIdentifier();
                if (baseType == null)
                    return false;

                typeStack.Peek().Extends = baseType;
            }

            if (Is(TokenType.Identifier, "implements"))
            {
                if (!SkipWhitespace())
                    return false;

                var baseClass = ParseBaseTypeIdentifier();
                if (baseClass == null)
                    return false;

                typeStack.Peek().Implements = baseClass;
            }

            if (IsWhitespace())
                SkipWhitespace();

            if (!Is(TokenType.Char, "{"))
                return false;

            modifiers.Clear();
            braceStack.Push("{type");

            return true;
        }

        private bool ParseImport()
        {
            if (!SkipWhitespace())
                return false;

            if (type != TokenType.Identifier)
                return false;

            var alias = text;

            SkipWhitespace();

            if (type != TokenType.Char ||
                text != "=")
            {
                // not interested with other imports, only aliases
                return Parse();
            }

            SkipWhitespace();
            var identifier = ParseBaseTypeIdentifier();
            if (identifier == null)
                return false;

            namespaceStack.Peek().Imports[alias] = identifier;
            SkipWhitespace();

            if (type == TokenType.Char ||
                text == ";")
                Next();

            return true;
        }

        private string ParseExpression(Func<bool> terminate)
        {
            var startBraceLevel = BraceStack.Count;

            string expression = null;

            do
            {
                if (type == TokenType.End || (startBraceLevel == BraceStack.Count && terminate()))
                    return expression;

                if (type == TokenType.Char && !ParseChar())
                    return null;

                if (type == TokenType.WhiteSpace ||
                    type == TokenType.EndOfLine)
                {
                    if (expression != null && !expression.EndsWith(" "))
                        expression += " ";
                }
                else
                {
                    string import;
                    if (string.IsNullOrEmpty(text) ||
                        !namespaceStack.Peek().Imports.TryGetValue(text, out import) ||
                        prior == null ||
                         (prior.Type == TokenType.Char &&
                          prior.Text == "."))
                    {
                        import = text;
                    }

                    if (expression == null)
                        expression = import;
                    else
                        expression += import;
                }

            } while (Next());

            return expression;
        }

        private string ParseTypeExpressionAndDefault(out string initialValue,
            Func<bool> terminator)
        {
            initialValue = "";

            var memberType = ParseExpression(() =>
            {
                if (Is(TokenType.Char, "=") ||
                    terminator())
                    return true;

                return false;
            });

            if (memberType == null)
                return null;

            if (Is(TokenType.Char, "="))
            {
                SkipWhitespace();

                initialValue = ParseExpression(terminator);

                if (initialValue == null)
                    return null;
            }

            if (terminator())
                return memberType;

            return null;
        }

        private bool ParseTypeMember()
        {
            var identifier = ParseGenericIdentifier();
            if (identifier == null)
                return false;

            if (IsWhitespace())
                SkipWhitespace();

            var member = new MemberInfo
            {
                Modifiers = new List<string>(modifiers),
                Arguments = new List<ArgumentInfo>(),
                Name = identifier,
                IsFunction = false,
                Decorators = new List<DecoratorInfo>(),
                Type = ""
            };

            modifiers.Clear();
            typeStack.Peek().Members.Add(member);

            if (Is(TokenType.Char, "?"))
            {
                member.IsOptional = true;
                SkipWhitespace();
            }

            if (Is(TokenType.Char, ":"))
            {
                SkipWhitespace();

                string defaultValue;
                member.Type = ParseTypeExpressionAndDefault(out defaultValue, () =>
                    Is(TokenType.Char, ";") || type == TokenType.EndOfLine);

                if (member.Type == null)
                    return false;

                member.InitialValue = defaultValue ?? "";
            }
            else if (Is(TokenType.Char, "("))
            {
                member.IsFunction = true;

                while (true)
                {
                    SkipWhitespace();

                    if (type == TokenType.Char &&
                        text == ")")
                        break;

                    if (type != TokenType.Identifier)
                        return false;

                    var argument = new ArgumentInfo()
                    {
                        Name = text,
                        Modifier = "",
                        Decorators = new List<DecoratorInfo>(),
                        InitialValue = "",
                        Type = ""
                    };

                    member.Arguments.Add(argument);

                    if (text == "private" ||
                        text == "public" ||
                        text == "protected")
                    {
                        argument.Modifier = text;

                        SkipWhitespace();
                        if (type != TokenType.Identifier)
                            return false;

                        argument.Name = text;
                    }

                    SkipWhitespace();

                    if (Is(TokenType.Char, "?"))
                    {
                        argument.IsOptional = true;
                        SkipWhitespace();
                    }

                    if (Is(TokenType.Char, ":"))
                    {
                        SkipWhitespace();

                        string defaultValue;
                        argument.Type = ParseTypeExpressionAndDefault(out defaultValue, () =>
                            Is(TokenType.Char, ",") ||
                            Is(TokenType.Char, ")"));

                        if (argument.Type == null)
                            return false;
                    }
                    else if (Is(TokenType.Char, "="))
                    {
                        argument.InitialValue = ParseExpression(() =>
                            Is(TokenType.Char, ",") ||
                            Is(TokenType.Char, ")"));

                        if (argument.InitialValue == null)
                            return false;
                    }

                    if (IsWhitespace())
                        SkipWhitespace();

                    if (Is(TokenType.Char, ","))
                        continue;

                    if (!Is(TokenType.Char, ")"))
                        return false;

                    break;
                }

                SkipWhitespace();

                if (Is(TokenType.Char, ":"))
                {
                    SkipWhitespace();

                    member.Type = ParseExpression(() =>
                        Is(TokenType.Char, ";") ||
                        Is(TokenType.Char, "{"));

                    if (member.Type == null)
                        return false;
                }

                if (IsWhitespace())
                    SkipWhitespace();

                if (Is(TokenType.Char, "{"))
                {
                    braceStack.Push("{");
                    return true;
                }
                else if (Is(TokenType.Char, ";"))
                {
                    return true;
                }
                else
                    return false;
            }
            else if (Is(TokenType.Char, "="))
            {
                SkipWhitespace();

                member.InitialValue = ParseExpression(() =>
                    Is(TokenType.Char, ";") ||
                    type == TokenType.EndOfLine);

                if (member.InitialValue == null)
                    return false;
            }
            else
                return true;

            return true;
        }

        private bool ParseFunction()
        {
            return true;
        }

        private bool ParseVar()
        {
            return true;
        }

        private bool ParseIdentifier()
        {
            bool rootOrNamespace = braceStack.Count == 0 ||
                braceStack.Peek() == "{namespace";

            bool typeBrace = braceStack.Count > 0 &&
                braceStack.Peek() == "{type";

            bool afterSpace = (prior == null || (prior.Type == TokenType.WhiteSpace ||
                prior.Type == TokenType.EndOfLine));

            switch (text)
            {
                case "namespace":
                    if (rootOrNamespace)
                        return ParseNamespace();
                    else if (typeBrace)
                        return false;
                    else
                        return true;

                case "class":
                case "interface":
                    if (rootOrNamespace)
                        return ParseType();
                    else
                        return !typeBrace;

                case "import":
                    if (rootOrNamespace)
                        return ParseImport();
                    else
                        return !typeBrace;

                case "declare":
                case "export":
                case "private":
                case "protected":
                case "public":
                case "static":
                    if (rootOrNamespace || typeBrace)
                        modifiers.Push(text);
                    return true;

                case "function":
                    if (rootOrNamespace)
                        return ParseFunction();
                    else
                        return !typeBrace;

                case "let":
                case "var":
                    if (rootOrNamespace)
                        return ParseVar();
                    else
                        return !typeBrace;

                default:
                    if (typeBrace)
                        return ParseTypeMember();

                    return true;
            }
        }

        private bool Parse()
        {
            switch (type)
            {
                case TokenType.Identifier:
                    if (!ParseIdentifier())
                        return false;

                    break;

                case TokenType.Char:
                    if (!ParseChar())
                        return false;
                    break;
            }

            return true;
        }

        private bool ParseChar()
        {
            string s;
            switch (text)
            {
                case "}":
                    if (braceStack.Count == 0 ||
                        !braceStack.Peek().StartsWith("{"))
                    {
                        return false;
                    }

                    s = braceStack.Pop();
                    if (s == "{namespace")
                    {
                        if (namespaceStack.Count <= 1)
                            return false;

                        namespaceStack.Pop();
                    }
                    else if (s == "{type")
                    {
                        if (typeStack.Count == 0)
                            return false;

                        var type = typeStack.Pop();
                        ReportType(type);
                    }

                    modifiers.Clear();

                    return true;

                case ")":
                    if (braceStack.Count == 0 ||
                        !braceStack.Peek().StartsWith("("))
                    {
                        return false;
                    }

                    s = braceStack.Pop();
                    return true;

                case "]":
                    if (braceStack.Count == 0 ||
                        !braceStack.Peek().StartsWith("["))
                    {
                        return false;
                    }

                    s = braceStack.Pop();
                    return true;

                case "{":
                    braceStack.Push("{");
                    modifiers.Clear();
                    return true;

                case "(":
                    braceStack.Push("(");
                    return true;

                case "[":
                    braceStack.Push("[");
                    return true;

                case ";":
                    modifiers.Clear();
                    return true;
            }

            return true;
        }

        public class MemberInfo
        {
            public bool IsFunction;
            public string Name;
            public string Type;
            public string InitialValue;
            public bool IsOptional;
            public List<string> Modifiers;
            public List<DecoratorInfo> Decorators;
            public List<ArgumentInfo> Arguments;
        }

        public class ArgumentInfo
        {
            public string Name;
            public string Type;
            public string InitialValue;
            public bool IsOptional;
            public string Modifier;
            public List<DecoratorInfo> Decorators;
        }

        public class DecoratorInfo
        {
            public string Name;
            public string Arguments;
        }

        public class TypeInfo
        {
            public NamespaceInfo Namespace;
            public bool IsInterface;
            public string Name;
            public string Extends;
            public string Implements;
            public List<string> Modifiers;
            public List<MemberInfo> Members;
            public bool IsDeclaration;
            public List<DecoratorInfo> Decorators;

            public override string ToString()
            {
                return (Modifiers.Count > 0 ?
                    (String.Join(" ", Modifiers) + " ") : "") +
                    (IsInterface ? "interface " : "class ") +
                    (Namespace.Name.Length > 0 ? (Namespace.Name + ".") : "") +
                    Name +
                    (!Extends.IsEmptyOrNull() ? (" extends " + String.Join(", ", Extends)) : "") +
                    (!Implements.IsEmptyOrNull() ? (" implements " + String.Join(", ", Implements)) : "");
            }
        }

        public class NamespaceInfo
        {
            public bool IsDeclaration;
            public string Name;
            public Dictionary<string, string> Imports;
            public List<MemberInfo> Members;
        }
    }
}
#endif