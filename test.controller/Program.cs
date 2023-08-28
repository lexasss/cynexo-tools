//#define SHOW_PORT_DEBUG

using Cynexo.Controller;
using System.Windows.Threading;

Console.Title = "Cynexo";
Console.WriteLine("Testing Cynexo Sniff-0 controller module (Cynexo.Controller)...\n");

var commands = new Dictionary<string, (string, string?)>()
{
    { "v", ("set verbose = TRUE", Command.SetVerbose(true)) },
    { "1", ("set channel = 1", Command.SetChannel(1)) },
    { "o", ("set valve = OPENED", Command.SetValve(true)) },
    { "c", ("set valve = CLOSED", Command.SetValve(false)) },
    { "v500", ("open valve for 500 ms", Command.OpenValve(500)) },
    { "r", ("set verbose = TRUE", Command.ReadFlow()) },
    { "oa", ("open all valves", Command.SetAllSolenoidValves(true)) },
    { "ca", ("close all valves", Command.SetAllSolenoidValves(false)) },
    { "do", ("set stepper motor to direction = OPEN", Command.SetMotorDirection(true)) },
    { "dc", ("set stepper motor to direction = OPEN", Command.SetMotorDirection(false)) },
    { "s", ("run stepper motor 5 steps", Command.RunMotorSteps(5)) },
    { "h", ("displays available commands", null) },
    { "e", ("exits the app", null) }
};


// COM port listener

COMUtils _com = new();
_com.Inserted += (s, e) => Dispatcher.CurrentDispatcher.Invoke(() =>
{
    Console.WriteLine($"[COM] port '{e.Name}' is available now ({e.Description}, by {e.Manufacturer})");
});

Console.WriteLine("Available ports:");
var ports = COMUtils.Ports;
if (ports.Length == 0)
{
    Console.WriteLine("  none");
}
else foreach (var port in ports)
    {
        Console.WriteLine($"  {port.Name} ({port.Description}; {port.Manufacturer})");
    }
Console.WriteLine("");


// Open a COM port or start a simulator`

var _port = new CommPort();
_port.Opened += (s, e) => Console.WriteLine("[PORT] opened");
_port.Closed += (s, e) => Console.WriteLine("[PORT] closed");
_port.Data += async (s, e) => await Task.Run(() => PrintData(e));
_port.COMError += (s, e) => Console.WriteLine($"[PORT] {e}");

#if SHOW_PORT_DEBUG
_port.Debug += (s, e) => PrintDebug(e);
#endif

do
{
    Console.WriteLine("Enter COM port number, or leave it blank to start simulation:");
    Console.Write("  COM");

    var com = Console.ReadLine() ?? "";
    if (!string.IsNullOrEmpty(com))
        com = "COM" + com;

    var openResult = _port.Open(com);

    Console.WriteLine($"Result: {openResult}\n");

    if (openResult.Error == Error.Success)
        break;

} while (true);


// Execute commands

PrintHelp();

while (true)
{
    Console.Write("\nCommand: ");
    var cmd = Console.ReadLine() ?? "";
    if (!HandleCommand(cmd))
    {
        break;
    }
}


// Exit

_port.Close();

Console.WriteLine("\nTesting finished.");


bool HandleCommand(string cmd)
{
    if (!commands.TryGetValue(cmd, out var requestDesc))
    {
        Console.WriteLine("Unknown command");
        return true;
    }

    var request = requestDesc.Item2;
    if (request == null)
    {
        if (cmd == "h")
        {
            PrintHelp();
            return true;
        }
        else
        {
            return false;
        }
    }

    var result = _port.Send(request);

    Console.WriteLine($"Sent:     {request}");
    Console.WriteLine("  " + result.Reason);

    return true;
}

void PrintData(object e)
{
    var line = Console.CursorTop;
    if (Console.CursorLeft > 0)
    {
        Console.WriteLine("\n");
        Console.CursorTop -= 2;
    }
    Console.WriteLine("Received: " + e);
    Console.Write("\nCommand: ");
}

void PrintHelp()
{
    Console.WriteLine("Available commands:");
    foreach (var cmd in commands)
    {
        if (!string.IsNullOrEmpty(cmd.Key))
        {
            Console.WriteLine($"    {cmd.Key,-8} - {cmd.Value.Item1}");
        }
    }
}

#if SHOW_PORT_DEBUG
void PrintDebug(string str)
{
    if (Console.CursorLeft > 0)
        Console.WriteLine("");
    Console.WriteLine($"[PORT] debug: {str}");
}
#endif