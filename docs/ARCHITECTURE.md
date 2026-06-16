# Architecture

Chow uses a bytecode interpreter. At runtime, Chow source code is compiled into bytecode and executed using a stack-based virtual machine.

## ChowEngine
Path: [..\src\Chow\ChowEngine.cs](../src/Chow/ChowEngine.cs)

A static class that provides a public API to evaluate Chow source code using the interpreter. ChowEngine is home to the *interpreter pipeline* that handles lexical analysis, syntax analysis, semantic analysis, bytecode compilation, and bytecode execution.

## Interpreter Pipeline

### Source Code → Bytecode
Before a piece of Chow source code can be evaluated, it must be compiled into bytecode that the virtual machine can execute. ChowEngine sequentially creates instances of the following:

- #### Scanner
    *Path:* [..\src\Chow\ChowEngine.cs](../src/Chow/ChowEngine.cs)
    
    Scanner performs lexical analysis for the interpreter. To perform lexical analysis at runtime, the ChowEngine creates a new Scanner instance and passes its constructor the Chow source code being evaluated. Then ChowEngine will call the Scanner instances' “tokenize” method, which will scan and evaluate the code provided to its constructor. Upon completion, the tokenize method will return a collection of tokens created using the source code.
        
- #### Parser
    *Path:* [..\src\Chow\Syntax\Parsing\Parser.cs](../src/Chow/Syntax/Parsing/Parser.cs)
    
    Parser performs syntax analysis for the interpreter. After lexical analysis is complete, the ChowEngine creates a new Parser instance and passes its constructor the collection of tokens produced by the Scanner. Then ChowEngine will call the Parser instances' "build AST" method, which analyzes the tokens provided to its constructor and determines how they fit together according to the language's grammar. Upon completion, the build AST method will return the root of an abstract syntax tree representing the source code's structure.

- #### SemanticAnalyzer
    *Path:* [..\src\Chow\Semantics\SemanticAnalyzer.cs](../src/Chow/Semantics/SemanticAnalyzer.cs)
    
    SemanticAnalyzer performs semantic analysis for the interpreter. After syntax analysis is complete, the ChowEngine creates a new SemanticAnalyzer instance and passes its constructor the abstract syntax tree produced by the Parser. Then ChowEngine will call the SemanticAnalyzer instances' "analyze" method, which walks the tree provided to its constructor, resolves names, validates the language's scoping rules, and throws an exception if any violation is found. Upon completion, the analyze method will have annotated the tree in place, allowing the Compiler to emit correct bytecode without performing scope analysis itself.
    
- #### Compiler
    *Path:* [..\src\Chow\Bytecode\Compilation\Compiler.cs](../src/Chow/Bytecode/Compilation/Compiler.cs)
    
    Compiler performs bytecode compilation for the interpreter. After semantic analysis is complete, the ChowEngine creates a new Compiler instance and passes its constructor the abstract syntax tree produced and annotated by the earlier stages. After, ChowEngine will call the Compiler instances' "compile root" method, which walks the tree provided to its constructor and emits the corresponding bytecode instructions. Upon completion, the compile root method will return a chunk of bytecode that the virtual machine can execute.

### Bytecode Execution
Once Chow's source code is converted into bytecode chunks, its actual logic will be executed by the virtual machine.

- #### InstructionProcessor
    *Path:* [..\src\Chow\VM\Processor.cs](../src/Chow/VM/Processor.cs)

    InstructionProcessor executes the primary business logic for the interpreter’s virtual machine. The ChowEngine creates a new InstructionProcessor instance and passes its constructor a scope (existing or new instance) and the chunk of bytecode produced by the Compiler. Then ChowEngine will call the InstructionProcessor instances' "execute" method, which runs the bytecode provided to its constructor. 

    During execution, the InstructionProcessor will sequentially invoke instruction operations, store and retrieve data, and manage function calls. Upon completion, the execute method will return the result of the last evaluated expression statement, which ChowEngine then returns to the caller.
