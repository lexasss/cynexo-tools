using System;
using System.Threading;

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

    bool _hasResponse = false;

    private static string GenerateData()
    {
        return "Some fake data is here";
    }

    private static class Random
    {
        public static float Range(float pm) => (float)((_random.NextDouble() - 0.5) * 2 * pm);

        static readonly System.Random _random = new();
    }
}
