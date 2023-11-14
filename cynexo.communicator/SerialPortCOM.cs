using System.IO.Ports;

namespace Cynexo.Communicator;

/// <summary>
/// This class only translates all method calls to an instance of <see cref="SerialPort"/>
/// This class is needed only because using <see cref="ISerialPort"/> interface allows 
/// testing the module without opening a real serial port
/// (we use <see cref="SerialPortEmulator"/> for this purpose).
/// </summary>
public class SerialPortCOM : ISerialPort
{
    public bool IsOpen => _port.IsOpen;

    public event SerialErrorReceivedEventHandler? ErrorReceived;

    public SerialPortCOM(string name)
    {
        _port = new SerialPort(name)
        {
            StopBits = StopBits.One,
            Parity = Parity.None,
            BaudRate = 9600,
            DataBits = 8,
            DtrEnable = true,
            RtsEnable = true,
            DiscardNull = false,
            WriteTimeout = 300, // only for writing.. reading should be able to hand until it returns with some data 
        };
        _port.ErrorReceived += ErrorReceived;
    }

    public void Open()
    {
        _port.Open();

        try { _port.BaseStream.Flush(); }
        catch { }  // do nothing
    }

    public void Close() => _port.Close();

    public string ReadLine() => _port.ReadLine();

    public void WriteLine(string text) => _port.Write(text);

    // Internal

    readonly SerialPort _port;
}
