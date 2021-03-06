﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using DefenseShields.Support;
using ParallelTasks;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace DefenseShields
{
    public partial class DefenseShields 
    {
        #region Setup
        private uint _tick;
        private uint _shieldEntRendId;
        private uint _subTick;
        private uint _funcTick;
        private uint _shapeTick;
        private uint _heatVentingTick = uint.MaxValue;
        internal uint UnsuspendTick;
        internal uint LosCheckTick;

        internal float ImpactSize { get; set; } = 9f;
        internal float Absorb { get; set; }
        internal float ShieldMaxBuffer;
        private float _power = 0.001f;
        private float _gridMaxPower;
        private float _gridCurrentPower;
        private float _powerNeeded;
        private float _otherPower;
        private float _gridAvailablePower;
        private float _batteryMaxPower;
        private float _batteryCurrentPower;
        private float _shieldMaxChargeRate;
        private float _shieldChargeRate;
        private float _damageReadOut;
        private float _accumulatedHeat;
        private float _shieldCurrentPower;
        private float _shieldMaintaintPower;
        private float _shieldConsumptionRate;
        private float _oldShieldFudge;
        private float _empScaleHp = 1f;
        private float _runningDamage;

        internal double EmpSize { get; set; }
        internal double BoundingRange;
        internal double EllipsoidVolume;
        private double _oldEllipsoidAdjust;
        private double _sAvelSqr;
        private double _ellipsoidSurfaceArea;
        private double _shieldVol;
        private double _sizeScaler;
        private double _roundedGridMax;

        public int BulletCoolDown { get; internal set; } = -1;
        public int WebCoolDown { get; internal set; } = -1;
        public int HitCoolDown { get; private set; } = -11;

        private int _count = -1;
        private int _lCount;
        private int _eCount;
        private int _powerNoticeLoop;
        private int _offlineCnt = -1;
        private int _overLoadLoop = -1;
        private int _empOverLoadLoop = -1;
        private int _genericDownLoop = -1;
        private int _reModulationLoop = -1;
        private int _heatCycle = -1;
        private int _fallbackCycle;
        private int _currentHeatStep;
        private int _empScaleTime = 1;

        private const int ReModulationCount = 300;
        private const int ShieldDownCount = 1200;
        private const int EmpDownCount = 3600;
        private const int GenericDownCount = 300;
        private const int PowerNoticeCount = 600;
        private const int OverHeat = 1200;
        private const int HeatingStep = 600;
        private const int CoolingStep = 1200;
        private const int FallBackStep = 10;

        private int _prevLod;
        private int _onCount;
        private int _shieldRatio = 1;

        internal bool WasOnline;
        internal bool DeformEnabled;
        internal bool ExplosionEnabled;
        internal bool PrePowerInit;
        internal bool PowerInited;
        internal bool AllInited;
        internal bool ContainerInited;
        internal bool HealthInited;
        internal bool WarmedUp;
        internal bool ComingOnline;
        internal bool Warming;
        internal bool UpdateDimensions;
        internal bool FitChanged;
        internal bool GridIsMobile;
        internal bool SettingsUpdated;
        internal bool ClientUiUpdate;
        internal bool IsStatic;
        internal bool WebDamage;

        private bool _resetEntity;
        private bool _empOverLoad;
        private bool _isDedicated;
        private bool _mpActive;
        private bool _isServer;
        private bool _hadPowerBefore;
        private bool _prevShieldActive;
        private bool _controlBlockWorking;
        private bool _requestedEnforcement;
        private bool _slaveLink;
        private bool _subUpdate;
        private bool _updateGridDistributor;
        private bool _effectsCleanup;
        private bool _hideShield;
        private bool _hideColor;
        private bool _supressedColor;
        private bool _shapeChanged;
        private bool _entityChanged;
        private bool _updateRender;
        private bool _functionalAdded;
        private bool _functionalRemoved;
        private bool _functionalChanged;
        private bool _functionalEvent;
        private bool _blockAdded;
        private bool _blockRemoved;
        private bool _blockChanged;
        private bool _blockEvent;
        private bool _shapeEvent;
        private bool _enablePhysics = true;
        private bool _updateMobileShape;
        private bool _clientNotReady;
        private bool _clientLowered;
        private bool _clientOn;
        private bool _tick60;
        private bool _tick600;
        private bool _syncEnts;

        private const string SpaceWolf = "Space_Wolf";
        private string _modelActive = "\\Models\\Cubes\\ShieldActiveBase.mwm";
        private string _modelPassive = "";

        private const string ModelMediumReflective = "\\Models\\Cubes\\ShieldPassive11.mwm";
        private const string ModelHighReflective = "\\Models\\Cubes\\ShieldPassive.mwm";
        private const string ModelLowReflective = "\\Models\\Cubes\\ShieldPassive10.mwm";
        private const string ModelRed = "\\Models\\Cubes\\ShieldPassive09.mwm";
        private const string ModelBlue = "\\Models\\Cubes\\ShieldPassive08.mwm";
        private const string ModelGreen = "\\Models\\Cubes\\ShieldPassive07.mwm";
        private const string ModelPurple = "\\Models\\Cubes\\ShieldPassive06.mwm";
        private const string ModelGold = "\\Models\\Cubes\\ShieldPassive05.mwm";
        private const string ModelOrange = "\\Models\\Cubes\\ShieldPassive04.mwm";
        private const string ModelCyan = "\\Models\\Cubes\\ShieldPassive03.mwm";

        private Vector2D _shieldIconPos = new Vector2D(-0.89, -0.86);

        internal Vector3D DetectionCenter;
        internal Vector3D WorldImpactPosition { get; set; } = new Vector3D(Vector3D.NegativeInfinity);
        internal Vector3D EmpDetonation { get; set; } = new Vector3D(Vector3D.NegativeInfinity);
        internal Vector3D ShieldSize { get; set; }
        internal Vector3D DebugVector3D;
        private Vector3D _localImpactPosition;
        private Vector3D _oldGridHalfExtents;

        internal MatrixD DetectMatrixOutsideInv;
        internal MatrixD DetectMatrixInInv;
        private MatrixD _shieldShapeMatrix;
        internal MatrixD DetectMatrixOutside;
        internal MatrixD DetectMatrixIn;
        internal MatrixD ShieldMatrix;
        internal MatrixD OldShieldMatrix;
        internal MatrixD OffsetEmitterWMatrix;

        internal BoundingBox ShieldAabb = new BoundingBox(-Vector3D.One, Vector3D.One);
        public BoundingSphereD ShieldSphere = new BoundingSphereD(Vector3D.Zero, 1);
        private BoundingSphereD _clientPruneSphere = new BoundingSphereD(Vector3D.Zero, 1f);
        private BoundingSphereD _pruneSphere1 = new BoundingSphereD(Vector3D.Zero, 1f);
        private BoundingSphereD _pruneSphere2 = new BoundingSphereD(Vector3D.Zero, 1f);

        public MyOrientedBoundingBoxD SOriBBoxD = new MyOrientedBoundingBoxD();

        private Quaternion _sQuaternion;
        private readonly List<MyResourceSourceComponent> _powerSources = new List<MyResourceSourceComponent>();
        private readonly List<MyCubeBlock> _functionalBlocks = new List<MyCubeBlock>();
        private readonly List<MyEntity> _pruneList = new List<MyEntity>();
        private readonly List<MyEntity> _clientPruneList = new List<MyEntity>();
        private readonly List<IMyBatteryBlock> _batteryBlocks = new List<IMyBatteryBlock>();
        private readonly List<KeyValuePair<MyEntity, EntIntersectInfo>> _webEntsTmp = new List<KeyValuePair<MyEntity, EntIntersectInfo>>();

        private Color _oldPercentColor = Color.Transparent;
        internal Task FuncTask;
        internal readonly object SubLock = new Object();

        internal readonly HashSet<IMyEntity> AuthenticatedCache = new HashSet<IMyEntity>();
        internal readonly HashSet<MyEntity> FriendlyCache = new HashSet<MyEntity>();
        internal readonly HashSet<MyEntity> PartlyProtectedCache = new HashSet<MyEntity>();
        internal readonly HashSet<MyEntity> IgnoreCache = new HashSet<MyEntity>();
        internal readonly HashSet<MyEntity> EnemyShields = new HashSet<MyEntity>();
        internal readonly HashSet<MyEntity> Missiles = new HashSet<MyEntity>();
        internal readonly HashSet<MyEntity> FriendlyMissileCache = new HashSet<MyEntity>();

        public readonly ConcurrentDictionary<MyEntity, EntIntersectInfo> WebEnts = new ConcurrentDictionary<MyEntity, EntIntersectInfo>();

        private readonly MyConcurrentQueue<MyCubeGrid> _eject = new MyConcurrentQueue<MyCubeGrid>();
        private readonly MyConcurrentQueue<IMySlimBlock> _dmgBlocks = new MyConcurrentQueue<IMySlimBlock>();
        private readonly MyConcurrentQueue<IMyWarhead> _empDmg = new MyConcurrentQueue<IMyWarhead>();
        private readonly MyConcurrentQueue<IMySlimBlock> _fewDmgBlocks = new MyConcurrentQueue<IMySlimBlock>();
        private readonly MyConcurrentQueue<MyEntity> _missileDmg = new MyConcurrentQueue<MyEntity>();
        private readonly MyConcurrentQueue<IMyMeteor> _meteorDmg = new MyConcurrentQueue<IMyMeteor>();
        private readonly MyConcurrentQueue<IMySlimBlock> _destroyedBlocks = new MyConcurrentQueue<IMySlimBlock>();
        private readonly MyConcurrentQueue<MyCubeGrid> _staleGrids = new MyConcurrentQueue<MyCubeGrid>();
        private readonly MyConcurrentQueue<IMyCharacter> _characterDmg = new MyConcurrentQueue<IMyCharacter>();
        private readonly MyConcurrentQueue<MyVoxelBase> _voxelDmg = new MyConcurrentQueue<MyVoxelBase>();
        private readonly MyConcurrentQueue<MyImpulseData> _impulseData = new MyConcurrentQueue<MyImpulseData>();
        private readonly MyConcurrentQueue<MyAddForceData> _forceData = new MyConcurrentQueue<MyAddForceData>();

        private static readonly MyStringId HudIconOffline = MyStringId.GetOrCompute("DS_ShieldOffline");
        private static readonly MyStringId HudIconHealth10 = MyStringId.GetOrCompute("DS_ShieldHealth10");
        private static readonly MyStringId HudIconHealth20 = MyStringId.GetOrCompute("DS_ShieldHealth20");
        private static readonly MyStringId HudIconHealth30 = MyStringId.GetOrCompute("DS_ShieldHealth30");
        private static readonly MyStringId HudIconHealth40 = MyStringId.GetOrCompute("DS_ShieldHealth40");
        private static readonly MyStringId HudIconHealth50 = MyStringId.GetOrCompute("DS_ShieldHealth50");
        private static readonly MyStringId HudIconHealth60 = MyStringId.GetOrCompute("DS_ShieldHealth60");
        private static readonly MyStringId HudIconHealth70 = MyStringId.GetOrCompute("DS_ShieldHealth70");
        private static readonly MyStringId HudIconHealth80 = MyStringId.GetOrCompute("DS_ShieldHealth80");
        private static readonly MyStringId HudIconHealth90 = MyStringId.GetOrCompute("DS_ShieldHealth90");
        private static readonly MyStringId HudIconHealth100 = MyStringId.GetOrCompute("DS_ShieldHealth100");

        //private static readonly MyStringId HudIconHeal = MyStringId.GetOrCompute("DS_ShieldHeal");
        private static readonly MyStringId HudIconHeal10 = MyStringId.GetOrCompute("DS_ShieldHeal10");
        private static readonly MyStringId HudIconHeal20 = MyStringId.GetOrCompute("DS_ShieldHeal20");
        private static readonly MyStringId HudIconHeal30 = MyStringId.GetOrCompute("DS_ShieldHeal30");
        private static readonly MyStringId HudIconHeal40 = MyStringId.GetOrCompute("DS_ShieldHeal40");
        private static readonly MyStringId HudIconHeal50 = MyStringId.GetOrCompute("DS_ShieldHeal50");
        private static readonly MyStringId HudIconHeal60 = MyStringId.GetOrCompute("DS_ShieldHeal60");
        private static readonly MyStringId HudIconHeal70 = MyStringId.GetOrCompute("DS_ShieldHeal70");
        private static readonly MyStringId HudIconHeal80 = MyStringId.GetOrCompute("DS_ShieldHeal80");
        private static readonly MyStringId HudIconHeal90 = MyStringId.GetOrCompute("DS_ShieldHeal90");
        private static readonly MyStringId HudIconHeal100 = MyStringId.GetOrCompute("DS_ShieldHeal100");

        //private static readonly MyStringId HudIconDps = MyStringId.GetOrCompute("DS_HudIconDps");
        private static readonly MyStringId HudIconDps10 = MyStringId.GetOrCompute("DS_ShieldDps10");
        private static readonly MyStringId HudIconDps20 = MyStringId.GetOrCompute("DS_ShieldDps20");
        private static readonly MyStringId HudIconDps30 = MyStringId.GetOrCompute("DS_ShieldDps30");
        private static readonly MyStringId HudIconDps40 = MyStringId.GetOrCompute("DS_ShieldDps40");
        private static readonly MyStringId HudIconDps50 = MyStringId.GetOrCompute("DS_ShieldDps50");
        private static readonly MyStringId HudIconDps60 = MyStringId.GetOrCompute("DS_ShieldDps60");
        private static readonly MyStringId HudIconDps70 = MyStringId.GetOrCompute("DS_ShieldDps70");
        private static readonly MyStringId HudIconDps80 = MyStringId.GetOrCompute("DS_ShieldDps80");
        private static readonly MyStringId HudIconDps90 = MyStringId.GetOrCompute("DS_ShieldDps90");
        private static readonly MyStringId HudIconDps100 = MyStringId.GetOrCompute("DS_ShieldDps100");

        //private static readonly MyStringId HudIconHeat = MyStringId.GetOrCompute("DS_HudIconHeat");
        private static readonly MyStringId HudIconHeat10 = MyStringId.GetOrCompute("DS_ShieldHeat10");
        private static readonly MyStringId HudIconHeat20 = MyStringId.GetOrCompute("DS_ShieldHeat20");
        private static readonly MyStringId HudIconHeat30 = MyStringId.GetOrCompute("DS_ShieldHeat30");
        private static readonly MyStringId HudIconHeat40 = MyStringId.GetOrCompute("DS_ShieldHeat40");
        private static readonly MyStringId HudIconHeat50 = MyStringId.GetOrCompute("DS_ShieldHeat50");
        private static readonly MyStringId HudIconHeat60 = MyStringId.GetOrCompute("DS_ShieldHeat60");
        private static readonly MyStringId HudIconHeat70 = MyStringId.GetOrCompute("DS_ShieldHeat70");
        private static readonly MyStringId HudIconHeat80 = MyStringId.GetOrCompute("DS_ShieldHeat80");
        private static readonly MyStringId HudIconHeat90 = MyStringId.GetOrCompute("DS_ShieldHeat90");
        private static readonly MyStringId HudIconHeat100 = MyStringId.GetOrCompute("DS_ShieldHeat100");

        private static readonly MyStringHash MPdamage = MyStringHash.GetOrCompute("MPdamage");
        private static readonly MyStringHash DelDamage = MyStringHash.GetOrCompute("DelDamage");
        private static readonly Type MissileObj = typeof(MyObjectBuilder_Missile);

        internal MyResourceSinkInfo ResourceInfo;
        internal MyResourceSinkComponent Sink;

        private static readonly MyDefinitionId GId = new MyDefinitionId(typeof(MyObjectBuilder_GasProperties), "Electricity");
        private readonly DataStructures _dataStructures = new DataStructures();
        //private readonly StructureBuilder _structureBuilder = new StructureBuilder();

        internal IMyUpgradeModule Shield => (IMyUpgradeModule)Entity;

        internal ShieldType ShieldMode;
        internal MyCubeGrid MyGrid;
        internal MyCubeBlock MyCube;
        internal MyResourceDistributorComponent MyGridDistributor;
        internal MyEntity ShieldEnt;
        private MyEntity _shellPassive;
        private MyEntity _shellActive;

        private MyParticleEffect _effect = new MyParticleEffect();

        internal readonly Spawn Spawn = new Spawn();
        internal readonly EllipsoidOxygenProvider EllipsoidOxyProvider = new EllipsoidOxygenProvider(Matrix.Zero);
        internal readonly EllipsoidSA EllipsoidSa = new EllipsoidSA(double.MinValue, double.MinValue, double.MinValue);

        internal Icosphere.Instance Icosphere;
        internal DSUtils Dsutil1 = new DSUtils();
        internal DSUtils Dsutil2 = new DSUtils();
        internal DSUtils Dsutil3 = new DSUtils();
        internal DSUtils Dsutil4 = new DSUtils();
        internal DSUtils Dsutil5 = new DSUtils();

        internal ControllerSettings DsSet;
        internal ControllerState DsState;

        internal ShieldGridComponent ShieldComp;
        internal ModulatorGridComponent ModComp;
        internal RunningAverage DpsAvg = new RunningAverage(2);

        internal MyStringId CustomDataTooltip = MyStringId.GetOrCompute("Shows an Editor for custom data to be used by scripts and mods");
        internal MyStringId CustomData = MyStringId.GetOrCompute("CustomData");
        internal MyStringId Password = MyStringId.GetOrCompute("Password");
        internal MyStringId PasswordTooltip = MyStringId.GetOrCompute("Set the shield modulation password");

        public enum ShieldType
        {
            Station,
            LargeGrid,
            SmallGrid,
            Unknown
        };
        #endregion

        #region constructors
        private MatrixD DetectionMatrix
        {
            get { return DetectMatrixOutside; }
            set
            {
                DetectMatrixOutside = value;
                DetectMatrixOutsideInv = MatrixD.Invert(value);
                DetectMatrixIn = MatrixD.Rescale(value, 1d + -6.0d / 100d);
                DetectMatrixInInv = MatrixD.Invert(DetectMatrixIn);
            }
        }
        #endregion
    }
}
