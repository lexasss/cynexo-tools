using System;
using System.Linq;
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
        var p = text.Split(' ');
        var cmd = p[0];
        var arg = p.Length > 1 ? p[1] : "";

        if (text.StartsWith("setVerbose"))
        {
            _isVerbose = int.Parse(arg) == 1;
            _isResponseData = true;
            _response = _isVerbose ?
                "Verbose mode ON" :
                "recived command setVerbose";
        }
        else if (text.StartsWith("readFlow"))
        {
            _isResponseData = true;
            var flow = _channels.Sum(ch => ch.IsOpen ? ch.Flow : 0);
            _response = $"Flow:  {flow:F5}";
        }
        else if (text.StartsWith("stopCalibration"))
        {
            _response = "recived command stopCalibration";
            _hasInterruptionRequest = true;
        }
        else
        {
            _response = $"--> {cmd}";
            if (text.StartsWith("setFlow"))
            {
                _ = RespondToSetFlow(arg);
            }
            else if (text.StartsWith("setChannel"))
            {
                RespondToSetChannel(arg);
            }
            else if (text.StartsWith("setValve"))
            {
                RespondToSetValve(arg);
            }
            else if (text.StartsWith("openValve"))
            {
                RespondToOpenValve(arg);
            }
            else if (text.StartsWith("setDirection"))
            {
                RespondToSetDirection(arg);
            }
            else if (text.StartsWith("steps"))
            {
                RespondToRunMotor(arg);
            }
        }
    }

    // Internal

    class Channel(int id, bool isOpen = false, double flow = 0)
    {
        public int ID => id;
        public bool IsOpen { get; set; } = isOpen;
        public double Flow { get; set; } = flow;
    }

    readonly Random _rnd = new Random((int)DateTime.Now.Ticks);
    readonly Channel[] _channels = Enumerable.Range(0, 14).Select(i => new Channel(i)).ToArray();

    string? _response = null;

    bool _hasInterruptionRequest = false;

    int _currentChannelIndex = 0;
    bool _isVerbose = false;
    bool _isMotorDirectionToOpen = true;
    bool _isResponseData = false;   // set it to true if the response must be captured regardless of the verbosity status

    //float Range(float scale) => (float)((_random.NextDouble() - 0.5) * 2 * scale);

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
            var realFlow = flow + _rnd.NextDouble() * 0.4;

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
                if (Math.Abs(realFlow - flow) >= 0.1)
                {
                    realFlow -= 0.15;

                    await Task.Delay(100);
                    _response = $"Target Flow {flow:F2}";

                    await Task.Delay(500);
                    _response = $"Measured Flow {realFlow:F2}";
                }

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

            _channels[id].Flow = realFlow;

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
        bool isOpen = state == "1";

        _channels[_currentChannelIndex].IsOpen = isOpen;

        Thread.Sleep(50);
        _response = $"valve state = {state}";

        Thread.Sleep(50);
        _response = state == "0" ? "Close" : "Open";
    }

    private void RespondToOpenValve(string ms)
    {
        Thread.Sleep(50);
        _response = $"open Valve Timed = {ms}";

        if (int.TryParse(ms, out int interval))
        {
            _channels[_currentChannelIndex].IsOpen = true;

            Thread.Sleep(50);
            _response = "Open";

            Thread.Sleep(interval);
            _response = "Close";

            _channels[_currentChannelIndex].IsOpen = false;
        }
    }

    private void RespondToSetDirection(string direction)
    {
        _isMotorDirectionToOpen = direction == "1";

        Thread.Sleep(50);
        _response = $"direction{direction}";
    }

    private void RespondToRunMotor(string steps)
    {
        if (int.TryParse(steps, out int count))
        {
            var change = 0.015 * count;
            if (!_isMotorDirectionToOpen)
                change = -change;

            _channels[_currentChannelIndex].Flow += change;
        }

        Thread.Sleep(50);
        _response = $"steps={steps}";
    }
}
