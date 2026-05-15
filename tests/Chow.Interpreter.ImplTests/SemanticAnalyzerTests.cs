using Chow.Interpreter.Exceptions;
using Chow.Interpreter.SyntaxTrees;
using Chow.Interpreter.SyntaxTrees.Expressions;
using Chow.Interpreter.SyntaxTrees.Statements;

namespace Chow.Interpreter.ImplTests
{
    [TestFixture]
    public class SemanticAnalyzerTests
    {
        // ------------------------------------------------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------------------------------------------------

        static TreeRootNode Analyze(string source)
        {
            var tokens = new Scanner(source).ScanTokens();
            var root = (TreeRootNode)new Parser(tokens).BuildTree();
            new SemanticAnalyzer(root).Analyze();
            return root;
        }

        static FunctionNode FindFunction(TreeRootNode root, string name)
        {
            foreach (var stmt in root.Statements)
            {
                var found = FindFunctionIn(stmt, name);
                if (found != null)
                {
                    return found;
                }
            }
            Assert.Fail($"Function '{name}' not found.");
            return null!;
        }

        static FunctionNode? FindFunctionIn(Node node, string name)
        {
            switch (node)
            {
                case FunctionNode func when func.Name == name:
                    return func;
                case FunctionNode func:
                    return FindFunctionIn(func.Body, name);
                case BlockNode block:
                    foreach (var stmt in block.Statements)
                    {
                        var nested = FindFunctionIn(stmt, name);
                        if (nested != null)
                        {
                            return nested;
                        }
                    }
                    return null;
                default:
                    return null;
            }
        }

        static VarAssignNode FindAssign(Node node, string name)
        {
            switch (node)
            {
                case VarAssignNode v when v.Name == name:
                    return v;
                case BlockNode block:
                {
                    foreach (var stmt in block.Statements)
                    {
                        var nested = TryFindAssign(stmt, name);
                        if (nested != null)
                        {
                            return nested;
                        }
                    }
                    break;
                }
                case FunctionNode func:
                {
                    var nested = TryFindAssign(func.Body, name);
                    if (nested != null)
                    {
                        return nested;
                    }
                    break;
                }
                case IfNode ifNode:
                {
                    return TryFindAssign(ifNode.Block, name) ?? TryFindAssign(ifNode.Branch, name)!;
                }
            }
            Assert.Fail($"Assignment to '{name}' not found.");
            return null!;
        }

        static VarAssignNode? TryFindAssign(Node? node, string name)
        {
            if (node == null)
            {
                return null;
            }
            switch (node)
            {
                case VarAssignNode v when v.Name == name:
                    return v;
                case BlockNode block:
                    foreach (var stmt in block.Statements)
                    {
                        var nested = TryFindAssign(stmt, name);
                        if (nested != null)
                        {
                            return nested;
                        }
                    }
                    return null;
                case FunctionNode func:
                    return TryFindAssign(func.Body, name);
                case IfNode ifNode:
                    return TryFindAssign(ifNode.Block, name) ?? TryFindAssign(ifNode.Branch, name);
                default:
                    return null;
            }
        }

        static NameNode FindRead(Node node, string name)
        {
            var found = TryFindRead(node, name);
            if (found == null)
            {
                Assert.Fail($"Read of '{name}' not found.");
            }
            return found!;
        }

        static NameNode? TryFindRead(Node? node, string name)
        {
            if (node == null)
            {
                return null;
            }
            switch (node)
            {
                case NameNode n when n.Name == name:
                    return n;
                case BlockNode block:
                    foreach (var stmt in block.Statements)
                    {
                        var nested = TryFindRead(stmt, name);
                        if (nested != null)
                        {
                            return nested;
                        }
                    }
                    return null;
                case FunctionNode func:
                    return TryFindRead(func.Body, name);
                case VarAssignNode v:
                    return TryFindRead(v.Expression, name);
                case ExprStatementNode ex:
                    return TryFindRead(ex.Expression, name);
                case ExprNode e:
                    return TryFindRead(e.Left, name) ?? TryFindRead(e.Right, name);
                case ReturnNode r:
                    return TryFindRead(r.Expression, name);
                case CallNode call:
                {
                    var nested = TryFindRead(call.CallName, name);
                    if (nested != null)
                    {
                        return nested;
                    }
                    foreach (var arg in call.Args)
                    {
                        nested = TryFindRead(arg, name);
                        if (nested != null)
                        {
                            return nested;
                        }
                    }
                    return null;
                }
                case IfNode ifNode:
                    return TryFindRead(ifNode.Expr, name)
                        ?? TryFindRead(ifNode.Block, name)
                        ?? TryFindRead(ifNode.Branch, name);
                default:
                    return null;
            }
        }

        // ============================================================================================================
        // A. Valid resolution: stamps Local / Global / Nonlocal correctly
        // ============================================================================================================

        [Test]
        public void Analyze_ModuleLevelAssignment_StampedLocal()
        {
            var root = Analyze("x = 1");
            var assign = FindAssign(root.Statements[0], "x");
            Assert.That(assign.Resolution, Is.EqualTo(ScopeKind.Local));
        }

        [Test]
        public void Analyze_PlainFunctionAssignment_StampedLocal()
        {
            var root = Analyze("def f():\n    x = 1");
            var func = FindFunction(root, "f");
            var assign = FindAssign(func.Body, "x");
            Assert.That(assign.Resolution, Is.EqualTo(ScopeKind.Local));
        }

        [Test]
        public void Analyze_GlobalDecl_AssignmentInFunction_StampedGlobal()
        {
            var root = Analyze("x = 0\ndef f():\n    global x\n    x = 1");
            var func = FindFunction(root, "f");
            var assign = FindAssign(func.Body, "x");
            Assert.That(assign.Resolution, Is.EqualTo(ScopeKind.Global));
        }

        [Test]
        public void Analyze_GlobalDecl_ReadInFunction_StampedGlobal()
        {
            var root = Analyze("x = 0\ndef f():\n    global x\n    return x");
            var func = FindFunction(root, "f");
            var read = FindRead(func.Body, "x");
            Assert.That(read.Resolution, Is.EqualTo(ScopeKind.Global));
        }

        [Test]
        public void Analyze_NonlocalDecl_AssignmentInNestedFunction_StampedNonlocal()
        {
            var src =
                "def outer():\n" +
                "    x = 1\n" +
                "    def inner():\n" +
                "        nonlocal x\n" +
                "        x = 2";
            var root = Analyze(src);
            var inner = FindFunction(root, "inner");
            var assign = FindAssign(inner.Body, "x");
            Assert.That(assign.Resolution, Is.EqualTo(ScopeKind.Nonlocal));
        }

        [Test]
        public void Analyze_NonlocalDecl_ReadInNestedFunction_StampedNonlocal()
        {
            var src =
                "def outer():\n" +
                "    x = 1\n" +
                "    def inner():\n" +
                "        nonlocal x\n" +
                "        return x";
            var root = Analyze(src);
            var inner = FindFunction(root, "inner");
            var read = FindRead(inner.Body, "x");
            Assert.That(read.Resolution, Is.EqualTo(ScopeKind.Nonlocal));
        }

        [Test]
        public void Analyze_DefBinding_UnderGlobalDecl_StampedGlobal()
        {
            // `def foo()` is a binding for `foo`; under `global foo` it must target module scope.
            var root = Analyze("def outer():\n    global foo\n    def foo():\n        return 1");
            var outer = FindFunction(root, "outer");
            FunctionNode? fooDef = null;
            foreach (var stmt in ((BlockNode)outer.Body).Statements)
            {
                if (stmt is FunctionNode fn && fn.Name == "foo")
                {
                    fooDef = fn;
                    break;
                }
            }
            Assert.That(fooDef, Is.Not.Null);
            Assert.That(fooDef!.Resolution, Is.EqualTo(ScopeKind.Global));
        }

        [Test]
        public void Analyze_ModuleLevelGlobalDecl_IsNoopAndAllowed()
        {
            // Module-level `global x` is legal (matches CPython); subsequent assignment stays Local.
            var root = Analyze("global x\nx = 1");
            var assign = FindAssign(root.Statements[1], "x");
            // At module level, both Local and Global resolve to module scope; the analyzer stamps Global.
            Assert.That(assign.Resolution, Is.EqualTo(ScopeKind.Global));
        }

        // ============================================================================================================
        // B. Errors: nonlocal at module level
        // ============================================================================================================

        [Test]
        public void Analyze_NonlocalAtModuleLevel_ThrowsSemanticEx()
        {
            Assert.That(() => Analyze("nonlocal x"),
                Throws.TypeOf<SemanticEx>().With.Message.Contains("nonlocal declaration not allowed at module level"));
        }

        // ============================================================================================================
        // C. Errors: nonlocal with no enclosing binding
        // ============================================================================================================

        [Test]
        public void Analyze_NonlocalWithoutEnclosingBinding_ThrowsSemanticEx()
        {
            var src = "def f():\n    nonlocal x";
            Assert.That(() => Analyze(src),
                Throws.TypeOf<SemanticEx>().With.Message.Contains("no binding for nonlocal 'x' found"));
        }

        [Test]
        public void Analyze_NonlocalThatTargetsModule_StillErrors()
        {
            // Module-level x is not an enclosing FUNCTION binding; nonlocal must not see it.
            var src = "x = 1\ndef f():\n    nonlocal x";
            Assert.That(() => Analyze(src), Throws.TypeOf<SemanticEx>());
        }

        [Test]
        public void Analyze_NonlocalSkipsEnclosingGlobalDecl()
        {
            // outer declares x global, so outer does not bind x for nonlocal purposes.
            var src =
                "def outer():\n" +
                "    global x\n" +
                "    def inner():\n" +
                "        nonlocal x";
            Assert.That(() => Analyze(src),
                Throws.TypeOf<SemanticEx>().With.Message.Contains("no binding for nonlocal 'x' found"));
        }

        // ============================================================================================================
        // D. Errors: parameter conflicts
        // ============================================================================================================

        [Test]
        public void Analyze_GlobalDeclOnParameter_ThrowsSemanticEx()
        {
            var src = "def f(a):\n    global a";
            Assert.That(() => Analyze(src),
                Throws.TypeOf<SemanticEx>().With.Message.Contains("parameter and global"));
        }

        [Test]
        public void Analyze_NonlocalDeclOnParameter_ThrowsSemanticEx()
        {
            var src =
                "def outer():\n" +
                "    a = 1\n" +
                "    def inner(a):\n" +
                "        nonlocal a";
            Assert.That(() => Analyze(src),
                Throws.TypeOf<SemanticEx>().With.Message.Contains("parameter and nonlocal"));
        }

        // ============================================================================================================
        // E. Errors: global and nonlocal on the same name
        // ============================================================================================================

        [Test]
        public void Analyze_GlobalAndNonlocalSameName_ThrowsSemanticEx()
        {
            var src =
                "def outer():\n" +
                "    x = 1\n" +
                "    def inner():\n" +
                "        global x\n" +
                "        nonlocal x";
            Assert.That(() => Analyze(src),
                Throws.TypeOf<SemanticEx>().With.Message.Contains("nonlocal and global"));
        }

        // ============================================================================================================
        // F. Errors: use/assign before decl in same scope
        // ============================================================================================================

        [Test]
        public void Analyze_UseBeforeGlobalDecl_ThrowsSemanticEx()
        {
            var src =
                "x = 0\n" +
                "def f():\n" +
                "    y = x\n" +
                "    global x";
            Assert.That(() => Analyze(src),
                Throws.TypeOf<SemanticEx>().With.Message.Contains("used prior to global declaration"));
        }

        [Test]
        public void Analyze_AssignBeforeGlobalDecl_ThrowsSemanticEx()
        {
            var src =
                "def f():\n" +
                "    x = 1\n" +
                "    global x";
            Assert.That(() => Analyze(src),
                Throws.TypeOf<SemanticEx>().With.Message.Contains("assigned to before global declaration"));
        }

        [Test]
        public void Analyze_UseBeforeNonlocalDecl_ThrowsSemanticEx()
        {
            var src =
                "def outer():\n" +
                "    x = 1\n" +
                "    def inner():\n" +
                "        y = x\n" +
                "        nonlocal x";
            Assert.That(() => Analyze(src),
                Throws.TypeOf<SemanticEx>().With.Message.Contains("used prior to nonlocal declaration"));
        }

        // ============================================================================================================
        // G. Tolerance: duplicate decls of the same kind
        // ============================================================================================================

        [Test]
        public void Analyze_DuplicateGlobalDecl_IsTolerated()
        {
            var src =
                "x = 0\n" +
                "def f():\n" +
                "    global x\n" +
                "    global x\n" +
                "    x = 1";
            Assert.That(() => Analyze(src), Throws.Nothing);
        }
    }
}
