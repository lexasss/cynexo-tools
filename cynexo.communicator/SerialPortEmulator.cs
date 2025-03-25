using System;
using System.Threading;
using System.Threading.Tasks;

namespace Cynexo.Communicator;

/// <summary>
/// Emulates communication with the device via <see cref="CommPort"/>
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

                //if (_isVerbose || _isResponseData)
                {
                    _isResponseData = false;
                    return response;
                }
            }
        }

        throw new Exception("Closed");
    }

    public void WriteLine(string text)
    {
        text = text.Trim();

        if (text.StartsWith("setVerbose"))
        {
            _isVerbose = int.Parse(text.Split(' ')[1]) == 1;
            _isResponseData = true;
            _response = _isVerbose ?
                "Verbose mode ON" :
                "recived command setVerbose";
        }
        else if (text.StartsWith("readFlow"))
        {
            _isResponseData = true;
            _response = $"Flow: {_flows[_currentChannelIndex]:F5}";
        }
        else if (text.StartsWith("setFlow"))
        {
            _response = "--> setFlow";
            _ = RespondToSetFlow(text.Split(' ')[1]);
        }
        else if (text.StartsWith("stopCalibration"))
        {
            _response = "recived command stopCalibration";
            _hasInterruptionRequest = true;
        }
        else if (text.StartsWith("setChannel"))
        {
            _response = "--> setChannel";
            RespondToSetChannel(text.Split(' ')[1]);
        }
        else if (text.StartsWith("setValve"))
        {
            _response = "--> setValve";
            RespondToSetValve(text.Split(' ')[1]);
        }
    }

    // Internal

    string? _response = null;

    bool _hasInterruptionRequest = false;

    readonly double[] _flows = new double[14] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

    int _currentChannelIndex = 0;
    bool _isVerbose = false;
    bool _isResponseData = false;   // set it to true if the response must be captured regardless of the verbosity status

    /*private static class Random
    {
        public static float Range(float pm) => (float)((_random.NextDouble() - 0.5) * 2 * pm);

        static readonly System.Random _random = new();
    }*/

    private async Task RespondToSetFlow(string parameters)
    {
        _hasInterruptionRequest = false;

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

            await Task.Delay(500);
            _response = $"measured = {realFlow:F2}";

            await Task.Delay(100);
            _response = $"checking flow {id}";

            while (!_hasInterruptionRequest)
            {
                realFlow -= 0.15;

                await Task.Delay(100);
                _response = $"Target Flow {flow:F2}";

                await Task.Delay(500);
                _response = $"Measured Flow {realFlow:F2}";

                if (Math.Abs(realFlow - flow) < 0.1)
                {
                    await Task.Delay(100);
                    _response = "Value in range";

                    break;
                }

                await Task.Delay(100);
                _response = $"Steps to move {Math.Abs(realFlow - flow) * 100:F0}";

                await Task.Delay(100);
                _response = "Closing Valve";

                await Task.Delay(100);
                _response = "moving motors";

                await Task.Delay(500);
                _response = "stop move";
            }

            await Task.Delay(100);
            _response = $"Checking Stability ch. {id}";

            await Task.Delay(100);
            _response = $"target: {flow}";

            _flows[id] = realFlow;

            if (_hasInterruptionRequest)
                break;
        }
    }

    private void RespondToSetChannel(string id)
    {
        _currentChannelIndex = int.Parse(id);

        Thread.Sleep(50);
        _response = $"channel={id}";
    }

    private void RespondToSetValve(string state)
    {
        Thread.Sleep(50);
        _response = $"valve state = {state}";

        Thread.Sleep(50);
        _response = state == "0" ? "Close" : "Open";
    }
}
