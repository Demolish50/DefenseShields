﻿using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.Entity.EntityComponents;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace DefenseShields.Support
{
    internal static class UtilsStatic
    {
        internal static Color White1 = new Color(255, 255, 255);
        internal static Color White2 = new Color(90, 118, 255);
        internal static Color White3 = new Color(47, 86, 255);
        internal static Color Blue1 = Color.Aquamarine;
        internal static Color Blue2 = new Color(0, 66, 255);
        internal static Color Blue3 = new Color(0, 7, 255, 255);
        internal static Color Blue4 = new Color(22, 0, 170);
        internal static Color Red1 = new Color(87, 0, 66);
        internal static Color Red2 = new Color(121, 0, 13);
        internal static Color Red3 = new Color(255, 0, 0);

        private static readonly Dictionary<float, float> DmgTable = new Dictionary<float, float>
        {
            [0.00000000001f] = -1f,
            [0.0000000001f] = 0.1f,
            [0.0000000002f] = 0.2f,
            [0.0000000003f] = 0.3f,
            [0.0000000004f] = 0.4f,
            [0.0000000005f] = 0.5f,
            [0.0000000006f] = 0.6f,
            [0.0000000007f] = 0.7f,
            [0.0000000008f] = 0.8f,
            [0.0000000009f] = 0.9f,
            [0.0000000010f] = 1,
            [0.0000000020f] = 2,
            [0.0000000030f] = 3,
            [0.0000000040f] = 4,
            [0.0000000050f] = 5,
            [0.0000000060f] = 6,
            [0.0000000070f] = 7,
            [0.0000000080f] = 8,
            [0.0000000090f] = 9,
            [0.0000000100f] = 10,
        };

        public static void UpdateTerminal(this MyCubeBlock block)
        {
            MyOwnershipShareModeEnum shareMode;
            long ownerId;
            if (block.IDModule != null)
            {
                ownerId = block.IDModule.Owner;
                shareMode = block.IDModule.ShareMode;
            }
            else
            {
                return;
            }
            block.ChangeOwner(ownerId, shareMode == MyOwnershipShareModeEnum.None ? MyOwnershipShareModeEnum.Faction : MyOwnershipShareModeEnum.None);
            block.ChangeOwner(ownerId, shareMode);
        }

        public static long IntPower(int x, short power)
        {
            if (power == 0) return 1;
            if (power == 1) return x;
            int n = 15;
            while ((power <<= 1) >= 0) n--;

            long tmp = x;
            while (--n > 0)
                tmp = tmp * tmp *
                      (((power <<= 1) < 0) ? x : 1);
            return tmp;
        }

        public static float ImpactFactor(MatrixD obbMatrix, Vector3 obbExtents, Vector3D impactPos, Vector3 direction)
        {
            var impactPosLcl = (Vector3)(impactPos - obbMatrix.Translation);
            var xProj = (Vector3)obbMatrix.Right;
            var yProj = (Vector3)obbMatrix.Up;
            var zProj = (Vector3)obbMatrix.Backward;

            // quick inverse transform normal: dot(xProj, pos), dot(yProj, pos), dot(zProj, pos)
            impactPosLcl = new Vector3(impactPosLcl.Dot(xProj), impactPosLcl.Dot(yProj), impactPosLcl.Dot(zProj));
            direction = new Vector3(direction.Dot(xProj), direction.Dot(yProj), direction.Dot(zProj));

            // find point outside of box along ray, then scale by inverse box size
            const float expandFactor = 25;
            var faceDirection = (impactPosLcl - direction * obbExtents.AbsMax() * expandFactor) / obbExtents;

            // dominant axis project, then sign
            // faceNormal = Vector3.Sign(Vector3.DominantAxisProjection(faceDirection));
            Vector3 faceNormal;
            if (Math.Abs(faceDirection.X) > Math.Abs(faceDirection.Y))
            {
                // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                if (Math.Abs(faceDirection.X) > Math.Abs(faceDirection.Z))
                    faceNormal = new Vector3(Math.Sign(faceDirection.X), 0, 0);
                else
                    faceNormal = new Vector3(0, 0, Math.Sign(faceDirection.Z));
            }
            else if (Math.Abs(faceDirection.Y) > Math.Abs(faceDirection.Z))
                faceNormal = new Vector3(0, Math.Sign(faceDirection.Y), 0);
            else
                faceNormal = new Vector3(0, 0, Math.Sign(faceDirection.Z));

            return Math.Abs(faceNormal.Dot(direction));
        }

        // This method only exists for consistency, so you can *always* call
        // MoreMath.Max instead of alternating between MoreMath.Max and Math.Max
        // depending on your argument count.
        public static int Max(int x, int y)
        {
            return Math.Max(x, y);
        }

        public static int Max(int x, int y, int z)
        {
            // Or inline it as x < y ? (y < z ? z : y) : (x < z ? z : x);
            // Time it before micro-optimizing though!
            return Math.Max(x, Math.Max(y, z));
        }

        public static int Max(int w, int x, int y, int z)
        {
            return Math.Max(w, Math.Max(x, Math.Max(y, z)));
        }

        public static float GetDmgMulti(float damage)
        {
            float tableVal;
            DmgTable.TryGetValue(damage, out tableVal);
            return tableVal;
        }

        public static void GetRealPlayers(Vector3D center, float radius, HashSet<long> realPlayers)
        {
            var realPlayersIdentities = new List<IMyIdentity>();
            MyAPIGateway.Players.GetAllIdentites(realPlayersIdentities, p => !string.IsNullOrEmpty(p?.DisplayName));
            var pruneSphere = new BoundingSphereD(center, radius);
            var pruneList = new List<MyEntity>();
            MyGamePruningStructure.GetAllTopMostEntitiesInSphere(ref pruneSphere, pruneList);

            foreach (var ent in pruneList)
            {
                if (ent == null || !(ent is IMyCubeGrid || ent is IMyCharacter)) continue;

                IMyPlayer player = null;

                if (ent is IMyCharacter)
                {
                    player = MyAPIGateway.Players.GetPlayerControllingEntity(ent);
                    if (player == null) continue;
                }
                else
                {
                    var playerTmp = MyAPIGateway.Players.GetPlayerControllingEntity(ent);

                    if (playerTmp?.Character != null) player = playerTmp;
                }

                if (player == null) continue;
                if (realPlayersIdentities.Contains(player.Identity)) realPlayers.Add(player.IdentityId);
            }
        }

        public static Color GetShieldColorFromFloat(float percent)
        {
            if (percent > 90) return White1;
            if (percent > 80) return White2;
            if (percent > 70) return White3;
            if (percent > 60) return Blue1;
            if (percent > 50) return Blue2;
            if (percent > 40) return Blue3;
            if (percent > 30) return Blue4;
            if (percent > 20) return Red1;
            if (percent > 10) return Red2;
            return Red3;
        }

        public static Color GetAirEmissiveColorFromDouble(double percent)
        {
            if (percent >= 80) return Color.Green;
            if (percent > 10) return Color.Yellow;
            return Color.Red;
        }

        public static long ThereCanBeOnlyOne(IMyCubeBlock shield)
        {
            if (Session.Enforced.Debug >= 1) Log.Line($"ThereCanBeOnlyOne start");
            var shieldBlocks = new List<MyCubeBlock>();
            foreach (var block in ((MyCubeGrid)shield.CubeGrid).GetFatBlocks())
            {
                if (block == null) continue;

                if (block.BlockDefinition.BlockPairName == "DS_Control" || block.BlockDefinition.BlockPairName == "DS_Control_Table")
                {
                    if (block.IsWorking) return block.EntityId;
                    shieldBlocks.Add(block);
                }
            }
            var shieldDistFromCenter = double.MinValue;
            var shieldId = long.MinValue;
            foreach (var s in shieldBlocks)
            {
                if (s == null) continue;

                var dist = Vector3D.DistanceSquared(s.PositionComp.WorldVolume.Center, shield.CubeGrid.WorldVolume.Center);
                if (dist > shieldDistFromCenter)
                {
                    shieldDistFromCenter = dist;
                    shieldId = s.EntityId;
                }
            }
            if (Session.Enforced.Debug >= 1) Log.Line($"ThereCanBeOnlyOne complete, found shield: {shieldId}");
            return shieldId;
        }

        public static bool DistanceCheck(IMyCubeBlock block, int x, double range)
        {
            if (MyAPIGateway.Session.Player.Character == null) return false;

            var pPosition = MyAPIGateway.Session.Player.Character.PositionComp.WorldVolume.Center;
            var cPosition = block.CubeGrid.PositionComp.WorldVolume.Center;
            var dist = Vector3D.DistanceSquared(cPosition, pPosition) <= (x + range) * (x + range);
            return dist;
        }

        public static void GetDefinitons()
        {
            try
            {
                var defintions = MyDefinitionManager.Static.GetAllDefinitions();
                foreach (var def in defintions)
                {
                    if (!(def is MyAmmoMagazineDefinition)) continue;
                    var ammoDef = def as MyAmmoMagazineDefinition;
                    var ammo = MyDefinitionManager.Static.GetAmmoDefinition(ammoDef.AmmoDefinitionId);
                    if (!(ammo is MyMissileAmmoDefinition)) continue;
                    var shot = ammo as MyMissileAmmoDefinition;
                    if (Session.AmmoCollection.ContainsKey(shot.MissileModelName)) continue;
                    Session.AmmoCollection.Add(shot.MissileModelName, new AmmoInfo(shot.IsExplosive, shot.MissileExplosionDamage, shot.MissileExplosionRadius, shot.DesiredSpeed, shot.MissileMass, shot.BackkickForce));
                }
                if (Session.Enforced.Debug >= 1) Log.Line($"Definitions: Session");
            }
            catch (Exception ex) { Log.Line($"Exception in GetAmmoDefinitions: {ex}"); }
        }

        public static double CreateNormalFit(IMyCubeBlock shield, Vector3D gridHalfExtents)
        {
            var blockPoints = new Vector3D[8];
            var blocks = new List<IMySlimBlock>();

            var subGrids = MyAPIGateway.GridGroups.GetGroup(shield.CubeGrid, GridLinkTypeEnum.Mechanical);
            foreach (var grid in subGrids) grid.GetBlocks(blocks);

            var bQuaternion = Quaternion.CreateFromRotationMatrix(shield.CubeGrid.WorldMatrix);
            var sqrt2 = Math.Sqrt(2);
            var sqrt3 = Math.Sqrt(3);
            const double percent = 0.1;
            var last = 0;
            var repeat = 0;
            for (int i = 0; i <= 10; i++)
            {
                var ellipsoidAdjust = MathHelper.Lerp(sqrt2, sqrt3, i * percent);

                var shieldSize = gridHalfExtents * ellipsoidAdjust;
                var mobileMatrix = MatrixD.CreateScale(shieldSize);
                mobileMatrix.Translation = shield.CubeGrid.PositionComp.LocalAABB.Center;
                var matrixInv = MatrixD.Invert(mobileMatrix * shield.CubeGrid.WorldMatrix);

                var c = 0;
                foreach (var block in blocks)
                {
                    BoundingBoxD blockBox;
                    Vector3D center;
                    block.ComputeWorldCenter(out center);
                    if (block.FatBlock != null)
                    {
                        blockBox = block.FatBlock.Model.BoundingBox;
                        blockBox.Translate(center);
                    }
                    else block.GetWorldBoundingBox(out blockBox);

                    var bOriBBoxD = new MyOrientedBoundingBoxD(center, blockBox.HalfExtents, bQuaternion);

                    bOriBBoxD.GetCorners(blockPoints, 0);
                    foreach (var point in blockPoints)
                        if (!CustomCollision.PointInShield(point, matrixInv)) c++;
                }

                if (c == last) repeat++;
                else repeat = 0;

                if (c == 0)
                {
                    return MathHelper.Lerp(sqrt2, sqrt3, i * percent);
                }
                last = c;
                if (i == 10 && repeat > 2) return MathHelper.Lerp(sqrt2, sqrt3, ((10 - repeat) + 1) * percent);
            }
            return sqrt3;
        }

        public static double CreateExtendedFit(IMyCubeBlock shield, Vector3D gridHalfExtents)
        {
            var blockPoints = new Vector3D[8];
            var blocks = new List<IMySlimBlock>();

            var subGrids = MyAPIGateway.GridGroups.GetGroup(shield.CubeGrid, GridLinkTypeEnum.Mechanical);
            foreach (var grid in subGrids) grid.GetBlocks(blocks);

            var bQuaternion = Quaternion.CreateFromRotationMatrix(shield.CubeGrid.WorldMatrix);
            var sqrt3 = Math.Sqrt(3);
            var sqrt5 = Math.Sqrt(5);
            const double percent = 0.1;
            var last = 0;
            var repeat = 0;
            for (int i = 0; i <= 10; i++)
            {
                var ellipsoidAdjust = MathHelper.Lerp(sqrt3, sqrt5, i * percent);

                var shieldSize = gridHalfExtents * ellipsoidAdjust;
                var mobileMatrix = MatrixD.CreateScale(shieldSize);
                mobileMatrix.Translation = shield.CubeGrid.PositionComp.LocalVolume.Center;
                var matrixInv = MatrixD.Invert(mobileMatrix * shield.CubeGrid.WorldMatrix);

                var c = 0;
                foreach (var block in blocks)
                {
                    BoundingBoxD blockBox;
                    Vector3D center;
                    block.ComputeWorldCenter(out center);
                    if (block.FatBlock != null)
                    {
                        blockBox = block.FatBlock.Model.BoundingBox;
                        blockBox.Translate(center);
                    }
                    else block.GetWorldBoundingBox(out blockBox);

                    var bOriBBoxD = new MyOrientedBoundingBoxD(center, blockBox.HalfExtents, bQuaternion);

                    bOriBBoxD.GetCorners(blockPoints, 0);
                    foreach (var point in blockPoints)
                        if (!CustomCollision.PointInShield(point, matrixInv)) c++;
                }

                if (c == last) repeat++;
                else repeat = 0;

                if (c == 0)
                {
                    return MathHelper.Lerp(sqrt3, sqrt5, i * percent);
                }
                last = c;
                if (i == 10 && repeat > 2) return MathHelper.Lerp(sqrt3, sqrt5, ((10 - repeat) + 1) * percent);
            }
            return sqrt5;
        }

        public static int BlockCount(IMyCubeBlock shield)
        {
            var subGrids = MyAPIGateway.GridGroups.GetGroup(shield.CubeGrid, GridLinkTypeEnum.Mechanical);
            var blockCnt = 0;
            foreach (var grid in subGrids)
            {
                blockCnt += ((MyCubeGrid) grid).BlocksCount;
            }
            return blockCnt;
        }	
        public static void PrepConfigFile()
        {
            const int baseScaler = 10;
            const float nerf = 1f;
            const float efficiency = 100f;
            const int stationRatio = 1;
            const int largeShipRate = 1;
            const int smallShipRatio = 1;
            const int disableVoxel = 0;
            const int disableGridDmg = 0;
            const int debug = 0;
            const bool altRecharge = false;
            const int version = 61;
            const float capScaler = 1f;
            const float hpsEfficiency = 0.5f;
            const float maintenanceCost = 0.5f;

            var dsCfgExists = MyAPIGateway.Utilities.FileExistsInGlobalStorage("DefenseShields.cfg");
            if (dsCfgExists)
            {
                var unPackCfg = MyAPIGateway.Utilities.ReadFileInGlobalStorage("DefenseShields.cfg");
                var unPackedData = MyAPIGateway.Utilities.SerializeFromXML<DefenseShieldsEnforcement>(unPackCfg.ReadToEnd());

                if (Session.Enforced.Debug >= 1) Log.Line($"unPackedData is: {unPackedData}\nServEnforced are: {Session.Enforced}");

                if (unPackedData.Version == version) return;
                Log.Line($"outdated config file regenerating, file version: {unPackedData.Version} - current version: {version}");
                Session.Enforced.BaseScaler = !unPackedData.BaseScaler.Equals(-1) ? unPackedData.BaseScaler : baseScaler;
                Session.Enforced.Unused = !unPackedData.Unused.Equals(-1f) ? unPackedData.Unused : nerf;
                Session.Enforced.Efficiency = !unPackedData.Efficiency.Equals(-1f) ? unPackedData.Efficiency : efficiency;
                Session.Enforced.StationRatio = !unPackedData.StationRatio.Equals(-1) ? unPackedData.StationRatio : stationRatio;
                Session.Enforced.LargeShipRatio = !unPackedData.LargeShipRatio.Equals(-1) ? unPackedData.LargeShipRatio : largeShipRate;
                Session.Enforced.SmallShipRatio = !unPackedData.SmallShipRatio.Equals(-1) ? unPackedData.SmallShipRatio : smallShipRatio;
                if (unPackedData.Version <= 58)
                {
                    Session.Enforced.Unused = 1;
                    Session.Enforced.BaseScaler = 10;
                    Session.Enforced.SmallShipRatio = 1;
                    Session.Enforced.LargeShipRatio = 1;
                    Session.Enforced.StationRatio = 1;
                }
                Session.Enforced.DisableVoxelSupport = !unPackedData.DisableVoxelSupport.Equals(-1) ? unPackedData.DisableVoxelSupport : disableVoxel;
                Session.Enforced.DisableGridDamageSupport = !unPackedData.DisableGridDamageSupport.Equals(-1) ? unPackedData.DisableGridDamageSupport : disableGridDmg;
                Session.Enforced.Debug = !unPackedData.Debug.Equals(-1) ? unPackedData.Debug : debug;
                Session.Enforced.AltRecharge = false;
                Session.Enforced.CapScaler = !unPackedData.CapScaler.Equals(-1f) ? unPackedData.CapScaler : capScaler;
                Session.Enforced.HpsEfficiency = !unPackedData.HpsEfficiency.Equals(-1f) ? unPackedData.HpsEfficiency : hpsEfficiency;
                Session.Enforced.MaintenanceCost = !unPackedData.MaintenanceCost.Equals(-1f) ? unPackedData.MaintenanceCost : maintenanceCost;

                Session.Enforced.Version = version;

                unPackedData = null;
                unPackCfg.Close();
                unPackCfg.Dispose();
                MyAPIGateway.Utilities.DeleteFileInGlobalStorage("DefenseShields.cfg");
                var newCfg = MyAPIGateway.Utilities.WriteFileInGlobalStorage("DefenseShields.cfg");
                var newData = MyAPIGateway.Utilities.SerializeToXML(Session.Enforced);
                newCfg.Write(newData);
                newCfg.Flush();
                newCfg.Close();

                if (Session.Enforced.Debug >= 1) Log.Line($"wrote modified config file - file exists: {MyAPIGateway.Utilities.FileExistsInGlobalStorage("DefenseShields.cfg")}");
            }
            else
            {
                Session.Enforced.BaseScaler = baseScaler;
                Session.Enforced.Unused = nerf;
                Session.Enforced.Efficiency = efficiency;
                Session.Enforced.StationRatio = stationRatio;
                Session.Enforced.LargeShipRatio = largeShipRate;
                Session.Enforced.SmallShipRatio = smallShipRatio;
                Session.Enforced.DisableVoxelSupport = disableVoxel;
                Session.Enforced.DisableGridDamageSupport = disableGridDmg;
                Session.Enforced.Debug = debug;
                Session.Enforced.AltRecharge = altRecharge;
                Session.Enforced.CapScaler = capScaler;
                Session.Enforced.HpsEfficiency = hpsEfficiency;
                Session.Enforced.MaintenanceCost = maintenanceCost;
                Session.Enforced.Version = version;

                var cfg = MyAPIGateway.Utilities.WriteFileInGlobalStorage("DefenseShields.cfg");
                var data = MyAPIGateway.Utilities.SerializeToXML(Session.Enforced);
                cfg.Write(data);
                cfg.Flush();
                cfg.Close();

                if (Session.Enforced.Debug >= 1) Log.Line($"wrote new config file - file exists: {MyAPIGateway.Utilities.FileExistsInGlobalStorage("DefenseShields.cfg")}");
            }
        }

        public static void ReadConfigFile()
        {
            var dsCfgExists = MyAPIGateway.Utilities.FileExistsInGlobalStorage("DefenseShields.cfg");

            if (Session.Enforced.Debug >= 1) Log.Line($"Reading config, file exists? {dsCfgExists}");

            if (!dsCfgExists) return;

            var cfg = MyAPIGateway.Utilities.ReadFileInGlobalStorage("DefenseShields.cfg");
            var data = MyAPIGateway.Utilities.SerializeFromXML<DefenseShieldsEnforcement>(cfg.ReadToEnd());
            Session.Enforced = data;

            if (Session.Enforced.Debug >= 1) Log.Line($"Writing settings to mod:\n{data}");
        }

        private static double PowerCalculation(IMyEntity breaching, IMyCubeGrid grid)
        {
            var bPhysics = breaching.Physics;
            var sPhysics = grid.Physics;

            const double wattsPerNewton = (3.36e6 / 288000);
            var velTarget = sPhysics.GetVelocityAtPoint(breaching.Physics.CenterOfMassWorld);
            var accelLinear = sPhysics.LinearAcceleration;
            var velTargetNext = velTarget + accelLinear * MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS;
            var velModifyNext = bPhysics.LinearVelocity;
            var linearImpulse = bPhysics.Mass * (velTargetNext - velModifyNext);
            var powerCorrectionInJoules = wattsPerNewton * linearImpulse.Length();

            return powerCorrectionInJoules * MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS;
        }

        private const string OB = @"<?xml version=""1.0"" encoding=""utf-16""?>
<MyObjectBuilder_Cockpit xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
   <SubtypeName>LargeBlockCockpit</SubtypeName>
   <Owner>0</Owner>
   <CustomName>Control Stations</CustomName>
</MyObjectBuilder_Cockpit> ";

        private static Random _random = new Random();
        /// <summary>
        /// Hacky way to get the ResourceDistributorComponent from a grid
        /// without benefit of the GridSystems.
        /// <para>Unfriendly to performance. Use sparingly and cache result.</para>
        /// </summary>
        /// <param name="grid"></param>
        /// <returns></returns>
        public static MyResourceDistributorComponent GetDistributor(MyCubeGrid grid)
        {
            if (grid == null || !grid.CubeBlocks.Any())
                return null;

            //attempt to grab the distributor from an extant ship controller
            var controller = grid.GetFatBlocks().FirstOrDefault(b => (b as MyShipController)?.GridResourceDistributor != null);
            if (controller != null)
                return ((MyShipController)controller).GridResourceDistributor;
            //didn't find a controller, so let's make one

            var ob = MyAPIGateway.Utilities.SerializeFromXML<MyObjectBuilder_Cockpit>(OB);
            //assign a random entity ID and hope we don't get collisions
            ob.EntityId = _random.Next(int.MinValue, int.MaxValue);
            //block position to something that will probably not have a block there already
            ob.Min = grid.WorldToGridInteger(grid.PositionComp.WorldAABB.Min) - new Vector3I(2);
            //note that this will slightly inflate the grid's boundingbox, but the Raze call later triggers a bounds recalc in 30 seconds

            //not exposed in the class but is in the interface???
            //also not synced
            var blk = ((IMyCubeGrid)grid).AddBlock(ob, false);
            var distributor = (blk.FatBlock as MyShipController)?.GridResourceDistributor;
            //hack to make it work on clients (removal not synced)
            grid.RazeBlocksClient(new List<Vector3I>() { blk.Position });
            //we don't need the block itself, we grabbed the distributor earlier
            blk.FatBlock?.Close();

            return distributor;
        }

        public static void CreateExplosion(Vector3D position, float radius, int damage = 5000)
        {
            MyExplosionTypeEnum explosionTypeEnum = MyExplosionTypeEnum.WARHEAD_EXPLOSION_50;
            if ((double)radius < 2.0)
                explosionTypeEnum = MyExplosionTypeEnum.WARHEAD_EXPLOSION_02;
            else if ((double)radius < 15.0)
                explosionTypeEnum = MyExplosionTypeEnum.WARHEAD_EXPLOSION_15;
            else if ((double)radius < 30.0)
                explosionTypeEnum = MyExplosionTypeEnum.WARHEAD_EXPLOSION_30;
            MyExplosionInfo explosionInfo = new MyExplosionInfo()
            {
                PlayerDamage = 0.0f,
                Damage = (float)damage,
                ExplosionType = explosionTypeEnum,
                ExplosionSphere = new BoundingSphereD(position, (double)radius),
                LifespanMiliseconds = 700,
                ParticleScale = 1f,
                Direction = new Vector3?(Vector3.Down),
                VoxelExplosionCenter = position,
                ExplosionFlags = MyExplosionFlags.CREATE_DEBRIS | MyExplosionFlags.AFFECT_VOXELS | MyExplosionFlags.APPLY_FORCE_AND_DAMAGE | MyExplosionFlags.CREATE_DECALS | MyExplosionFlags.CREATE_PARTICLE_EFFECT | MyExplosionFlags.CREATE_SHRAPNELS | MyExplosionFlags.APPLY_DEFORMATION,
                VoxelCutoutScale = 1f,
                PlaySound = true,
                ApplyForceAndDamage = true,
                ObjectsRemoveDelayInMiliseconds = 40
            };
            MyExplosions.AddExplosion(ref explosionInfo, true);
        }

        public static void CreateFakeSmallExplosion(Vector3D position)
        {
            const MyExplosionTypeEnum explosionTypeEnum = MyExplosionTypeEnum.WARHEAD_EXPLOSION_02;
            MyExplosionInfo explosionInfo = new MyExplosionInfo()
            {
                PlayerDamage = 0.0f,
                Damage = 0f,
                ExplosionType = explosionTypeEnum,
                ExplosionSphere = new BoundingSphereD(position, 0d),
                LifespanMiliseconds = 0,
                ParticleScale = 1f,
                Direction = Vector3.Down,
                VoxelExplosionCenter = position,
                ExplosionFlags = MyExplosionFlags.CREATE_PARTICLE_EFFECT,
                VoxelCutoutScale = 0f,
                PlaySound = true,
                ApplyForceAndDamage = false,
                ObjectsRemoveDelayInMiliseconds = 0
            };
            MyExplosions.AddExplosion(ref explosionInfo, true);
        }
    }
}
