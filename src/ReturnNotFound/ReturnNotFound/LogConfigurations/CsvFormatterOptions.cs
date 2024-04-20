using Microsoft.Extensions.Logging.Console;

namespace ReturnNotFound.LogConfigurations;

public class CsvFormatterOptions : ConsoleFormatterOptions
{
    public string ListSeparator { get; set; }
}