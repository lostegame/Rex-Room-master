using System;
using System.Threading.Tasks;
using Figgle.Fonts;

bool noMenu = Array.Exists(args, a => string.Equals(a, "--no-menu", StringComparison.OrdinalIgnoreCase));

if (noMenu)
{
    Console.WriteLine("\n[System] Auto-starting APIServer (--no-menu)...");
    await Rex_Room_20230414.Server.StartAsync(args);
    return;
}

int selectedIndex = 0;
bool apiSelected = false;
bool wsSelected = false;

while (true)
{
    if (!Console.IsOutputRedirected) Console.Clear();
    
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("--------------------------------------------------");
    Console.WriteLine(FiggleFonts.Standard.Render("Rex Room 20230414"));
    Console.WriteLine("--------------------------------------------------");
    Console.ResetColor();
    Console.WriteLine(" Info: This server is powered by .NET 10 Kestrel.");
    Console.WriteLine(" Feel free to fork this project and add anything you want.");
    Console.WriteLine(" Made by lostegame (discord)!");
    Console.WriteLine("--------------------------------------------------");
    Console.WriteLine(" Use [UP/DOWN] to navigate, [ENTER] to toggle.");
    Console.WriteLine(" Press [ENTER] on 'Run Selected Servers' to start.");
    Console.WriteLine(" Press [ESC] to exit.\n");
    
    for (int i = 0; i < 3; i++)
    {
        if (i == selectedIndex)
        {
            Console.BackgroundColor = ConsoleColor.White;
            Console.ForegroundColor = ConsoleColor.Black;
        }
        else
        {
            Console.ResetColor();
        }

        if (i == 0)
        {
            string check = apiSelected ? "X" : " ";
            Console.WriteLine($"[{check}] Start APIServer");
        }
        else if (i == 1)
        {
            string check = wsSelected ? "X" : " ";
            Console.WriteLine($"[{check}] Start WebsocketServer");
        }
        else if (i == 2)
        {
            Console.WriteLine("Run Selected Servers");
        }
    }

    Console.ResetColor();

    var key = Console.ReadKey(intercept: true).Key;

    if (key == ConsoleKey.UpArrow)
    {
        selectedIndex--;
        if (selectedIndex < 0) selectedIndex = 2;
    }
    else if (key == ConsoleKey.DownArrow)
    {
        selectedIndex++;
        if (selectedIndex > 2) selectedIndex = 0;
    }
    else if (key == ConsoleKey.Enter)
    {
        if (selectedIndex == 0) apiSelected = !apiSelected;
        else if (selectedIndex == 1) wsSelected = !wsSelected;
        else if (selectedIndex == 2) break;
    }
    
    else if (key == ConsoleKey.Escape)
    {
        Console.WriteLine("\n[System] Shutting down...");
        return;
    }
}

Console.WriteLine("\n[System] Booting up selected servers...");

var serverTasks = new System.Collections.Generic.List<Task>();

if (apiSelected)
{
    serverTasks.Add(Rex_Room_20230414.Server.StartAsync(args));
}

if (wsSelected)
{
}

if (serverTasks.Count > 0)
{
    await Task.WhenAll(serverTasks);
}
else
{
    Console.WriteLine("[System] No servers were selected. Exiting.");
}