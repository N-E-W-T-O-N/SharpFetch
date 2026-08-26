using Spectre.Console;


AnsiConsole.MarkupLine("[bold green]Hello from Spectre.Console![/]");

var name = await AnsiConsole.AskAsync<string>("What's your name?");

AnsiConsole.MarkupLine($"[yellow]Welcome, {name}![/]");

AnsiConsole.Write(
    new Panel("[cyan]This is a simple Spectre.Console panel.[/]")
        .Header("Demo")
        .Border(BoxBorder.Rounded)
);