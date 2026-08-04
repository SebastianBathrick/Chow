# Architecture

Chow uses a bytecode interpreter. At runtime, Chow source code is compiled into bytecode and executed using a stack-based virtual machine.

**Documentation last updated:** 7-13-2026

## ChowEngine
Path: [`..\src\Chow\Api\ChowEngine.cs`](../src/Chow/Api/ChowEngine.cs)

`ChowEngine` is a static class that provides the public API for evaluating Chow source code. It sets up the global scope—optionally seeding it with the language's built-in functions—and converts results between the internal value representation and the public types the host program sees. It delegates the actual *interpreter functionality* to the `VirtualMachine`, performing no compilation or execution itself.

## VirtualMachine
Path: [`..\src\Chow\Interpreter\VirtualMachine.cs`](../src/Chow/Interpreter/VirtualMachine.cs)

Orchestrates the two halves of the pipeline, compiling source code into bytecode and then executing it. It also provides a path for the host to invoke a Chow closure directly.

### VirtualMachine: Source Code → Bytecode
Before a piece of Chow source code can be evaluated, it must be compiled into bytecode that the virtual machine can execute. The `BytecodeConverter` ([..\src\Chow\Interpreter\BytecodeConverter.cs](../src/Chow/Interpreter/BytecodeConverter.cs)) drives the following stages in order, passing each stage's output to the next.

- #### Scanner: [..\src\Chow\Interpreter\Lexing\Scanner.cs](../src/Chow/Interpreter/Lexing/Scanner.cs)
    Scanner performs lexical analysis, the first phase of the pipeline, consuming the raw Chow source code and producing a stream of tokens.

- #### Parser: [..\src\Chow\Interpreter\Syntax\Parser.cs](../src/Chow/Interpreter/Syntax/Parser.cs)
    Parser performs syntax analysis using recursive descent, consuming the Scanner's tokens and producing an abstract syntax tree that represents the source code's structure according to the language's grammar.

- #### SemanticAnalyzer: [..\src\Chow\Interpreter\Semantics\SemanticAnalyzer.cs](../src/Chow/Interpreter/Semantics/SemanticAnalyzer.cs)
    SemanticAnalyzer performs name resolution between parsing and compilation, walking the abstract syntax tree and annotating it in place so the Compiler can emit correct bytecode without doing scope analysis itself. It also validates Chow-compatible `global`/`nonlocal` scoping rules and reports any violations.

- #### Compiler: [..\src\Chow\Interpreter\Compilation\Compiler.cs](../src/Chow/Interpreter/Compilation/Compiler.cs)

    Compiler performs bytecode compilation, the final phase before execution, walking the annotated abstract syntax tree and emitting a chunk of bytecode the virtual machine can execute.

### VirtualMachine: Bytecode Execution
Once Chow's source code is converted into a bytecode chunk, the `VirtualMachine` executes its actual logic by running the chunk through the `Processor`.

- It can also invoke a single callable on its own, without a chunk around it, which is how the host reaches into Chow through `ChowObject.Call`. A host-provided delegate is invoked directly; a Chow callable—a closure, a bound method, or a class—is run by a `Processor` through `CallValue`, which applies the same call rules compiled code goes through. Since a call made from the host has no surrounding frame to inherit a module scope from, it is recovered by walking the callable's captured scope chain to its root, so `global` inside the body still resolves.

- #### Processor
    *Path:* [..\src\Chow\Interpreter\VM\Processor.cs](../src/Chow/Interpreter/VM/Processor.cs)

    `Processor` executes the primary business logic of the interpreter's virtual machine, running a bytecode chunk against a global scope using an operand stack and a call stack of frames, and returning the result of the last evaluated expression statement to the caller.

## Value Model

Chow keeps values in two parallel representations and converts between them only at the public boundary. Keeping the two straight is essential when working across the API/VM line.

### SourceValue
Path: [`..\src\Chow\SourceData\SourceValue.cs`](../src/Chow/SourceData/SourceValue.cs)

`SourceValue` is the internal runtime value—what the virtual machine pushes and pops. It is an immutable `readonly struct` with an explicit memory layout (a union over `object`, `long`, and `double`) tagged by a `DataType`, letting it hold any Chow type (`int`, `float`, `str`, `bool`, `None`, `list`, `dict`, `range`, and boxed .NET types). Object-like types (lists, dicts, ranges, functions, scopes, classes, and class instances) implement `ISourceObject` ([..\src\Chow\SourceData\SourceObjects](../src/Chow/SourceData/SourceObjects)) and are constructed through `SourceObjectFactory`—except those carrying constructor state (ranges, functions, slices, classes, and instances), which are built directly.

### ChowObject
Path: [`..\src\Chow\Api\ChowObject.cs`](../src/Chow/Api/ChowObject.cs)

`ChowObject` (implementing `IChowObject`) is the public wrapper the host program sees. It wraps a single `SourceValue` and exposes host-friendly members—`Length`, an indexer, the `IsNone`/`IsList`/etc. flags, and the `Create`/`CreateList`/`CreateDictionary`/`CreateScope` factories—while keeping the internal `SourceValue` hidden from clients. `ChowScope` is the host-facing variable bag handed to `ChowEngine.Run`.

### ApiConverter
Path: [`..\src\Chow\Api\ApiConverter.cs`](../src/Chow/Api/ApiConverter.cs)

`ApiConverter` is the sole bridge between the two layers, translating `SourceValue` to `ChowObject` on the way out and back again on the way in. Conversion happens only at the boundary: internal `SourceValue`s are never exposed on the public surface, and `ChowObject`s never enter the virtual machine.

## Classes

Classes reuse the machinery that already builds functions, and are worth understanding as a pair with it.

A `def` compiles to a `FunctionDefinition` ([..\src\Chow\Bytecode\FunctionDefinition.cs](../src/Chow/Bytecode/FunctionDefinition.cs)) stored as a chunk constant; the `PushNewSourceFunction` op combines that template with the scope active at declaration time to produce a `SourceFunction`. A `class` works the same way: it compiles to a `ClassDefinition` ([..\src\Chow\Bytecode\ClassDefinition.cs](../src/Chow/Bytecode/ClassDefinition.cs)) holding one `FunctionDefinition` per method, and the `PushNewSourceClass` op turns each into a closure over the *declaring* scope before assembling a `SourceClass`. The class body is therefore never executed as a frame, which is what makes a method resolve names against the scope the class was declared in rather than against the class—matching Python, where a method cannot see class-level names without going through `self` or the class. Class-level variables are the one part evaluated at declaration time: the Compiler emits their initializers into the enclosing chunk and `PushNewSourceClass` pops the resulting values, the same push-N-then-build shape used by list and dict literals.

Methods and class variables share one attribute table on `SourceClass`, since a method is just an attribute holding a `SourceFunction`. Reading an attribute off a `SourceClassInstance` checks the instance's own fields first and then falls through to the class; a function found there is bound to the receiver via `SourceFunction.Bind`, which returns a copy of the closure carrying the instance. The VM pushes that receiver ahead of the call's arguments so it lands in the first declared parameter (`self`), and expects one fewer argument at the call site to account for it.

Construction is the one place the call protocol differs. Calling a class allocates a `SourceClassInstance`, and—when the class declares `__init__`—enters that method's frame with the instance recorded on the frame as its construction result. `__init__` returns None like any other function, so on return the Processor discards that value and pushes the instance, which is what makes `Point(1, 2)` evaluate to the new object.

