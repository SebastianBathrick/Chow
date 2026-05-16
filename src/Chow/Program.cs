using Chow.Cli;
using Chow.Execution;
var executor = new ChowModuleExecutor();
var app = new CliApp(executor);

return app.Run(args);
