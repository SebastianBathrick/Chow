using Chow;

class ReadEvalPrintLooper
{
       const string ExpressionStatementResult = "expr_result";
       const string ExitChowName = "exit";
       
       readonly ICodeEditor _editor;
       readonly ChowScope _scope;

       public ReadEvalPrintLooper(ICodeEditor editor, ChowObject? scope = null)
       {
              _editor = editor;
              _scope = scope ?? ChowObject.CreateScope();
              _scope[ExitChowName] = ChowObject.Create(new Action(() => Environment.Exit(0)));
       }

       public bool TryLoop()
       {
              try
              {
                     Loop();
                     return true;
              }
              catch (Exception ex)
              {
                     Console.WriteLine($"EXCEPTION THROWN:\n{ex}");
                     return false;
              }
       }

       void Loop()
       {
              // Continues until the exit() function is evaluated by the Chow VM
              while (true)
              {
                     var srcCode = _editor.GetTextSubmission();
                     var output = Evaluate(srcCode);
                     
                     Console.WriteLine($"\nOutput:\n{output.ToString()}");
                     Console.WriteLine($"\nPress any key to continue...");
                     Console.Read();
              }
       }

       ChowObject Evaluate(string srcCode)
       {
              ChowEngine.Run(srcCode, _scope);
              return _scope[ExpressionStatementResult];
       }
}
