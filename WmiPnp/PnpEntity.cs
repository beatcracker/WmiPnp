﻿using LanguageExt;
using System.Management;
using WmiPnp.Extensions;

namespace WmiPnp;

public class PnpEntity
{
    public string? Name;
    public string? Description;

    public string? ClassGuid;
    public string? DeviceId;
    public string? PnpDeviceId;

    private ManagementObject? _entity = null;

    public Some<DeviceProperty> UpdateProperty( Some<DeviceProperty> deviceProperty )
        => GetDeviceProperty( deviceProperty.Value.Key )
        .Some( x => {
            deviceProperty.Value.Data = x.Data;
            return deviceProperty;
        } )
        .None( () => deviceProperty );

    /// <summary>
    /// Get device property
    /// </summary>
    /// <param name="key">Device property --key or --keyName</param>
    /// <returns></returns>
    public Option<DeviceProperty> GetDeviceProperty( string key )
    {
        ArgumentNullException.ThrowIfNull( _entity ); // TODO do not allow mandatory fields to be a null

        var args = new object[] { new string[] { key }, null! };
        try {
            _entity.InvokeMethod( GetDeviceProperties_MethodName, args );
        }
        catch ( ManagementException ) {
            // Not found or wrong key
            return Option<DeviceProperty>.None;
        }

        ManagementBaseObject? ss = ( args[1] as ManagementBaseObject[] )?[0];
        if ( ss is null ) return Option<DeviceProperty>.None;

        var ps =
            new Dictionary<string, object>(
                ss.Properties
                .Cast<PropertyData>()
                .Select( x => new KeyValuePair<string, object>( x.Name, x.Value ) ) );

        var typeValue = (uint)ss.GetPropertyValue( DeviceProperty.Type_PropertyField );
        _ = ps.TryGetValue(
            DeviceProperty.Data_PropertyField,
            out var dataValue );

        var noValidDataValue =
            typeValue == (uint)DataType.Empty
            || dataValue is null;

        if ( noValidDataValue )
            return Option<DeviceProperty>.None;

        DeviceProperty dp =
            new(
                deviceId: ss.ValueOf( DeviceProperty.DeviceID_PropertyField ),
                key: ss.ValueOf( DeviceProperty.Key_PropertyField ),
                type: typeValue,
                data: dataValue
            );

        return dp;
    }

    private static Option<PnpEntity> EntityOrNone( string where )
    {
        Option<PnpEntity> entity = Option<PnpEntity>.None;

        try {
            var searcher =
                new ManagementObjectSearcher(
                    Select_Win32PnpEntity_Where
                    + where );

            var collection = searcher.Get();

            var mo =
                collection
                .Cast<ManagementBaseObject>()
                .FirstOrDefault();

            var deviceFound = mo is not null;
            if ( deviceFound )
                entity = ToPnpEntity( mo! );
        }
        catch { }

        return entity;
    }

    /// <summary>
    /// Find exact one entity by given name
    /// </summary>
    /// <param name="name">The name or part of its for entities</param>
    /// <returns>PNP entity or None</returns>
    public static Option<PnpEntity> ByFriendlyName( string name )
        => EntityOrNone( where: $"{Name_FieldName}='{name}'" );

    /// <summary>
    /// Find entity by exact equal device id
    /// </summary>
    /// <param name="id">DeviceID or PNPDeviceID</param>
    /// <param name="duplicateSlashes">Duplicate slashes by default, so '\' becomes '\\'.</param>
    /// <returns>PnpEntity or None</returns>
    public static Option<PnpEntity> ByDeviceId( string id, bool duplicateSlashes = true )
    {
        if ( duplicateSlashes )
            id = id.Replace( "\\", "\\\\" );

        return
            EntityOrNone(
                where: $"{DeviceId_FieldName}='{id}' OR {PnpDeviceId_FieldName}='{id}'" );
    }

    /// <summary>
    /// Find one or more entities by given name
    /// </summary>
    /// <param name="name">The name or part of its for entities</param>
    /// <returns>List of found entities or empty list</returns>
    public static IEnumerable<PnpEntity> LikeFriendlyName( string name )
    {
        IEnumerable<PnpEntity> entities = List.empty<PnpEntity>();

        try {
            var searcher =
                new ManagementObjectSearcher(
                    Select_Win32PnpEntity_Where
                    + $"{Name_FieldName} LIKE '%{name}%'" );

            var collection = searcher.Get();

            entities =
                collection
                .Cast<ManagementBaseObject>()
                .Select( o => ToPnpEntity( o ) );
        }
        catch { }

        return entities;
    }

    private static PnpEntity ToPnpEntity(
        ManagementBaseObject entity )
        => new() {
            Name = entity.ValueOf( Name_FieldName ),
            Description = entity.ValueOf( Description_FieldName ),
            ClassGuid = entity.ValueOf( ClassGuid_FieldName ),
            DeviceId = entity.ValueOf( DeviceId_FieldName ),
            PnpDeviceId = entity.ValueOf( PnpDeviceId_FieldName ),
            _entity =
                entity as ManagementObject
                ?? throw new NotSupportedException( "Not a ManagementObject." ),
        };

    const string Select_Win32PnpEntity_Where =
       "Select Name,Description,ClassGuid,DeviceID,PNPDeviceID From Win32_PnPEntity WHERE ";

    public const string Name_FieldName = "Name";
    public const string Description_FieldName = "Description";
    public const string ClassGuid_FieldName = "ClassGuid";
    public const string DeviceId_FieldName = "DeviceID";
    public const string PnpDeviceId_FieldName = "PNPDeviceID";

    public const string GetDeviceProperties_MethodName = "GetDeviceProperties";
}