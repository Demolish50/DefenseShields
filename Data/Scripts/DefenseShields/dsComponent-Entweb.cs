﻿using System.Collections.Generic;
using DefenseShields.Support;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Weapons;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRage.Voxels;
using VRageMath;

namespace DefenseShields
{
    public partial class DefenseShields
    {
        #region Web Entities
        private void WebEntities()
        {
            Dsutil1.Sw.Restart();
            var pruneSphere = new BoundingSphereD(_detectionCenter, Range);
            var pruneList = new List<MyEntity>();
            MyGamePruningStructure.GetAllTopMostEntitiesInSphere(ref pruneSphere, pruneList);
            for (int i = 0; i < pruneList.Count; i++)
            {

                var ent = pruneList[i];
                if (ent == null || FriendlyCache.Contains(ent)) continue;

                var entCenter = ent.PositionComp.WorldVolume.Center;

                if (ent == _shield || ent as IMyCubeGrid == Shield.CubeGrid || ent.Physics == null || ent.MarkedForClose || ent is MyVoxelBase && !GridIsMobile
                    || ent is IMyFloatingObject || ent is IMyEngineerToolBase || double.IsNaN(entCenter.X) || ent.GetType().Name == "MyDebrisBase") continue;

                var relation = EntType(ent);
                if (relation == global::DefenseShields.DefenseShields.Ent.Ignore || relation == global::DefenseShields.DefenseShields.Ent.Friend)
                {
                    if (relation == global::DefenseShields.DefenseShields.Ent.Friend && !CustomCollision.PointInShield(ent.PositionComp.WorldVolume.Center, _detectMatrixOutsideInv)) continue;
                    FriendlyCache.Add(ent);
                    continue;
                }

                _enablePhysics = true;
                lock (_webEnts)
                {
                    EntIntersectInfo entInfo;
                    _webEnts.TryGetValue(ent, out entInfo);
                    if (entInfo != null)
                    {
                        entInfo.LastTick = _tick;
                        if (entInfo.SpawnedInside) FriendlyCache.Add(ent);
                    }
                    else
                    {
                        var inside = false;
                        if (relation == global::DefenseShields.DefenseShields.Ent.Other && CustomCollision.PointInShield(ent.PositionComp.WorldVolume.Center, _detectMatrixOutsideInv))
                        {
                            FriendlyCache.Add(ent);
                            continue;
                        }
                        if ((relation == global::DefenseShields.DefenseShields.Ent.LargeNobodyGrid || relation == global::DefenseShields.DefenseShields.Ent.SmallNobodyGrid) && CustomCollision.AllAabbInShield(ent.PositionComp.WorldAABB, _detectMatrixOutsideInv))
                        {
                            inside = true;
                            FriendlyCache.Add(ent);
                        }
                        _webEnts.Add(ent, new EntIntersectInfo(ent.EntityId, 0f, Vector3D.NegativeInfinity, _tick, _tick, relation, inside, new List<IMySlimBlock>(), new MyStorageData()));
                    }
                }
            }
            if (_enablePhysics || _shieldMoving || _gridChanged)
            {
                Icosphere.ReturnPhysicsVerts(_detectMatrixOutside, PhysicsOutside);
                Icosphere.ReturnPhysicsVerts(_detectMatrixOutside, PhysicsOutsideLow);
                Icosphere.ReturnPhysicsVerts(_detectMatrixInside, PhysicsInside);
            }
            if (_enablePhysics) MyAPIGateway.Parallel.Start(WebDispatch);

            Dsutil1.StopWatchReport($"ShieldId:{Shield.EntityId.ToString()} - Web", 3);
        }

        private void WebDispatch()
        {
            Dsutil3.Sw.Restart();
            var ep = 0;
            var ns = 0;
            var nl = 0;
            var es = 0;
            var el = 0;
            var ss = 0;
            var oo = 0;
            var vv = 0;
            var xx = 0;
            lock (_webEnts)
            {
                foreach (var webent in _webEnts.Keys)
                {
                    var entCenter = webent.PositionComp.WorldVolume.Center;
                    var entInfo = _webEnts[webent];
                    if (entInfo.LastTick != _tick || entInfo.SpawnedInside) continue;
                    if (entInfo.FirstTick == _tick && (_webEnts[webent].Relation == global::DefenseShields.DefenseShields.Ent.LargeNobodyGrid || _webEnts[webent].Relation == global::DefenseShields.DefenseShields.Ent.LargeEnemyGrid)) ((IMyCubeGrid)webent).GetBlocks(_webEnts[webent].CacheBlockList, CollectCollidableBlocks);
                    switch (_webEnts[webent].Relation)
                    {
                        case global::DefenseShields.DefenseShields.Ent.EnemyPlayer:
                            {
                                ep++;
                                if ((_count == 2 || _count == 17 || _count == 32 || _count == 47) && CustomCollision.PointInShield(entCenter, _detectMatrixOutsideInv))
                                {
                                    MyAPIGateway.Parallel.Start(() => PlayerIntersect(webent));
                                }
                                continue;
                            }
                        case global::DefenseShields.DefenseShields.Ent.SmallNobodyGrid:
                            {
                                ns++;
                                MyAPIGateway.Parallel.Start(() => SmallGridIntersect(webent));
                                continue;
                            }
                        case global::DefenseShields.DefenseShields.Ent.LargeNobodyGrid:
                            {
                                nl++;
                                MyAPIGateway.Parallel.Start(() => GridIntersect(webent));
                                continue;
                            }
                        case global::DefenseShields.DefenseShields.Ent.SmallEnemyGrid:
                            {
                                es++;
                                MyAPIGateway.Parallel.Start(() => SmallGridIntersect(webent));
                                continue;
                            }
                        case global::DefenseShields.DefenseShields.Ent.LargeEnemyGrid:
                            {
                                el++;
                                MyAPIGateway.Parallel.Start(() => GridIntersect(webent));
                                continue;
                            }
                        case global::DefenseShields.DefenseShields.Ent.Shielded:
                            {
                                ss++;
                                MyAPIGateway.Parallel.Start(() => ShieldIntersect(webent as IMyCubeGrid));
                                continue;
                            }
                        case global::DefenseShields.DefenseShields.Ent.Other:
                            {
                                oo++;
                                if (CustomCollision.PointInShield(entCenter, _detectMatrixOutsideInv))
                                {
                                    if (webent.MarkedForClose || webent.Closed) continue;
                                    if (webent is IMyMeteor) _meteorDmg.Enqueue(webent as IMyMeteor);
                                    else _missileDmg.Enqueue(webent);
                                }
                                continue;
                            }
                        case global::DefenseShields.DefenseShields.Ent.VoxelBase:
                            {
                                vv++;
                                MyAPIGateway.Parallel.Start(() => VoxelIntersect(webent as MyVoxelBase));
                                continue;
                            }
                        default:
                            xx++;
                            continue;
                    }
                }
            }

            if (Debug && _longLoop == 5 && _count == 5)
                lock (_webEnts) Log.Line($"ShieldId:{Shield.EntityId.ToString()} - friend:{FriendlyCache.Count} total:{_webEnts.Count} ep:{ep} ns:{ns} nl:{nl} es:{es} el:{el} ss:{ss} oo:{oo} vv:{vv} xx:{xx}");
            Dsutil3.StopWatchReport($"ShieldId:{Shield.EntityId.ToString()} - webDispatch", 3);
        }
        #endregion

        #region Gather Entity Information
        private global::DefenseShields.DefenseShields.Ent EntType(IMyEntity ent)
        {
            if (ent == null) return global::DefenseShields.DefenseShields.Ent.Ignore;
            if (ent is MyVoxelBase && !GridIsMobile) return global::DefenseShields.DefenseShields.Ent.Ignore;

            if (ent is IMyCharacter)
            {
                var dude = MyAPIGateway.Players.GetPlayerControllingEntity(ent)?.IdentityId;
                if (dude == null) return global::DefenseShields.DefenseShields.Ent.Ignore;
                var playerrelationship = Shield.GetUserRelationToOwner((long)dude);
                if (playerrelationship == MyRelationsBetweenPlayerAndBlock.Owner || playerrelationship == MyRelationsBetweenPlayerAndBlock.FactionShare) return global::DefenseShields.DefenseShields.Ent.Friend;
                return (ent as IMyCharacter).IsDead ? global::DefenseShields.DefenseShields.Ent.Ignore : global::DefenseShields.DefenseShields.Ent.EnemyPlayer;
            }
            if (ent is IMyCubeGrid)
            {
                var grid = ent as IMyCubeGrid;
                if (((MyCubeGrid)grid).BlocksCount < 3 && grid.BigOwners.Count == 0) return global::DefenseShields.DefenseShields.Ent.SmallNobodyGrid;
                if (grid.BigOwners.Count <= 0) return global::DefenseShields.DefenseShields.Ent.LargeNobodyGrid;

                var enemy = GridEnemy(grid);
                if (enemy && ((MyCubeGrid)grid).BlocksCount < 3) return global::DefenseShields.DefenseShields.Ent.SmallEnemyGrid;

                ShieldGridComponent shieldComponent;
                grid.Components.TryGet(out shieldComponent);
                if (shieldComponent != null && !enemy) return global::DefenseShields.DefenseShields.Ent.Friend;
                if (shieldComponent != null && !shieldComponent.DefenseShields.ShieldActive) return global::DefenseShields.DefenseShields.Ent.LargeEnemyGrid;
                if (shieldComponent != null && Entity.EntityId > shieldComponent.DefenseShields.Entity.EntityId) return global::DefenseShields.DefenseShields.Ent.Shielded;
                if (shieldComponent != null) return global::DefenseShields.DefenseShields.Ent.Ignore; //only process the higher EntityID
                return enemy ? global::DefenseShields.DefenseShields.Ent.LargeEnemyGrid : global::DefenseShields.DefenseShields.Ent.Friend;
            }

            if (ent is IMyMeteor || ent.GetType().Name.StartsWith("MyMissile")) return global::DefenseShields.DefenseShields.Ent.Other;
            if (ent is MyVoxelBase && GridIsMobile) return global::DefenseShields.DefenseShields.Ent.VoxelBase;
            return 0;
        }

        private bool GridEnemy(IMyCubeGrid grid)
        {
            var owners = grid.BigOwners;
            if (owners.Count == 0) return true;
            var relationship = Shield.GetUserRelationToOwner(owners[0]);
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

        #region Compute Missile Intersect Damage
        private float ComputeAmmoDamage(IMyEntity ammoEnt)
        {
            //bypass < 0 kickback
            //Ignores Shield entirely.
            //
            //healing < 0 mass ,  radius 0
            //Heals Shield, converting weapon damage to healing value.
            //Values as close to Zero (0) as possible, to best results, and less unintentional Results.
            //Shield-Damage: All values such as projectile Velocity & Mass for non-explosive types and Explosive-damage when dealing with Explosive-types.
            AmmoInfo ammoInfo;
            _ammoInfo.TryGetValue(ammoEnt.Model.AssetName, out ammoInfo);
            var damage = 10f;
            if (ammoInfo == null)
            {
                Log.Line($"ShieldId:{Shield.EntityId.ToString()} - No Missile Ammo Match Found for {((MyEntity)ammoEnt).DebugName}! Let wepaon mod author know their ammo definition has improper model path");
                return damage;
            }

            if (ammoInfo.BackKickForce < 0) damage = float.NegativeInfinity;
            else if (ammoInfo.Explosive) damage = (ammoInfo.Damage * (ammoInfo.Radius * 0.5f)) * 7.5f;
            else damage = ammoInfo.Mass * ammoInfo.Speed;

            if (ammoInfo.Mass < 0 && ammoInfo.Radius <= 0) damage = -damage;
            return damage;
        }
        #endregion
    }
}
