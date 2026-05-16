using System;
using System.Collections.Generic;
using Chow.Interpreter.Exceptions;
using Chow.Interpreter.SyntaxTrees;
using Chow.Interpreter.SyntaxTrees.Attributes;
using Chow.Interpreter.SyntaxTrees.Expressions;
using Chow.Interpreter.SyntaxTrees.Literals;
using Chow.Interpreter.SyntaxTrees.Scope;
using Chow.Interpreter.SyntaxTrees.Statements;
using Chow.Interpreter.SyntaxTrees.Subscripts;
namespace Chow.Interpreter
{
    /// <summary>
    /// Performs name-resolution between parsing and compilation. Walks the AST and stamps every
    /// name-bearing node (<see cref="NameNode"/>, <see cref="VariableAssignStatementNode"/>,
    /// <see cref="FunctionNode"/>) with a <see cref="ScopeType"/> so the
    /// <see cref="Compiler"/> can emit the correct opcode without performing scope analysis itself.
    /// <para>
    /// Validates Python-compatible <c>global</c>/<c>nonlocal</c> rules and raises
    /// <see cref="SemanticEx"/> on any violation. Each function scope is processed in two phases:
    /// a pre-scan that collects bindings, uses, and declarations; followed by an annotation pass
    /// that stamps <see cref="ScopeType"/> values and recursively analyzes nested function bodies.
    /// </para>
    /// </summary>
    sealed class SemanticAnalyzer
    {
        readonly TreeRootNode _root;
        readonly Stack<ScopeFrame> _scopes;

        #region Primary Methods

        public SemanticAnalyzer(Node root)
        {
            if (!(root is TreeRootNode treeRoot))
            {
                throw new InvalidOperationException("SemanticAnalyzer expects a TreeRootNode.");
            }

            _root = treeRoot;
            _scopes = new Stack<ScopeFrame>();
        }

        public void Analyze()
        {
            AnalyzeModule(_root);
        }

        #endregion

        #region Per-Scope Analysis

        void AnalyzeModule(TreeRootNode root)
        {
            var frame = ScopeFrame.NewModule();
            _scopes.Push(frame);

            foreach (var stmt in root.Statements)
            {
                PreScan(stmt);
            }

            ValidateScope();

            foreach (var stmt in root.Statements)
            {
                Annotate(stmt);
            }

            _scopes.Pop();
        }

        void AnalyzeFunction(FunctionNode funcNode)
        {
            var frame = ScopeFrame.NewFunction(funcNode);
            _scopes.Push(frame);

            PreScan(funcNode.Body);
            ValidateScope();
            Annotate(funcNode.Body);

            _scopes.Pop();
        }

        #endregion

        #region Pre-Scan Pass

        // Walks the current scope's AST and collects bindings, uses, and declarations into the
        // top scope frame. Nested function bodies are NOT recursed into here (their def-name
        // binding is recorded as a binding of the current scope, but their interior is opaque).
        void PreScan(Node node)
        {
            if (node == null)
            {
                return;
            }

            switch (node)
            {
                case BlockNode blockNode:
                {
                    foreach (var stmt in blockNode.Statements)
                    {
                        PreScan(stmt);
                    }

                    break;
                }

                case VariableAssignStatementNode varAssignNode:
                {
                    PreScan(varAssignNode.Expression);
                    RecordBinding(varAssignNode.Name, varAssignNode.LineNumber);
                    break;
                }

                case FunctionNode funcNode:
                {
                    // The def-name binding belongs to the current scope; the body is opaque to this pre-scan.
                    RecordBinding(funcNode.Name, funcNode.LineNumber);
                    break;
                }

                case NameNode nameNode:
                {
                    RecordUse(nameNode.Name, nameNode.LineNumber);
                    break;
                }

                case GlobalDeclarationNode globalNode:
                {
                    foreach (var name in globalNode.Names)
                    {
                        RecordGlobalDecl(name, globalNode.LineNumber);
                    }

                    break;
                }

                case NonlocalDeclarationNode nonlocalNode:
                {
                    foreach (var name in nonlocalNode.Names)
                    {
                        RecordNonlocalDecl(name, nonlocalNode.LineNumber);
                    }

                    break;
                }

                case IfStatementNode ifNode:
                {
                    PreScan(ifNode.Expression);
                    PreScan(ifNode.Block);
                    PreScan(ifNode.Branch);
                    break;
                }

                case BranchStatementNode branchNode:
                {
                    PreScan(branchNode.Expression);
                    PreScan(branchNode.Block);
                    PreScan(branchNode.Branch);
                    break;
                }

                case WhileStatementNode whileNode:
                {
                    PreScan(whileNode.Expression);
                    PreScan(whileNode.Block);
                    break;
                }

                case ForStatementNode forNode:
                {
                    PreScan(forNode.Iterable);
                    // Loop variable is an assignment target; record it as a binding in this scope.
                    RecordBinding(forNode.Target.Name, forNode.Target.LineNumber);
                    PreScan(forNode.Block);
                    PreScan(forNode.ElseBranch);
                    break;
                }

                case ReturnStatementNode returnNode:
                {
                    PreScan(returnNode.Expression);
                    break;
                }

                case ExpressionStatementNode exprStmtNode:
                {
                    PreScan(exprStmtNode.Expression);
                    break;
                }

                case ExpressionNode exprNode:
                {
                    PreScan(exprNode.Left);
                    PreScan(exprNode.Right);
                    break;
                }

                case CallNode callNode:
                {
                    PreScan(callNode.CallName);

                    foreach (var arg in callNode.Args)
                    {
                        PreScan(arg);
                    }

                    break;
                }

                case ListLiteralNode listNode:
                {
                    foreach (var element in listNode.Elements)
                    {
                        PreScan(element);
                    }

                    break;
                }

                case DictLiteralNode dictNode:
                {
                    for (var i = 0; i < dictNode.Keys.Count; i++)
                    {
                        PreScan(dictNode.Keys[i]);
                        PreScan(dictNode.Values[i]);
                    }

                    break;
                }

                case SubscriptNode subscriptNode:
                {
                    PreScan(subscriptNode.Target);
                    PreScan(subscriptNode.Index);
                    break;
                }

                case SubscriptSliceNode sliceNode:
                {
                    PreScan(sliceNode.Start);
                    PreScan(sliceNode.Stop);
                    PreScan(sliceNode.Step);
                    break;
                }

                case AttributeAccessNode attrAccessNode:
                {
                    PreScan(attrAccessNode.Target);
                    break;
                }

                case SubscriptAssignNode subscriptAssignNode:
                {
                    PreScan(subscriptAssignNode.Target);
                    PreScan(subscriptAssignNode.Index);
                    PreScan(subscriptAssignNode.Expression);
                    break;
                }

                case AttributeAssignNode attrAssignNode:
                {
                    PreScan(attrAssignNode.Target);
                    PreScan(attrAssignNode.Expression);
                    break;
                }

                case LiteralNode _:
                case BreakStatementNode _:
                case ContinueStatementNode _:
                {
                    break;
                }
            }
        }

        #endregion

        #region Annotation Pass

        // Walks the current scope's AST a second time and stamps Resolution on every name-bearing
        // node. Recurses into nested function bodies (each gets its own analyze cycle).
        void Annotate(Node node)
        {
            if (node == null)
            {
                return;
            }

            switch (node)
            {
                case BlockNode blockNode:
                {
                    foreach (var stmt in blockNode.Statements)
                    {
                        Annotate(stmt);
                    }

                    break;
                }

                case VariableAssignStatementNode varAssignNode:
                {
                    Annotate(varAssignNode.Expression);
                    varAssignNode.Resolution = ResolveName(varAssignNode.Name);
                    break;
                }

                case FunctionNode funcNode:
                {
                    funcNode.Resolution = ResolveName(funcNode.Name);
                    AnalyzeFunction(funcNode);
                    break;
                }

                case NameNode nameNode:
                {
                    nameNode.Resolution = ResolveName(nameNode.Name);
                    break;
                }

                case GlobalDeclarationNode _:
                case NonlocalDeclarationNode _:
                {
                    break;
                }

                case IfStatementNode ifNode:
                {
                    Annotate(ifNode.Expression);
                    Annotate(ifNode.Block);
                    Annotate(ifNode.Branch);
                    break;
                }

                case BranchStatementNode branchNode:
                {
                    Annotate(branchNode.Expression);
                    Annotate(branchNode.Block);
                    Annotate(branchNode.Branch);
                    break;
                }

                case WhileStatementNode whileNode:
                {
                    Annotate(whileNode.Expression);
                    Annotate(whileNode.Block);
                    break;
                }

                case ForStatementNode forNode:
                {
                    Annotate(forNode.Iterable);
                    forNode.Target.Resolution = ResolveName(forNode.Target.Name);
                    Annotate(forNode.Block);
                    Annotate(forNode.ElseBranch);
                    break;
                }

                case ReturnStatementNode returnNode:
                {
                    Annotate(returnNode.Expression);
                    break;
                }

                case ExpressionStatementNode exprStmtNode:
                {
                    Annotate(exprStmtNode.Expression);
                    break;
                }

                case ExpressionNode exprNode:
                {
                    Annotate(exprNode.Left);
                    Annotate(exprNode.Right);
                    break;
                }

                case CallNode callNode:
                {
                    Annotate(callNode.CallName);

                    foreach (var arg in callNode.Args)
                    {
                        Annotate(arg);
                    }

                    break;
                }

                case ListLiteralNode listNode:
                {
                    foreach (var element in listNode.Elements)
                    {
                        Annotate(element);
                    }

                    break;
                }

                case DictLiteralNode dictNode:
                {
                    for (var i = 0; i < dictNode.Keys.Count; i++)
                    {
                        Annotate(dictNode.Keys[i]);
                        Annotate(dictNode.Values[i]);
                    }

                    break;
                }

                case SubscriptNode subscriptNode:
                {
                    Annotate(subscriptNode.Target);
                    Annotate(subscriptNode.Index);
                    break;
                }

                case SubscriptSliceNode sliceNode:
                {
                    Annotate(sliceNode.Start);
                    Annotate(sliceNode.Stop);
                    Annotate(sliceNode.Step);
                    break;
                }

                case AttributeAccessNode attrAccessNode:
                {
                    Annotate(attrAccessNode.Target);
                    break;
                }

                case SubscriptAssignNode subscriptAssignNode:
                {
                    Annotate(subscriptAssignNode.Target);
                    Annotate(subscriptAssignNode.Index);
                    Annotate(subscriptAssignNode.Expression);
                    break;
                }

                case AttributeAssignNode attrAssignNode:
                {
                    Annotate(attrAssignNode.Target);
                    Annotate(attrAssignNode.Expression);
                    break;
                }

                case LiteralNode _:
                case BreakStatementNode _:
                case ContinueStatementNode _:
                {
                    break;
                }
            }
        }

        #endregion

        #region Bookkeeping

        ScopeFrame Current => _scopes.Peek();

        void RecordBinding(string name, int line)
        {
            if (!Current.Bindings.ContainsKey(name))
            {
                Current.Bindings[name] = line;
            }
        }

        void RecordUse(string name, int line)
        {
            if (!Current.Uses.ContainsKey(name))
            {
                Current.Uses[name] = line;
            }
        }

        void RecordGlobalDecl(string name, int line)
        {
            if (!Current.GlobalDeclarations.ContainsKey(name))
            {
                Current.GlobalDeclarations[name] = line;
            }
        }

        void RecordNonlocalDecl(string name, int line)
        {
            if (!Current.NonlocalDeclarations.ContainsKey(name))
            {
                Current.NonlocalDeclarations[name] = line;
            }
        }

        #endregion

        #region Validation

        void ValidateScope()
        {
            var frame = Current;

            foreach (var pair in frame.GlobalDeclarations)
            {
                ValidateGlobalDeclaration(frame, pair.Key, pair.Value);
            }

            foreach (var pair in frame.NonlocalDeclarations)
            {
                ValidateNonlocalDeclaration(frame, pair.Key, pair.Value);
            }
        }

        void ValidateGlobalDeclaration(ScopeFrame frame, string name, int declareLine)
        {
            if (frame.Parameters.Contains(name))
            {
                throw new SemanticEx($"name '{name}' is parameter and global", declareLine);
            }

            if (frame.NonlocalDeclarations.ContainsKey(name))
            {
                throw new SemanticEx($"name '{name}' is nonlocal and global", declareLine);
            }

            if (frame.Bindings.TryGetValue(name, out var bindLine) && bindLine < declareLine)
            {
                throw new SemanticEx($"name '{name}' is assigned to before global declaration", declareLine);
            }

            if (frame.Uses.TryGetValue(name, out var useLine) && useLine < declareLine)
            {
                throw new SemanticEx($"name '{name}' is used prior to global declaration", declareLine);
            }
        }

        void ValidateNonlocalDeclaration(ScopeFrame frame, string name, int declareLine)
        {
            if (frame.IsModule)
            {
                throw new SemanticEx("nonlocal declaration not allowed at module level", declareLine);
            }

            if (frame.Parameters.Contains(name))
            {
                throw new SemanticEx($"name '{name}' is parameter and nonlocal", declareLine);
            }

            if (frame.GlobalDeclarations.ContainsKey(name))
            {
                throw new SemanticEx($"name '{name}' is nonlocal and global", declareLine);
            }

            if (frame.Bindings.TryGetValue(name, out var bindLine) && bindLine < declareLine)
            {
                throw new SemanticEx($"name '{name}' is assigned to before nonlocal declaration", declareLine);
            }

            if (frame.Uses.TryGetValue(name, out var useLine) && useLine < declareLine)
            {
                throw new SemanticEx($"name '{name}' is used prior to nonlocal declaration", declareLine);
            }

            if (!HasEnclosingFunctionBinding(name))
            {
                throw new SemanticEx($"no binding for nonlocal '{name}' found", declareLine);
            }
        }

        // Walks the scope stack, skipping the current scope and the module scope, and returns true
        // iff some enclosing function scope binds the name (param, assignment/def, or its own nonlocal).
        // A `global` declaration in an enclosing scope removes that scope from consideration.
        bool HasEnclosingFunctionBinding(string name)
        {
            var skippedCurrent = false;

            foreach (var scope in _scopes)
            {
                if (!skippedCurrent)
                {
                    skippedCurrent = true;
                    continue;
                }

                if (scope.IsModule)
                {
                    return false;
                }

                if (scope.GlobalDeclarations.ContainsKey(name))
                {
                    continue;
                }

                if (scope.Parameters.Contains(name) || scope.Bindings.ContainsKey(name) || scope.NonlocalDeclarations.ContainsKey(name))
                {
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region Resolution

        ScopeType ResolveName(string name)
        {
            var frame = Current;

            if (frame.GlobalDeclarations.ContainsKey(name))
            {
                return ScopeType.Global;
            }

            if (frame.NonlocalDeclarations.ContainsKey(name))
            {
                return ScopeType.Nonlocal;
            }

            return ScopeType.Local;
        }

        #endregion

        sealed class ScopeFrame
        {
            public bool IsModule { get; }
            public HashSet<string> Parameters { get; }
            public Dictionary<string, int> Bindings { get; }
            public Dictionary<string, int> Uses { get; }
            public Dictionary<string, int> GlobalDeclarations { get; }
            public Dictionary<string, int> NonlocalDeclarations { get; }

            ScopeFrame(bool isModule, HashSet<string> parameters)
            {
                IsModule = isModule;
                Parameters = parameters;
                Bindings = new Dictionary<string, int>();
                Uses = new Dictionary<string, int>();
                GlobalDeclarations = new Dictionary<string, int>();
                NonlocalDeclarations = new Dictionary<string, int>();
            }

            public static ScopeFrame NewModule()
            {
                return new ScopeFrame(true, new HashSet<string>());
            }

            public static ScopeFrame NewFunction(FunctionNode funcNode)
            {
                var parameters = new HashSet<string>();

                foreach (var param in funcNode.Params)
                {
                    if (param is NameNode nameNode)
                    {
                        parameters.Add(nameNode.Name);
                    }
                }

                return new ScopeFrame(false, parameters);
            }
        }
    }
}
