# VirtualMachine Performance Review

Findings are ordered by likely payoff. This review is based on `src/Chow/Evaluation/VirtualMachine.cs` and the nearby runtime types it calls.

1. Binary operations use `Func` delegates on the VM hot path

   Locations:
   - `src/Chow/Evaluation/VirtualMachine.cs:41`
   - `src/Chow/Evaluation/VirtualMachine.cs:335`

   Arithmetic and comparison opcodes call `ExecuteBinaryOperation((l, r) => ...)`. Even if non-capturing lambdas are cached, the delegate call blocks straightforward inlining and adds overhead to very common operations.

   Suggested modification: replace with direct per-op helpers or a helper that switches on `OperationCode`.

   Difficulty: low/medium.

   Expected save: high for arithmetic-heavy code.

2. Variable reads walk scopes twice

   Locations:
   - `src/Chow/Evaluation/VirtualMachine.cs:260`
   - `src/Chow/Evaluation/CallStack.cs:73`
   - `src/Chow/Evaluation/CallStack.cs:92`

   `PushVariableValue` first checks `IsVariableDefined`, then calls `GetVariableValue`, which repeats the same scope-chain walk. Inside each scope, `ContainsKey` plus indexer access can mean another duplicated dictionary lookup.

   Suggested modification: add `TryGetVariableValue(name, out value)` that walks once and uses `Dictionary.TryGetValue`.

   Difficulty: low.

   Expected save: high for variable-heavy loops.

3. The dispatch loop repeatedly re-fetches the current instruction

   Locations:
   - `src/Chow/Evaluation/VirtualMachine.cs:33`
   - `src/Chow/Evaluation/VirtualMachine.cs:35`
   - `src/Chow/Evaluation/VirtualMachine.cs:38`

   `CurrentOperation` routes through `_callStack.CurrentInstr`, which routes through current-frame lookup and chunk indexing. Many cases read `CurrentOperation.Code` and then `CurrentOperation.Operand`, doing that chain more than once per instruction.

   Suggested modification: cache `Instruction instr = _callStack.CurrentInstr` once per loop iteration and use `instr.Code` / `instr.Operand`.

   Difficulty: low.

   Expected save: medium.

4. Function calls allocate argument arrays every time

   Location:
   - `src/Chow/Evaluation/VirtualMachine.cs:287`

   `ExecuteCall` creates `new TaggedUnion[argCount]` for every call, including zero-arg calls. Closure calls then pop args into the array and push them back onto the value stack.

   Suggested modification: fast-path `argCount == 0`; longer-term, bind closure parameters directly during frame setup instead of re-pushing args.

   Difficulty: low for zero-arg fast path, medium/high for direct parameter binding.

   Expected save: medium/high for call-heavy code.

5. List literal construction copies through a temporary array

   Location:
   - `src/Chow/Evaluation/VirtualMachine.cs:368`

   `ExecuteBuildList` allocates a `TaggedUnion[]`, fills it, creates an `InternalList`, then copies values into that list.

   Suggested modification: add an `InternalList(int capacity)` constructor and fill the list directly in source order.

   Difficulty: medium.

   Expected save: medium for large or frequent list literals.

6. Dict literal construction copies through two temporary arrays

   Location:
   - `src/Chow/Evaluation/VirtualMachine.cs:388`

   `ExecuteBuildDict` allocates separate key and value arrays, then creates an `InternalDict` and inserts each pair.

   Suggested modification: add capacity support to `InternalDict` and avoid temporary arrays where possible.

   Difficulty: medium.

   Expected save: medium for large or frequent dict literals.

7. List membership pays extra indexer overhead

   Location:
   - `src/Chow/Evaluation/VirtualMachine.cs:410`

   `ExecuteIn` scans lists with `list[i]`, and the public list indexer normalizes and bounds-checks each index. For a forward scan from `0` to `Count - 1`, that normalization is unnecessary.

   Suggested modification: add an internal `Contains`/raw scan method on `InternalList`.

   Difficulty: low/medium.

   Expected save: medium for large list membership checks.

8. Attribute lookup repeats method-name dispatch

   Location:
   - `src/Chow/Evaluation/VirtualMachine.cs:513`

   `ExecuteGetAttr` calls `HasMethod(attrName)` and then `list[attrName]` or `dict[attrName]`, which performs another method-name switch through `GetMethod`.

   Suggested modification: replace with a single `TryGetMethod(attrName, out method)` style API.

   Difficulty: low.

   Expected save: medium for method-heavy code.

9. The value stack uses `Stack<TaggedUnion>`

   Location:
   - `src/Chow/Evaluation/VirtualMachine.cs:16`

   Almost every opcode calls `Push`, `Pop`, or `Peek`. `Stack<T>` is convenient but has general-purpose overhead.

   Suggested modification: replace with a small custom array-backed stack, ideally pre-sized based on chunk needs if stack-depth analysis is added later.

   Difficulty: medium.

   Expected save: medium/high, but this should be benchmarked before doing because it touches the whole VM.

