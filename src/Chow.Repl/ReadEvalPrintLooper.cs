using Chow;

class ReadEvalPrintLooper
{
       const string ExpressionStatementResult = "expr_result";
       const string ExitChowName = "exit";
       
       readonly ChowScope _scope;

       public ReadEvalPrintLooper(ChowObject? scope = null)
       {
              _scope = scope ?? ChowObject.CreateScope();
              _scope[ExitChowName] = ChowObject.Create(new Action(() => Environment.Exit(0)));
       }

       public void Loop()
       {
              while (true)
              {
                     try
                     {
                            var editor = ChowEditorFactory.CreateEditor();
                            var srcCode = editor.GetTextSubmission();
                            var output = Evaluate(srcCode);
                            Console.WriteLine($"\n{output}");
                     }          
                     catch (Exception ex)
                     {
                            Console.WriteLine($"EXCEPTION THROWN:\n{ex.Message}");
                     }

                     Console.WriteLine($"\nPress any key to continue...");
                     Console.ReadKey(true);
              }
}

       ChowObject Evaluate(string srcCode)
       {
              ChowEngine.Run(srcCode, _scope);
              return _scope[ExpressionStatementResult];
       }
}
