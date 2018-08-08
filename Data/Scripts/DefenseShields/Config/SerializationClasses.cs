﻿using System;
using System.ComponentModel;
using ProtoBuf;
using VRageMath;

namespace DefenseShields
{
    [ProtoContract]
    public class ShieldState
    {
        [ProtoMember(1), DefaultValue(-1)]
        public float Buffer;
        [ProtoMember(2), DefaultValue(-1)]
        public double IncreaseO2ByFPercent = 0f;
        [ProtoMember(3), DefaultValue(-1)]
        public float ModulateEnergy = 1f;
        [ProtoMember(4), DefaultValue(-1)]
        public float ModulateKinetic = 1f;
        [ProtoMember(5), DefaultValue(-1)]
        public int EnhancerPowerMulti = 1;
        [ProtoMember(6), DefaultValue(-1)]
        public int EnhancerProtMulti = 1;
        [ProtoMember(7)]
        public bool Online = false;
        [ProtoMember(8)]
        public bool Overload = false;
        [ProtoMember(9)]
        public bool Remodulate = false;
        [ProtoMember(10)]
        public bool Lowered = false;
        [ProtoMember(11)]
        public bool Sleeping = false;
        [ProtoMember(12)]
        public bool Suspended = false;
        [ProtoMember(13)]
        public bool Waking = false;
        [ProtoMember(14)]
        public bool FieldBlocked = false;
        [ProtoMember(15)]
        public bool InFaction = false;
        [ProtoMember(16)]
        public bool IsOwner = false;
        [ProtoMember(17)]
        public bool ControllerGridAccess = true;
        [ProtoMember(18)]
        public bool ResetShape = false;
        [ProtoMember(19)]
        public bool Enhancer = false;
        [ProtoMember(20), DefaultValue(-1)]
        public double EllipsoidAdjust = Math.Sqrt(2);
        [ProtoMember(21)]
        public Vector3D GridHalfExtents;

        public override string ToString()
        {
            return $"Buffer = {Math.Round(Buffer, 4)}";
        }
    }

    [ProtoContract]
    public class DefenseShieldsModSettings
    {
        [ProtoMember(1)]
        public bool Enabled = false;

        [ProtoMember(2), DefaultValue(-1)]
        public float Width = 30f;

        [ProtoMember(3), DefaultValue(-1)]
        public float Height = 30f;

        [ProtoMember(4), DefaultValue(-1)]
        public float Depth = 30f;

        [ProtoMember(5)]
        public bool PassiveInvisible = false;

        [ProtoMember(6)]
        public bool ActiveInvisible = false;

        [ProtoMember(7), DefaultValue(-1)]
        public float Rate = 50f;

        [ProtoMember(8)]
        public bool ExtendFit = false;

        [ProtoMember(9)]
        public bool SphereFit = false;

        [ProtoMember(10)]
        public bool FortifyShield = false;

        [ProtoMember(11)]
        public bool SendToHud = true;

        [ProtoMember(12)]
        public bool UseBatteries = true;

        [ProtoMember(13)]
        public bool ShieldActive = false;

        [ProtoMember(14)]
        public bool RaiseShield = true;

        [ProtoMember(15)]
        public long ShieldShell = 0;

        public override string ToString()
        {
            return $"Enabled = {Enabled}\nIdleVisible = {PassiveInvisible}\nActiveVisible = {ActiveInvisible}\nWidth = {Math.Round(Width, 4)}" +
                   $"\nHeight = {Math.Round(Height, 4)}\nDepth = {Math.Round(Depth, 4)}\nRate = {Math.Round(Rate, 4)}" +
                   $"\nExtendFit = {ExtendFit}\nSphereFit = {SphereFit}" +
                   $"\nFortifyShield = {FortifyShield}\nSendToHud = {SendToHud}\nUseBatteries = {UseBatteries}" +
                   $"\nShieldActive = {ShieldActive}\nRaiseShield = {RaiseShield}\n ShieldShell = {ShieldShell}";
        }
    }

    [ProtoContract]
    public class DefenseShieldsEnforcement
    {
        [ProtoMember(1), DefaultValue(-1)]
        public float Nerf = -1f;

        [ProtoMember(2), DefaultValue(-1)]
        public int BaseScaler = -1;

        [ProtoMember(3), DefaultValue(-1)]
        public float Efficiency = -1f;

        [ProtoMember(4), DefaultValue(-1)]
        public int StationRatio = -1;

        [ProtoMember(5), DefaultValue(-1)]
        public int LargeShipRatio = -1;

        [ProtoMember(6), DefaultValue(-1)]
        public int SmallShipRatio = -1;

        [ProtoMember(7), DefaultValue(-1)]
        public int DisableVoxelSupport = -1;

        [ProtoMember(8), DefaultValue(-1)]
        public int DisableGridDamageSupport = -1;

        [ProtoMember(9), DefaultValue(-1)]
        public int Debug = -1;

        [ProtoMember(10)]
        public bool AltRecharge = false;

        [ProtoMember(11), DefaultValue(-1)]
        public int Version = -1;

        [ProtoMember(12)]
        public ulong SenderId = 0;

        public override string ToString()
        {
            return $"Nerf = {Math.Round(Nerf, 4)}\nBaseScaler = {BaseScaler}\nEfficiency = {Math.Round(Efficiency, 4)}\nStationRatio = {StationRatio}\nLargeShipRatio = {LargeShipRatio}" +
                   $"\nSmallShipRatio = {SmallShipRatio}\nDisableVoxelSupport = {DisableVoxelSupport}\nDisableGridDamageSupport = {DisableGridDamageSupport}" +
                   $"\nDebug = {Debug}\nAltRecharge = {AltRecharge}\nVersion = {Version}\nSenderId = {SenderId}";
        }

    }

    [ProtoContract]
    public class ModulatorBlockSettings
    {
        [ProtoMember(1)]
        public bool Enabled = true;

        [ProtoMember(2)]
        public bool ModulateVoxels = true;

        [ProtoMember(3)]
        public bool ModulateGrids = false;

        [ProtoMember(4), DefaultValue(-1)]
        public int ModulateDamage = 100;

        public override string ToString()
        {
            return $"Enabled = {Enabled}\nModulateVoxels = {ModulateVoxels}\nModulateGrids = {ModulateGrids}\nModulateDamage = {ModulateDamage}";
        }
    }

    [ProtoContract]
    public class StateData
    {
        [ProtoMember(1)]
        public PacketType Type = PacketType.STATE;

        [ProtoMember(2)]
        public long EntityId = 0;

        [ProtoMember(3)]
        public ulong Sender = 0;

        [ProtoMember(4)]
        public ShieldState State = null;

        public StateData() { } // empty ctor is required for deserialization

        public StateData(ulong sender, long entityId, ShieldState state)
        {
            Type = PacketType.STATE;
            Sender = sender;
            EntityId = entityId;
            State = state;
        }

        public StateData(ulong sender, long entityId, PacketType action)
        {
            Type = action;
            Sender = sender;
            EntityId = entityId;
            State = null;
        }
    }

    [ProtoContract]
    public class PacketData
    {
        [ProtoMember(1)]
        public PacketType Type = PacketType.SETTINGS;

        [ProtoMember(2)]
        public long EntityId = 0;

        [ProtoMember(3)]
        public ulong Sender = 0;

        [ProtoMember(4)]
        public DefenseShieldsModSettings Settings = null;

        public PacketData() { } // empty ctor is required for deserialization

        public PacketData(ulong sender, long entityId, DefenseShieldsModSettings settings)
        {
            Type = PacketType.SETTINGS;
            Sender = sender;
            EntityId = entityId;
            Settings = settings;
        }

        public PacketData(ulong sender, long entityId, PacketType action)
        {
            Type = action;
            Sender = sender;
            EntityId = entityId;
            Settings = null;
        }
    }

    [ProtoContract]
    public class EnforceData
    {
        [ProtoMember(1)]
        public PacketType Type = PacketType.ENFORCE;

        [ProtoMember(2)]
        public long EntityId = 0;

        [ProtoMember(3)]
        public ulong Sender = 0;

        [ProtoMember(4)]
        public DefenseShieldsEnforcement Enforce = null;

        public EnforceData() { } // empty ctor is required for deserialization

        public EnforceData(ulong sender, long entityId, DefenseShieldsEnforcement enforce)
        {
            Type = PacketType.ENFORCE;
            Sender = sender;
            EntityId = entityId;
            Enforce = enforce;
        }

        public EnforceData(ulong sender, long entityId, PacketType action)
        {
            Type = action;
            Sender = sender;
            EntityId = entityId;
            Enforce = null;
        }
    }

    [ProtoContract]
    public class ModulatorData
    {
        [ProtoMember(1)]
        public PacketType Type = PacketType.MODULATOR;

        [ProtoMember(2)]
        public long EntityId = 0;

        [ProtoMember(3)]
        public ulong Sender = 0;

        [ProtoMember(4)]
        public ModulatorBlockSettings Settings = null;

        public ModulatorData() { } // empty ctor is required for deserialization

        public ModulatorData(ulong sender, long entityId, ModulatorBlockSettings settings)
        {
            Type = PacketType.MODULATOR;
            Sender = sender;
            EntityId = entityId;
            Settings = settings;
        }

        public ModulatorData(ulong sender, long entityId, PacketType action)
        {
            Type = action;
            Sender = sender;
            EntityId = entityId;
            Settings = null;
        }
    }
    public enum PacketType : byte
    {
        STATE,
        SETTINGS,
        ENFORCE,
        MODULATOR,
    }
}
