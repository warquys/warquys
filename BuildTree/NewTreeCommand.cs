using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Spectre.Console;
using Spectre.Console.Cli;

namespace BuildTree;

internal class NewTreeCommand : Command<NewTreeCommand.Setting>
{
    public override int Execute(CommandContext context, Setting settings)
    {
        settings.RootName ??= AnsiConsole.Ask<string>("What is the root name of the tree ?");
        settings.FilePath ??= AnsiConsole.Ask<string>("Where do you want to save the file ?");
        if (".xml" != Path.GetExtension(settings.FilePath))
        {
            settings.FilePath += ".xml";
        }
        if (File.Exists(settings.FilePath))
        {
            if (!AnsiConsole.Confirm("This file already exists, do you want to overwrite it ?", false))
            {
                AnsiConsole.WriteLine("If you want to open it you can do it with the load command.");
                AnsiConsole.WriteLine($"BuildTree Edit {settings.FilePath}");
                return 0;
            }
        }
        var editor = new TreeEditer(settings.RootName, settings.FilePath);
        editor.StartEdition();

        return 0;
    }

    public class Setting : CommandSettings
    {
        [Description("The root node Name")]
        [CommandArgument(0, "[rootName]")]
        public string? RootName { get; set; }

        [Description("The root node Name")]
        [CommandArgument(1, "[filePath]")]
        public string? FilePath { get; set; }
    }
}