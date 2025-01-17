﻿using FluentResults;
using System.Management;

namespace WmiPnp.Xm4
{
    public class Xm4Entity
    {
        private readonly PnpEntity _xm4;
        private readonly PnpEntity _handsFree;

        private Xm4Entity( PnpEntity handsFree, PnpEntity xm4 )
        {
            _handsFree = handsFree;
            _xm4 = xm4;
        }

        public static Result<Xm4Entity> Create()
            => CreateBy(
                HandsFree_PnpEntity_FriendlyName,
                Headphones_PnpEntity_FriendlyName );

        public static Result<Xm4Entity> CreateBy(
            string handsfreeNameExact,
            string headphonesNameExact )
        {
            var hfResult =
                PnpEntity
                .ByFriendlyName(
                    handsfreeNameExact
                    ?? HandsFree_PnpEntity_FriendlyName );

            if ( hfResult.IsFailed )
                return Result.Fail( $"Can not create {HandsFree_PnpEntity_FriendlyName} entity" );

            var xm4result =
                PnpEntity
                .ByFriendlyName(
                    headphonesNameExact
                    ?? Headphones_PnpEntity_FriendlyName );

            if ( xm4result.IsFailed )
                return Result.Fail( $"Can not create {Headphones_PnpEntity_FriendlyName} entity" );

            return
                new Xm4Entity(
                    hfResult.Value,
                    xm4result.Value );
        }

        public static Result<Xm4Entity> CreateUnsafe(
            PnpEntity batteryEntity,
            PnpEntity stateEntity )
            => new Xm4Entity(
                batteryEntity,
                stateEntity );

        public int BatteryLevel {
            get {
                var batteryLevel =
                    _handsFree.GetDeviceProperty(
                        PnpEntity.DeviceProperty_BatteryLevel )
                    .Value;

                return (byte)( batteryLevel.Data ?? 0 );
            }
        }

        public bool IsConnected {
            get {
                var connected =
                    _xm4.GetDeviceProperty(
                        PnpEntity.DeviceProperty_IsConnected )
                    .Value;
                return (bool)( connected.Data ?? false );
            }
        }

        public Result<DateTime> LastConnectedTime {
            get {
                var dtResult =
                    _xm4.GetDeviceProperty(
                        PnpEntity.DeviceProperty_LastConnectedTime );

                return
                    dtResult.IsSuccess
                    ? ManagementDateTimeConverter
                        .ToDateTime( dtResult.Value.Data as string )
                        .ToUniversalTime()
                    : Result.Fail(
                        "Can not find `LastConnectedTime` property. It is possible the device is still connected." );
            }
        }

        public const string HandsFree_PnpEntity_FriendlyName
            = "WH-1000XM4 Hands-Free AG"; // Battery level related
        public const string Headphones_PnpEntity_FriendlyName
            = "WH-1000XM4"; // Headphones state related
    }
}