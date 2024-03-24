using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace BuildTree;

internal class LoadTreeCommand : Command<LoadTreeCommand.Setting>
{
    public override int Execute(CommandContext context, Setting settings)
    {
        if (".xml" != Path.GetExtension(settings.FilePath))
        {
            settings.FilePath += ".xml";
        }
        if (!File.Exists(settings.FilePath))
        {
            AnsiConsole.WriteLine("This file do not existe.");
            AnsiConsole.WriteLine($"{settings.FilePath}");
            return 0;
        }
        var editor = new TreeEditer(settings.FilePath);
        editor.StartEdition();

        return 0;
    }

    public class Setting : CommandSettings
    {
        [Description("The file to load")]
        [CommandArgument(0, "<filePath>")]
        public string? FilePath { get; set; }
    }
}
