﻿using Serenity.TypeScript.TsParser;
using Serenity.TypeScript.TsTypes;
using System.Collections.Generic;

namespace Serenity.TypeScript
{
    public interface ITypeScriptAST
    {
        ScriptTarget ScriptTarget { get; set; }
        string SourceStr { get; set; }
        Node RootNode { get; set; }
        string GetTreeString(bool withPos);
        IEnumerable<Node> GetDescendants();
        void MakeAST(string source, string fileName = "fileName.ts", bool setChildren = true);
    }
    public class TypeScriptAST: ITypeScriptAST
    {
        public ScriptTarget ScriptTarget { get; set; } = ScriptTarget.Latest; //ES6
        public string SourceStr { get; set; }
        public Node RootNode { get; set; }

        public TypeScriptAST(string source = null, string fileName = "fileName.ts", bool setChildren = true, bool optimized = false)
        {
            if (source != null)
            {
                MakeAST(source, fileName, setChildren, optimized);
            }
        }

        public void MakeAST(string source, string fileName = "fileName.ts", bool setChildren = true)
        {
            MakeAST(source, fileName, setChildren, false);
        }

        public void MakeAST(string source, string fileName = "fileName.ts", bool setChildren = true, bool optimized = false)
        {
            SourceStr = source;
            var parser = new Parser();
            parser.Optimized = optimized;
            var sourceFile = parser.ParseSourceFile(fileName, source, ScriptTarget, null, false, ScriptKind.Ts);
            RootNode = sourceFile;
            RootNode.Ast = this;
            if (setChildren)
            {
                childrenMade = true;
                if (optimized)
                    RootNode.MakeChildrenOptimized(this);
                else
                    RootNode.MakeChildren(this);
            }
            //RootNode.GetDescendants().ToList().ForEach((n) => n.AST = this);
        }

        private bool childrenMade = false;
        public IEnumerable<Node> OfKind(SyntaxKind kind) => RootNode?.OfKind(kind);
        public IEnumerable<Node> GetDescendants()
        {
            if (!childrenMade && RootNode != null)
            {
                RootNode.MakeChildren(this);
                childrenMade = true;
            }
            return RootNode?.GetDescendants();
        }

        public string GetTreeString(bool withPos = true)
        {
            return RootNode?.GetTreeString(withPos);
        }
    }
}