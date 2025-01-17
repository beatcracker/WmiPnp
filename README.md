# WMI for Plug-and-Play Devices

> WMI = Windows Management Interface.

The primary project goal is to get battery level of WH-1000XM4 headphones.

![xm4battery-trayicon-230303](https://user-images.githubusercontent.com/11328666/222558052-b05eacab-6a9a-4d45-8d23-e94b1f33f9a7.jpg)

- [Xm4Battery](#xm4battery)
  - [Interface](#interface)
  - [Tray Icon Mods](#tray-icon-mods)
- [Xm4Entity](#xm4entity)
  - [Create XM4 Instance](#create-xm4-instance)
  - [Is Connected or Not?](#is-connected-or-not)
  - [What Is The Last Connected Time?](#what-is-the-last-connected-time)
  - [Headphones Battery Level](#headphones-battery-level)
- [PnpEntity](#pnpentity)
  - [How To Find PNP Device?](#how-to-find-pnp-device)
  - [Get / Update Specific Device Property](#get--update-specific-device-property)
  - [Enumerate Device Properties](#enumerate-device-properties)
- [Specific Device Properties](#specific-device-properties)
  - [Battery Level](#battery-level)
  - [Is Connected](#is-connected)
  - [Last Arrival Date](#last-arrival-date)
  - [Last Removal Date](#last-removal-date)
- [XM4 Related Properties](#xm4-related-properties)
  - [DEVPKEY\_Device\_DevNodeStatus](#devpkey_device_devnodestatus)
  - [DEVPKEY\_Bluetooth\_LastConnectedTime](#devpkey_bluetooth_lastconnectedtime)
  - [?Last Connected Time](#last-connected-time)
- [Links](#links)

## Xm4Battery

WinForms window-less trayicon application. Ready to run app is available at the [Latest Release](https://github.com/nikvoronin/WmiPnp/releases/latest) section.

System requirements: Windows 10 x64, .NET 6.0.

### Interface

- F - 100% or fully charged
- 9..5 - 90..50%
- 4..3 yellow - 40..30%
- 2 orange - 20%
- 1 red - 10%
- X - headphones disconnected, gray background. Tooltip displays last known battery level and the last connected date/time.

`Right Mouse Button` opens context menu:

- About - lead to this page.
- Quit - close and unload application at all.

### Tray Icon Mods

Background colors defined at the `CreateLevelIcon` method:

```csharp
var brush =
    level switch {
        > 0 and <= 10 => Brushes.Red,
        > 0 and <= 20 => Brushes.Orange,
        > 0 and <= 40 => Brushes.Yellow,
        <= 0 => Brushes.Gray,
        _ => Brushes.White
    };
```

Font for icon symbols/digits:

```csharp
static readonly Font _notifyIconFont
    = new ( "Segoe UI", 16, FontStyle.Regular );
```

## Xm4Entity

### Create XM4 Instance

```csharp
var xm4result = Xm4Entity.Create();
if ( xm4result.IsFailed ) return; // headphones did not found at all

Xm4Entity _xm4 = xm4result.Value;
```

### Is Connected or Not?

```csharp
...
bool connected = _xm4.IsConnected;
```

### What Is The Last Connected Time?

```csharp
Result<DateTime> dt = _xm4.LastConnectedTime;
```

```csharp
if ( !_xm4.IsConnected )
    Console.WriteLine( $"Last connected time: {_xm4.LastConnectedTime.Value}.\n" );
else
    var it_is_true = _xm4.LastConnectedTime.IsFailed; // can not get the last connection time
```

We can not get the last connection time if headphones is online and connected.

### Headphones Battery Level

Can get actual battery level if headphones are connected or the last known level (headphones are not connected).

```csharp
int level = _xm4.BatteryLevel;
```

## PnpEntity

First we should know the `name` or `device id` of the device we фку working with or at least a part of the device name.

- ByFriendlyName ( exact a friendly name )
- ByDeviceId ( exact a device id, like {GUID} pid )
- LikeFriendlyName ( a part of a friendly name ) - returns a list of found devices: `IEnumerable<PnpEntity>` or empty list.

All of methods produce instances of `PnpEntity` or `Result.Fail` if the given device was not found.

### How To Find PNP Device?

```csharp
Result<PnpEntity> result =
    PnpEntity.ByFriendlyName( "The Bluetooth Device #42" );

if ( result.IsSuccess ) {
    PnpEntity btDevice = result.Value;
    ...
}
```

### Get / Update Specific Device Property

```csharp
...
PnpEntity btDevice = result.Value;

Result<DeviceProperty> propertyResult =
    btDevice.GetDeviceProperty( PnpEntity.DeviceProperty_IsConnected );

if ( propertyResult.IsSuccess ) {
    DeviceProperty dp = propertyResult.Value;

    while ( !Console.KeyAvailable ) {
        bool connected = (bool)(dp.Data ?? false);

        Console.WriteLine(
            $"{btDevice.Name} is {(connected ? "connected" : "disconnected")}" );

        btDevice.UpdateProperty( dp );
    }    
}
```

### Enumerate Device Properties

```csharp
...
PnpEntity btDevice = result.Value;

IEnumerable<DeviceProperty> properties = btDevice.UpdateProperties();

foreach( var p in properties ) {
    Console.WriteLine( $"{p.KeyName}: {p.Data}" );
    ...
}
```

The same but with a cached list of the last update result

```csharp
_ = btDevice.UpdateProperties();

foreach( var p in btDevice.Properties ) {
    ...
```

## Specific Device Properties

> Key = {GUID} pid

### Battery Level

- Key = `{104EA319-6EE2-4701-BD47-8DDBF425BBE5} 2`
- Type = 3 (Byte)

`Data` is in percents

### Is Connected

- Key = `{83DA6326-97A6-4088-9453-A1923F573B29} 15`
- Type = 17 (Boolean)

Data = False → device is disconnected

### Last Arrival Date

- Key = `{83DA6326-97A6-4088-9453-A1923F573B29} 102`
- KeyName = DEVPKEY_Device_LastArrivalDate
- Type = 16 (FileTime)

Data = 20230131090906.098359+180 → 2023 Jan 31, 9:09:06 GMT+3

### Last Removal Date

Key = {83da6326-97a6-4088-9453-a1923f573b29} 103

## XM4 Related Properties

- `WH-1000XM4 Hands-Free AG` - exact name for PnpEntity to get a **BATTERY LEVEL** --only-- of the xm4.
- `WH-1000XM4` - exact name for PnpEntity to get a **STATE** of the xm4.

### DEVPKEY_Device_DevNodeStatus

> Instead of this bit flags, we can use [Is Connected](#is-connected) property to retrieve a connection status of xm4.

- Key = `{4340A6C5-93FA-4706-972C-7B648008A5A7} 2`
- KeyName = DEVPKEY_Device_DevNodeStatus
- Type = 7 (Uint32)

- Connected = 25190410 (fall bit#25): value & 0x20000 == 0
- Disconnected = 58744842 (set bit#25): value & 0x20000 == 0x20000

### DEVPKEY_Bluetooth_LastConnectedTime

This is only property to retrieve a last connection date-time of xm4. This property does not present when headphones are connected.

- Key = `{2BD67D8B-8BEB-48D5-87E0-6CDA3428040A} 11`
- KeyName = DEVPKEY_Bluetooth_LastConnectedTime
- Type = 16 (FileTime)

For ex.: Data = 20230131090906.098359+180 → 2023 Jan 31, 9:09:06, GMT+3

### ?Last Connected Time

Contains the same data as the [DEVPKEY_Bluetooth_LastConnectedTime](#devpkey_bluetooth_lastconnectedtime) property. Same behavior.

- Key = `{2BD67D8B-8BEB-48D5-87E0-6CDA3428040A} 5`
- Type = 16 (FileTime)

## Links

- [Enumerating windows device](https://www.codeproject.com/articles/14412/enumerating-windows-device). Enumerating the device using the SetupDi* API provided with WinXP. CodeProject // 17 Jun 2006
- [How to get the details for each enumerated device?](https://social.msdn.microsoft.com/Forums/en-US/65086709-cee8-4efa-a794-b32979abb0ea/how-to-get-the-details-for-each-enumerated-device?forum=vbgeneral) MSDN, Archived Forums 421-440, Visual Basic.
- [Query battery level for WH-1000XM4 wireless headphones](https://gist.github.com/nikvoronin/e8fc8a1631dd0e851f1ab821d0e3cf01) by PowerShell script.
