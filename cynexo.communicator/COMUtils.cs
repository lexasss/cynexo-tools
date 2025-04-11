using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Management;

namespace Cynexo.Communicator;

/// <summary>
/// COM port utils: list of available ports, and firing events when this list changes.
/// </summary>
public class COMUtils : IDisposable
{
    /// <summary>
    /// Port descriptor
    /// </summary>
    public class Port(string id, string name, string? description, string? manufacturer)
    {
        public string ID { get; } = id;
        public string Name { get; } = name;
        public string? Description { get; } = description;
        public string? Manufacturer { get; } = manufacturer;
    }

    /// <summary>
    /// Fires when a COM port becomes available
    /// </summary>
    public event EventHandler<Port>? Inserted;
    /// <summary>
    /// Fires when a COM port is removed
    /// </summary>
    public event EventHandler<Port>? Removed;

    /// <summary>
    /// List of all COM ports in the system
    /// </summary>
    public Port[] Ports => GetAvailableCOMPorts();

    public COMUtils()
    {
        Listen("__InstanceCreationEvent", "Win32_USBControllerDevice", ActionType.Inserted);    // Win32_SerialPort
        Listen("__InstanceDeletionEvent", "Win32_USBControllerDevice", ActionType.Removed);
    }

    public void Dispose()
    {
        foreach (var watcher in _watchers)
        {
            watcher.Stop();
            watcher.Dispose();
        }

        GC.SuppressFinalize(this);
    }

    // Internal

    private enum ActionType
    {
        Inserted,
        Removed
    }

    readonly List<Port> _cachedPorts = [];
    
    readonly List<ManagementEventWatcher> _watchers = [];

    private void Listen(string source, string target, ActionType actionType)
    {
        var query = new WqlEventQuery($"SELECT * FROM {source} WITHIN 2 WHERE TargetInstance ISA '{target}'");
        var watcher = new ManagementEventWatcher(query);

        watcher.EventArrived += (s, e) =>
        {
            try
            {
                using var target = (ManagementBaseObject)e.NewEvent["TargetInstance"];
                switch (actionType)
                {
                    case ActionType.Inserted:
                        var port = CreateCOMPort(target.Properties);
                        if (port != null)
                        {
                            _cachedPorts.Add(port);
                            Inserted?.Invoke(this, port);
                        }
                        break;
                    case ActionType.Removed:
                        var deviceID = GetDeviceID(target.Properties);
                        if (deviceID != null)
                        {
                            var cachedPort = _cachedPorts.FirstOrDefault(port => port.ID == deviceID);
                            if (cachedPort != null)
                            {
                                _cachedPorts.Remove(cachedPort);
                                Removed?.Invoke(this, cachedPort);
                            }
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[USB] {ex.Message}");
            }
        };

        _watchers.Add(watcher);

        try
        {
            watcher.Start();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[USB] {ex.Message}");
        }
    }

    private Port[] GetAvailableCOMPorts()
    {
        var portNames = SerialPort.GetPortNames();

        IEnumerable<Port>? ports = null;

        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE Caption LIKE '%COM%'");
            var records = searcher.Get().Cast<ManagementBaseObject>();
            ports = records.Select(rec =>
                {
                    var id = rec["DeviceID"]?.ToString();
                    var name = 
                        portNames.FirstOrDefault(name => rec["DeviceID"]?.ToString()?.Contains($"{name}") ?? false) ??
                        portNames.FirstOrDefault(name => rec["Caption"]?.ToString()?.Contains($"({name})") ?? false) ??
                        "";
                    var description = rec["Description"]?.ToString();
                    var manufacturer = rec["Manufacturer"]?.ToString();
                    return new Port(id ?? "", name ?? id ?? "", description, manufacturer);
                })
                .Where(p => p.Name.StartsWith("COM"));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[USB] {ex.Message}");
        }

        _cachedPorts.Clear();
        if (ports != null)
        {
            foreach (var port in ports)
            {
                _cachedPorts.Add(port);
            }
        }

        return ports?.ToArray() ?? [];
    }

    private static Port? CreateCOMPort(PropertyDataCollection props, string? deviceName = null)
    {
        string? deviceID = null;
        string? descrition = null;
        string? manufacturer = null;

        foreach (PropertyData property in props)
        {
            if (property.Name == "DeviceID")
            {
                deviceID = (string?)property.Value;
            }
            else if (property.Name == "Description")
            {
                descrition = (string?)property.Value;
            }
            else if (property.Name == "Manufacturer")
            {
                manufacturer = (string?)property.Value;
            }
            else if (property.Name == "Dependent")  // this handles Win32_USBControllerDevice, as Win32_SerialPort stopped working
            {
                var usbControllerID = (string)property.Value;
                usbControllerID = usbControllerID.Replace("\"", "");
                var devID = usbControllerID.Split('=')[1];
                using var searcher = new ManagementObjectSearcher($"SELECT * FROM Win32_PnPEntity WHERE DeviceID LIKE '%{devID}%'");
                var records = searcher.Get().Cast<ManagementBaseObject>();
                foreach (var rec in records)
                {
                    var name = (string?)rec.Properties["Name"]?.Value;
                    if (name?.Contains("(COM") ?? false)
                    {
                        return CreateCOMPort(rec.Properties, name);
                    }
                }
            }
        }

        return deviceID == null ? null : new Port(deviceID, deviceName ?? "COMXX", descrition, manufacturer);
    }

    private static string? GetDeviceID(PropertyDataCollection props)
    {
        string? deviceID = null;

        foreach (PropertyData property in props)
        {
            if (property.Name == "DeviceID")
            {
                deviceID = (string?)property.Value;
            }
            else if (property.Name == "Dependent")  // this handles Win32_USBControllerDevice, as Win32_SerialPort stopped working
            {
                var usbControllerID = (string)property.Value;
                usbControllerID = usbControllerID.Replace("\"", "").Replace(@"\\", @"\");
                deviceID = usbControllerID.Split('=')[1];
            }
        }

        return deviceID;
    }
}