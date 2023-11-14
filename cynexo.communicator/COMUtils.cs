using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Management;

namespace Cynexo.Communicator;

/// <summary>
/// COM port utils: list of available ports, and firing events when this list changes.
/// </summary>
public class COMUtils
{
    /// <summary>
    /// Port descriptor
    /// </summary>
    public class Port
    {
        public string Name { get; }
        public string? Description { get; }
        public string? Manufacturer { get; }
        public Port(string name, string? description, string? manufacturer)
        {
            Name = name;
            Description = description;
            Manufacturer = manufacturer;
        }
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
    public static Port[] Ports => _cachedPorts ??= GetAvailableCOMPorts();

    public COMUtils()
    {
        Listen("__InstanceCreationEvent", "Win32_SerialPort", ActionType.Inserted);
        Listen("__InstanceDeletionEvent", "Win32_SerialPort", ActionType.Removed);
    }

    // Internal

    private enum ActionType
    {
        Inserted,
        Removed
    }

    static Port[]? _cachedPorts = null;

    private void Listen(string source, string target, ActionType actionType)
    {
        var query = new WqlEventQuery($"SELECT * FROM {source} WITHIN 2 WHERE TargetInstance ISA '{target}'");
        var watcher = new ManagementEventWatcher(query);

        watcher.EventArrived += (s, e) =>
        {
            _cachedPorts = null;
            Port? port = null;

            try
            {
                var target = (ManagementBaseObject)e.NewEvent["TargetInstance"];
                port = CreateCOMPort(target.Properties);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("USB ERROR: " + ex.Message);
            }

            if (port != null)
            {
                switch (actionType)
                {
                    case ActionType.Inserted:
                        Inserted?.Invoke(this, port);
                        break;
                    case ActionType.Removed:
                        Removed?.Invoke(this, port);
                        break;
                }
            }
        };

        watcher.Start();
    }

    private static Port[] GetAvailableCOMPorts()
    {
        var portNames = SerialPort.GetPortNames();

        IEnumerable<Port>? ports = null;

        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE Caption LIKE '%COM%'");
            ManagementBaseObject[]? records = searcher.Get().Cast<ManagementBaseObject>().ToArray();
            ports = records.Select(rec =>
                {
                    var name =
                        portNames.FirstOrDefault(name => rec["Caption"]?.ToString()?.Contains($"({name})") ?? false) ??
                        portNames.FirstOrDefault(name => rec["DeviceID"]?.ToString()?.Contains($"{name}") ?? false) ??
                        "";
                    var description = rec["Description"]?.ToString();
                    var manufacturer = rec["Manufacturer"]?.ToString();
                    return new Port(name, description, manufacturer);
                })
                .Where(p => p.Name.StartsWith("COM"));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("USB ERROR: " + ex.Message);
        }

        return ports?.ToArray() ?? Array.Empty<Port>();
    }

    private static Port? CreateCOMPort(PropertyDataCollection props)
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
        }

        return deviceID == null ? null : new Port(deviceID, descrition, manufacturer);
    }
}