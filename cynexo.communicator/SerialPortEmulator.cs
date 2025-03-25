using System;
using System.Threading;
using System.Threading.Tasks;

namespace Cynexo.Communicator;

/// <summary>
/// This class is used to emulate communication with the device via <see cref="CommPort"/>
/// </summary>
public class SerialPortEmulator : ISerialPort
{
    public bool IsOpen { get; private set; } = false;

    public void Open() { IsOpen = true; }

    public void Close()
    {
        IsOpen = false;
        Thread.Sleep(10);
    }

    public string ReadLine()
    {
        while (IsOpen)
        {
            Thread.Sleep(10);
            if (_response != null)
            {
                var response = _response;
                _response = null;
                return response;
            }
        }

        throw new Exception("Closed");
    }

    public void WriteLine(string text)
    {
        if (text.StartsWith("setVerbose"))
        {
            _response = "Verbose mode " + (text.Split(' ')[1] == "1" ? "ON" : "OFF");
        }
        else if (text.StartsWith("readFlow"))
        {
            _response = GenerateData();
        }
        else if (text.StartsWith("setFlow"))
        {
            _response = "--> setFlow";
            Task.Run(() => RespondToSetFlow(text.Split(' ')[1]));
        }
    }

    // Internal

    string? _response = null;

    private static string GenerateData()
    {
        return "Some fake data is here";
    }

    private static class Random
    {
        public static float Range(float pm) => (float)((_random.NextDouble() - 0.5) * 2 * pm);

        static readonly System.Random _random = new();
    }

    private async Task RespondToSetFlow(string parameters)
    {
        await Task.Delay(100);
        _response = $"parameters={parameters}";

        var channelCommands = parameters.Split(';');
        foreach (var cmd in channelCommands)
        {
            var p = cmd.Split(':');
            int id = int.Parse(p[0]);
            double flow = double.Parse(p[1]);
            var realFlow = flow + 0.4;

            await Task.Delay(100);
            _response = $"enable Channel {id}";

            await Task.Delay(100);
            _response = $"enable Valve {id}";

            await Task.Delay(100);
            _response = "recived command setFlow";

            await Task.Delay(100);
            _response = $"target = {flow:F2}";

            await Task.Delay(100);
            _response = $"measured = {flow + 0.4:F2}";

            await Task.Delay(100);
            _response = $"checking flow {id}";

            realFlow -= 0.15;

            await Task.Delay(100);
            _response = $"Target Flow {flow:F2}";

            await Task.Delay(500);
            _response = $"Measured Flow {realFlow:F2}";

            while (Math.Abs(realFlow - flow) >= 0.1)
            {
                realFlow -= 0.15;

                await Task.Delay(100);
                _response = $"Steps to move {Math.Abs(realFlow - flow) * 100:F0}";

                await Task.Delay(100);
                _response = "Closing Valve";

                await Task.Delay(100);
                _response = "moving motors";

                await Task.Delay(500);
                _response = "stop move";

                await Task.Delay(100);
                _response = $"Target Flow {flow:F2}";

                await Task.Delay(500);
                _response = $"Measured Flow {realFlow:F2}";
            }

            await Task.Delay(100);
            _response = "Value in range";

            await Task.Delay(100);
            _response = $"Checking Stability ch. {id}";
        }
    }
}
