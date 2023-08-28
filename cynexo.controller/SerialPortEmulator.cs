using System;
using System.Threading;

namespace Cynexo.Controller;

/// <summary>
/// This class is used to emulate communication with the device via <see cref="CommPort"/>
/// </summary>
public class SerialPortEmulator : ISerialPort
{
    public bool IsOpen => _isOpen;

    public SerialPortEmulator()
    {
    }

    public void Open() { _isOpen = true; }

    public void Close()
    {
        _isOpen = false;
        Thread.Sleep(10);
    }

    public string ReadLine()
    {
        while (_isOpen)
        {
            Thread.Sleep(10);
            if (_hasResponse)
            {
                _hasResponse = false;
                return GenerateData();
            }
        }

        throw new Exception("Closed");
    }

    public void WriteLine(string text)
    {
        if (text.StartsWith("readFlow"))
        {
            _hasResponse = true;
        }
    }

    // Internal

    bool _isOpen = false;
    bool _hasResponse = false;

    private string GenerateData()
    {
        return "Some fake data is here";
    }

    private static class Random
    {
        public static float Range(float pm) => (float)((_random.NextDouble() - 0.5) * 2 * pm);

        static readonly System.Random _random = new();
    }
}
