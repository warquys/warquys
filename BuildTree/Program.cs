using System.Diagnostics.CodeAnalysis;
using BuildTree;
using Spectre.Console;
using Spectre.Console.Cli;

var app = new CommandApp<NewTreeCommand>();

app.Configure(config =>
{
#if DEBUG
    config.PropagateExceptions();
    config.ValidateExamples();
#endif
    config.AddCommand<NewTreeCommand>("new");
    config.AddCommand<LoadTreeCommand>("load");
});

return app.Run(args);