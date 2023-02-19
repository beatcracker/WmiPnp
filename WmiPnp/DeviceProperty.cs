﻿using System.Management;

namespace WmiPnp;

public class DeviceProperty
{    
    public readonly string DeviceId; // ex. BTHENUM\{0000111E-0000-1000-8000-00805F9B34FB}_VID&0002054C_PID&0D58\8&37022D96&0&F84E17FE9B55_C00000000
    public readonly string Key; // ex. {540B947E-8B40-45BC-A8A2-6A0B894CBDA2} 8
    public readonly CimType Type;
    public string? Data;
    public readonly string? KeyName; // ex. DEVPKEY_Device_InLocalMachineContainer

    public DeviceProperty(
        string deviceId,
        string key,
        CimType type,

        string? data = null,
        string? keyName = null )
    {
        DeviceId = deviceId;
        Key = key;
        Type = type;

        KeyName = keyName ?? key;
        Data = data;
    }
}