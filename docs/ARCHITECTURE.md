# Architecture

Chow uses a bytecode interpreter. At runtime, Chow source code is compiled into bytecode and executed using a stack-based virtual machine.

**Documentation last updated:** 6-30-2026

## ChowEngine
Path: [`..\src\Chow\Api\ChowEngine.cs`](../src/Chow/Api/ChowEngine.cs)

`ChowEngine` is a static class that provides the public API for evaluating Chow source code. It sets up the global scope—optionally seeding it with the language's built-in functions—and converts results between the internal value representation and the public types the host program sees. It delegates the actual *interpreter functionality* to the `Interpreter`, performing no compilation or execution itself.

## Interpreter
Path: [`..\src\Chow\Pipelines\Interpreter.cs`](../src/Chow/Pipelines/Interpreter.cs)

Orchestrates the two halves of the pipeline, compiling source code into bytecode and then executing it. It also provides a path for the host to invoke a Chow closure directly through the virtual machine.

### Interpreter: Source Code → Bytecode
Before a piece of Chow source code can be evaluated, it must be compiled into bytecode that the virtual machine can execute. The `CompilationPipeline` ([..\src\Chow\Pipelines\Compilation\CompilationPipeline.cs](../src/Chow/Pipelines/Compilation/CompilationPipeline.cs)) drives the following stages in order, passing each stage's output to the next.

- #### Scanner: [..\src\Chow\Pipelines\Compilation\Scanner.cs](../src/Chow/Pipelines/Compilation/Scanner.cs)
    Scanner performs lexical analysis, the first phase of the pipeline, consuming the raw Chow source code and producing a stream of tokens.

- #### Parser: [..\src\Chow\Pipelines\Compilation\Parser.cs](../src/Chow/Pipelines/Compilation/Parser.cs)
    Parser performs syntax analysis using recursive descent, consuming the Scanner's tokens and producing an abstract syntax tree that represents the source code's structure according to the language's grammar.

- #### SemanticAnalyzer: [..\src\Chow\Pipelines\Compilation\SemanticAnalyzer.cs](../src/Chow/Pipelines/Compilation/SemanticAnalyzer.cs)
    SemanticAnalyzer performs name resolution between parsing and compilation, walking the abstract syntax tree and annotating it in place so the Compiler can emit correct bytecode without doing scope analysis itself. It also validates Chow-compatible `global`/`nonlocal` scoping rules and reports any violations.

- #### Compiler: [..\src\Chow\Pipelines\Compilation\Compiler.cs](../src/Chow/Pipelines/Compilation/Compiler.cs)

    Compiler performs bytecode compilation, the final phase before execution, walking the annotated abstract syntax tree and emitting a chunk of bytecode the virtual machine can execute.

### Interpreter: Bytecode Execution
Once Chow's source code is converted into a bytecode chunk, its actual logic is executed by the virtual machine. The `VirtualMachine` ([..\src\Chow\Pipelines\Execution\VirtualMachine.cs](../src/Chow/Pipelines/Execution/VirtualMachine.cs)) is a thin entry point that runs a chunk through the `Processor`.

- It can also invoke a host-provided callable directly.

- #### Processor
    *Path:* [..\src\Chow\Pipelines\Execution\Processor.cs](../src/Chow/Pipelines/Execution/Processor.cs)

    `Processor` executes the primary business logic of the interpreter's virtual machine, running a bytecode chunk against a global scope using an operand stack and a call stack of frames, and returning the result of the last evaluated expression statement to the caller.

## Value Model

Chow keeps values in two parallel representations and converts between them only at the public boundary. Keeping the two straight is essential when working across the API/VM line.

### SourceValue
Path: [`..\src\Chow\SourceData\SourceValue.cs`](../src/Chow/SourceData/SourceValue.cs)

`SourceValue` is the internal runtime value—what the virtual machine pushes and pops. It is an immutable `readonly struct` with an explicit memory layout (a union over `object`, `long`, and `double`) tagged by a `DataType`, letting it hold any Chow type (`int`, `float`, `str`, `bool`, `None`, `list`, `dict`, `range`, and boxed .NET types). Object-like types (lists, dicts, ranges, functions, scopes) implement `ISourceObject` ([..\src\Chow\SourceData\Objects](../src/Chow/SourceData/Objects)) and are constructed through `SourceObjectFactory`.

### ChowObject
Path: [`..\src\Chow\Api\ChowObject.cs`](../src/Chow/Api/ChowObject.cs)

`ChowObject` (implementing `IChowObject`) is the public wrapper the host program sees. It wraps a single `SourceValue` and exposes host-friendly members—`Length`, an indexer, the `IsNone`/`IsList`/etc. flags, and the `Create`/`CreateList`/`CreateDictionary`/`CreateScope` factories—while keeping the internal `SourceValue` hidden from clients. `ChowScope` is the host-facing variable bag handed to `ChowEngine.Run`.

### ApiConverter
Path: [`..\src\Chow\Api\ApiConverter.cs`](../src/Chow/Api/ApiConverter.cs)

`ApiConverter` is the sole bridge between the two layers, translating `SourceValue` to `ChowObject` on the way out and back again on the way in. Conversion happens only at the boundary: internal `SourceValue`s are never exposed on the public surface, and `ChowObject`s never enter the virtual machine.

