using Chow;

namespace Chow.Repl;

class ReadEvalPrintLooper
{
       const string ExpressionStatementResult = "expr_result";
       const string ExitChowFunctionName = "exit";
       const string HelpChowFunctionName = "help";
       const string HelpPrintedText = """
                           =========================
                                     HELP
                           =========================		  
                           Key Bindings:
                           - Run Code: ESC
                           - Newline: ENTER
                           - Delete Character: BACKSPACE
                           - Cursor Down: UP ARROW
                           - Cursor Up: DOWN ARROW
                           - Cursor Left: LEFT ARROW
                           - Cursor Right: RIGHT ARROW
                           
                           REPL Chow Functions:
                           - help(): Prints help information
                           - exit(): Exits the application
                           """;
       
       readonly ChowScope _scope;

       public ReadEvalPrintLooper()
       {
              _scope = ChowObject.CreateScope();
              _scope[ExitChowFunctionName] = ChowObject.Create(new Action(() => Environment.Exit(0)));
              _scope[HelpChowFunctionName] = ChowObject.Create(PrintHelp);
       }

       public void Loop()
       {
              Console.Clear();
              
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

       public static void PrintHelp()
       {
              Console.WriteLine(HelpPrintedText);
       }
}
