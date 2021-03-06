﻿using System.Collections.Generic;
using DefenseShields.Support;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Weapons;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;

namespace DefenseShields
{
    public partial class DefenseShields
    {
        #region Web Entities
        private void WebEntities()
        {
            _pruneList.Clear();
            MyGamePruningStructure.GetAllTopMostEntitiesInSphere(ref _pruneSphere2, _pruneList);

            foreach (var eShield in EnemyShields) _pruneList.Add(eShield);
            foreach (var missile in Missiles)
                if (missile.InScene && !missile.MarkedForClose && _pruneSphere2.Intersects(missile.PositionComp.WorldVolume)) _pruneList.Add(missile);

            var disableVoxels = Session.Enforced.DisableVoxelSupport == 1 || ShieldComp.Modulator == null || ShieldComp.Modulator.ModSet.Settings.ModulateVoxels;
            var entChanged = false;
            _enablePhysics = false;
            for (int i = 0; i < _pruneList.Count; i++)
            {
                var ent = _pruneList[i];
                var voxel = ent as MyVoxelBase;
                if (ent == null || ent.MarkedForClose || voxel == null && ent.Physics == null || !GridIsMobile && voxel != null || disableVoxels && voxel != null || voxel != null && voxel != voxel.RootVoxel) continue;
                var entCenter = ent.PositionComp.WorldVolume.Center;
                if (FriendlyCache.Contains(ent) || IgnoreCache.Contains(ent) || PartlyProtectedCache.Contains(ent) || AuthenticatedCache.Contains(ent) || ent is IMyFloatingObject || ent is IMyEngineerToolBase || double.IsNaN(entCenter.X) || ent.GetType().Name == "MyDebrisBase") continue;
                if (ent.DefinitionId.HasValue && ent.DefinitionId.Value.TypeId == MissileObj && FriendlyMissileCache.Contains(ent)) continue;
                EntIntersectInfo entInfo;
                WebEnts.TryGetValue(ent, out entInfo);

                Ent relation;
                if (entInfo != null)
                {
                    if (_tick600) entInfo.Relation = EntType(ent);
                    relation = entInfo.Relation;
                }
                else relation = EntType(ent);

                switch (relation)
                {
                    case Ent.Authenticated:
                        continue;
                    case Ent.Ignore:
                    case Ent.Friend:
                        if (relation == Ent.Friend) 
                        {
                            var grid = ent as MyCubeGrid;
                            if (grid != null)
                            {
                                if (ShieldEnt.PositionComp.WorldVolume.Intersects(grid.PositionComp.WorldVolume))
                                {
                                    var cornersInShield = CustomCollision.NotAllCornersInShield(grid, DetectMatrixOutsideInv);
                                    if (cornersInShield > 0 && cornersInShield != 8) PartlyProtectedCache.Add(ent);
                                    else if (cornersInShield == 8) FriendlyCache.Add(ent);
                                }
                            }
                            else if (CustomCollision.PointInShield(ent.PositionComp.WorldVolume.Center, DetectMatrixOutsideInv))
                            {
                                FriendlyCache.Add(ent);
                                continue;
                            }
                            IgnoreCache.Add(ent);
                        }
                        continue;
                }
                if (entInfo != null)
                {
                    var interestingEnts = relation == Ent.LargeEnemyGrid || relation == Ent.LargeNobodyGrid || relation == Ent.SmallEnemyGrid || relation == Ent.SmallNobodyGrid || relation == Ent.Shielded;
                    if (ent.Physics != null && ent.Physics.IsMoving) entChanged = true;
                    else if (entInfo.Touched || _count == 0 && interestingEnts && !ent.PositionComp.LocalAABB.Equals(entInfo.Box))
                    {
                        entInfo.Box = ent.PositionComp.LocalAABB;
                        entChanged = true;
                    }

                    _enablePhysics = true;
                    entInfo.LastTick = _tick;
                    if (_tick600)
                    {
                        if ((relation == Ent.LargeEnemyGrid || relation == Ent.LargeNobodyGrid) && entInfo.CacheBlockList.Count != (ent as MyCubeGrid).BlocksCount)
                        {
                            entInfo.RefreshTick = _tick;
                            entInfo.CacheBlockList.Clear();
                        }
                    }
                }
                else
                {
                    if (relation == Ent.Other)
                    {
                        var missilePast = -Vector3D.Normalize(ent.Physics.LinearVelocity) * 6;
                        var missileTestLoc = ent.PositionComp.WorldVolume.Center + missilePast;
                        var centerStep = -Vector3D.Normalize(missileTestLoc - DetectionCenter) * 2f;
                        var counterDrift = centerStep + missileTestLoc;
                        if (CustomCollision.PointInShield(counterDrift, DetectMatrixOutsideInv))
                        {
                            FriendlyMissileCache.Add(ent);
                            continue;
                        }
                    }
                    if ((relation == Ent.LargeNobodyGrid || relation == Ent.SmallNobodyGrid) && CustomCollision.AllAabbInShield(ent.PositionComp.WorldAABB, DetectMatrixOutsideInv))
                    {
                        FriendlyCache.Add(ent);
                        EntIntersectInfo gridRemoved;
                        WebEnts.TryRemove(ent, out gridRemoved);
                        continue;
                    }
                    entChanged = true;
                    _enablePhysics = true;
                    WebEnts.TryAdd(ent, new EntIntersectInfo(ent.EntityId, 0f, 0f, false, ent.PositionComp.LocalAABB, Vector3D.NegativeInfinity, Vector3D.NegativeInfinity, _tick, _tick, _tick, relation, new List<IMySlimBlock>()));
                }
            }

            if (!_enablePhysics) return;
            ShieldMatrix = ShieldEnt.PositionComp.WorldMatrix;
            if (!ShieldMatrix.EqualsFast(ref OldShieldMatrix))
            {
                OldShieldMatrix = ShieldMatrix;
                Icosphere.ReturnPhysicsVerts(DetectMatrixOutside, ShieldComp.PhysicsOutside);
                if (!disableVoxels) Icosphere.ReturnPhysicsVerts(DetectMatrixOutside, ShieldComp.PhysicsOutsideLow);
            }
            if (ShieldComp.GridIsMoving || entChanged) MyAPIGateway.Parallel.Start(WebDispatch);
        }

        private void WebDispatch()
        {
            foreach (var webent in WebEnts.Keys)
            {
                var entCenter = webent.PositionComp.WorldVolume.Center;
                var entInfo = WebEnts[webent];
                if (entInfo.LastTick != _tick) continue;
                if (entInfo.RefreshTick == _tick && (WebEnts[webent].Relation == Ent.LargeNobodyGrid || WebEnts[webent].Relation == Ent.LargeEnemyGrid))
                    (webent as IMyCubeGrid)?.GetBlocks(WebEnts[webent].CacheBlockList, CollectCollidableBlocks);
                switch (WebEnts[webent].Relation)
                {
                    case Ent.EnemyPlayer:
                        {
                            if ((_count == 2 || _count == 17 || _count == 32 || _count == 47) && CustomCollision.PointInShield(entCenter, DetectMatrixOutsideInv))
                            {
                                if (Session.Enforced.Debug >= 2) Log.Line($"Ent EnemyPlayer: {webent.DebugName} - ShieldId [{Shield.EntityId}]");
                                MyAPIGateway.Parallel.Start(() => PlayerIntersect(webent));
                            }
                            continue;
                        }
                    case Ent.SmallNobodyGrid:
                        {
                            if (Session.Enforced.Debug >= 2) Log.Line($"Ent SmallNobodyGrid: {webent.DebugName} - ShieldId [{Shield.EntityId}]");
                            MyAPIGateway.Parallel.Start(() => SmallGridIntersect(webent));
                            continue;
                        }
                    case Ent.LargeNobodyGrid:
                        {
                            if (Session.Enforced.Debug >= 2) Log.Line($"Ent LargeNobodyGrid: {webent.DebugName} - ShieldId [{Shield.EntityId}]");
                            MyAPIGateway.Parallel.Start(() => GridIntersect(webent));
                            continue;
                        }
                    case Ent.SmallEnemyGrid:
                        {
                            if (Session.Enforced.Debug >= 2) Log.Line($"Ent SmallEnemyGrid: {webent.DebugName} - ShieldId [{Shield.EntityId}]");
                            MyAPIGateway.Parallel.Start(() => SmallGridIntersect(webent));
                            continue;
                        }
                    case Ent.LargeEnemyGrid:
                        {
                            if (Session.Enforced.Debug >= 2) Log.Line($"Ent LargeEnemyGrid: {webent.DebugName} - ShieldId [{Shield.EntityId}]");
                            MyAPIGateway.Parallel.Start(() => GridIntersect(webent));
                            continue;
                        }
                    case Ent.Shielded:
                        {
                            if (Session.Enforced.Debug >= 2) Log.Line($"Ent Shielded: {webent.DebugName} - ShieldId [{Shield.EntityId}]");
                            MyAPIGateway.Parallel.Start(() => ShieldIntersect(webent));
                            continue;
                        }
                    case Ent.Other:
                        {
                            if (Session.Enforced.Debug >= 2) Log.Line($"Ent Other: {webent.DebugName} - ShieldId [{Shield.EntityId}]");
                            if (webent.MarkedForClose || !webent.InScene || webent.Closed) continue;
                            var meteor = webent as IMyMeteor;
                            if (meteor != null)
                            {
                                if (CustomCollision.PointInShield(entCenter, DetectMatrixOutsideInv)) _meteorDmg.Enqueue(meteor);
                            }
                            else
                            {
                                var predictedHit = CustomCollision.MissileIntersect(this, webent, DetectionMatrix, DetectMatrixOutsideInv);
                                if (predictedHit != null) _missileDmg.Enqueue(webent);
                            }
                            continue;
                        }
                    case Ent.VoxelBase:
                        {
                            if (Session.Enforced.Debug >= 2) Log.Line($"Ent VoxelBase: {webent.DebugName} - ShieldId [{Shield.EntityId}]");
                            MyAPIGateway.Parallel.Start(() => VoxelIntersect(webent as MyVoxelBase));
                            continue;
                        }
                    default:
                        if (Session.Enforced.Debug >= 2) Log.Line($"Ent default: {webent.DebugName} - ShieldId [{Shield.EntityId}]");
                        continue;
                }
            }
        }
        #endregion

        #region Gather Entity Information
        public enum Ent
        {
            Unknown,
            Ignore,
            Friend,
            EnemyPlayer,
            SmallNobodyGrid,
            LargeNobodyGrid,
            SmallEnemyGrid,
            LargeEnemyGrid,
            Shielded,
            Other,
            VoxelBase,
            Weapon,
            Authenticated
        };

        private Ent EntType(MyEntity ent)
        {
            if (ent == null) return Ent.Ignore;
            var voxel = ent as MyVoxelBase;
            if (voxel != null && (Session.Enforced.DisableVoxelSupport == 1 || ShieldComp.Modulator == null || ShieldComp.Modulator.ModSet.Settings.ModulateVoxels || !GridIsMobile)) return Ent.Ignore;

            var player = ent as IMyCharacter;
            if (player != null)
            {
                var dude = MyAPIGateway.Players.GetPlayerControllingEntity(ent)?.IdentityId;
                if (dude == null) return Ent.Ignore;
                var playerrelationship = MyCube.GetUserRelationToOwner((long)dude);
                if (playerrelationship == MyRelationsBetweenPlayerAndBlock.Owner || playerrelationship == MyRelationsBetweenPlayerAndBlock.FactionShare) return Ent.Friend;
                return player.IsDead ? Ent.Ignore : Ent.EnemyPlayer;
            }
            var grid = ent as MyCubeGrid;
            if (grid != null)
            {
                if (ShieldComp.Modulator != null && ShieldComp.Modulator.ModSet.Settings.ModulateGrids || Session.Enforced.DisableGridDamageSupport == 1) return Ent.Ignore;

                ModulatorGridComponent modComp;
                grid.Components.TryGet(out modComp);
                if (!string.IsNullOrEmpty(modComp?.ModulationPassword) && modComp.ModulationPassword == Shield.CustomData)
                {
                    foreach (var subGrid in modComp.GetSubGrids)
                    {
                        if (ShieldEnt.PositionComp.WorldVolume.Intersects(grid.PositionComp.WorldVolume))
                        {
                            var cornersInShield = CustomCollision.NotAllCornersInShield(grid, DetectMatrixOutsideInv);
                            if (cornersInShield > 0 && cornersInShield != 8) PartlyProtectedCache.Add(subGrid);
                            else if (cornersInShield == 8) FriendlyCache.Add(subGrid);
                            else AuthenticatedCache.Add(subGrid);
                        }
                        else AuthenticatedCache.Add(subGrid);
                    }
                    return Ent.Authenticated;
                }
                var bigOwners = grid.BigOwners.Count;
                var blockCnt = grid.BlocksCount;
                if (blockCnt < 10 && bigOwners == 0) return Ent.SmallNobodyGrid;
                if (bigOwners == 0) return Ent.LargeNobodyGrid;
                var enemy = GridEnemy(grid);

                ShieldGridComponent shieldComponent;
                grid.Components.TryGet(out shieldComponent);
                if (shieldComponent?.DefenseShields?.ShieldComp != null && shieldComponent.DefenseShields.WasOnline)
                {
                    var dsComp = shieldComponent.DefenseShields;
                    var shieldEntity = MyCube.Parent;
                    if (!enemy) return Ent.Friend;
                    dsComp.EnemyShields.Add(shieldEntity);
                    return Ent.Shielded;    
                }
                return enemy ? Ent.LargeEnemyGrid : Ent.Friend;
            }

            if (ent is IMyMeteor || ent.DefinitionId.HasValue && ent.DefinitionId.Value.TypeId == MissileObj) return Ent.Other;
            if (voxel != null && GridIsMobile) return Ent.VoxelBase;
            return 0;
        }

        private bool GridEnemy(MyCubeGrid grid)
        {
            var owners = grid.BigOwners;
            if (owners.Count == 0) return true;
            var relationship = MyCube.GetUserRelationToOwner(owners[0]);
            var enemy = relationship != MyRelationsBetweenPlayerAndBlock.Owner && relationship != MyRelationsBetweenPlayerAndBlock.FactionShare;
            return enemy;
        }

        private static bool CollectCollidableBlocks(IMySlimBlock mySlimBlock)
        {
            return mySlimBlock.BlockDefinition.Id.TypeId != typeof(MyObjectBuilder_TextPanel)
                   && mySlimBlock.BlockDefinition.Id.TypeId != typeof(MyObjectBuilder_ButtonPanel)
                   && mySlimBlock.BlockDefinition.Id.SubtypeId != MyStringHash.TryGet("SmallLight");
        }
        #endregion
    }
}
