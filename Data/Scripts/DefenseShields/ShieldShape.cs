﻿using System;
using DefenseShields.Support;
using VRage.Game;
using VRageMath;

namespace DefenseShields
{
    public partial class DefenseShields
    {
        #region Shield Shape
        public void ResetShape(bool background, bool newShape = false)
        {
            if (Session.Enforced.Debug >= 1) Log.Line($"ResetShape: Mobile:{GridIsMobile} - Mode:{ShieldMode}/{DsState.State.Mode} - newShape:{newShape} - Offline:{!DsState.State.Online} - offCnt:{_offlineCnt} - blockChanged:{_blockEvent} - functional:{_functionalEvent} - Sleeping:{DsState.State.Sleeping} - Suspend:{DsState.State.Suspended} - EWorking:{ShieldComp.EmittersWorking} - ShieldId [{Shield.EntityId}]");

            if (newShape)
            {
                UpdateSubGrids();
                BlockMonitor();
                if (_blockEvent) BlockChanged(background);
                if (_shapeEvent) CheckExtents(background);
                if (GridIsMobile) _updateMobileShape = true;
                return;
            }

            if (GridIsMobile) MobileUpdate();
            else
            {
                UpdateDimensions = true;
                if (UpdateDimensions) RefreshDimensions();
            }
            Icosphere.ReturnPhysicsVerts(DetectionMatrix, ShieldComp.PhysicsOutside);
        }

        public void CreateHalfExtents()
        {
            var myAabb = MyGrid.PositionComp.LocalAABB;
            var shieldGrid = MyGrid;
            var expandedAabb = myAabb;
            if (ShieldComp.GetSubGrids.Count > 1)
            {
                foreach (var grid in ShieldComp.GetSubGrids)
                {
                    if (grid == null || grid == shieldGrid) continue;
                    var shieldMatrix = shieldGrid.PositionComp.WorldMatrixNormalizedInv;
                    var gQuaternion = Quaternion.CreateFromRotationMatrix(grid.WorldMatrix);
                    var gOriBBoxD = new MyOrientedBoundingBox(grid.PositionComp.WorldAABB.Center, grid.PositionComp.LocalAABB.HalfExtents, gQuaternion);
                    gOriBBoxD.Transform(shieldMatrix);
                    expandedAabb.Include(gOriBBoxD.GetAABB());
                }
            }

            if (DsSet.Settings.SphereFit || DsSet.Settings.FortifyShield)
            {
                var extend = DsSet.Settings.ExtendFit ? 2 : 1;
                var fortify = DsSet.Settings.FortifyShield ? 3 : 1;
                var size = expandedAabb.HalfExtents.Max() * fortify;
                var scaler = 4;
                if (shieldGrid.GridSizeEnum == MyCubeSize.Small && !DsSet.Settings.ExtendFit) scaler = 5;
                var vectorSize = new Vector3D(size, size, size);
                var fudge = shieldGrid.GridSize * scaler * extend;
                var extentsDiff = DsState.State.GridHalfExtents.LengthSquared() - vectorSize.LengthSquared();
                if (extentsDiff < -1 || extentsDiff > 1 || DsState.State.GridHalfExtents == Vector3D.Zero || !fudge.Equals(DsState.State.ShieldFudge)) DsState.State.GridHalfExtents = vectorSize;
                DsState.State.ShieldFudge = fudge;
            }
            else
            {
                DsState.State.ShieldFudge = 0f;
                var extentsDiff = DsState.State.GridHalfExtents.LengthSquared() - expandedAabb.HalfExtents.LengthSquared();
                if (extentsDiff < -1 || extentsDiff > 1 || DsState.State.GridHalfExtents == Vector3D.Zero) DsState.State.GridHalfExtents = expandedAabb.HalfExtents;
            }
        }

        private void GetShapeAdjust()
        {
            if (DsSet.Settings.SphereFit || DsSet.Settings.FortifyShield) DsState.State.EllipsoidAdjust = 1f;
            else if (!DsSet.Settings.ExtendFit) DsState.State.EllipsoidAdjust = UtilsStatic.CreateNormalFit(Shield, DsState.State.GridHalfExtents);
            else DsState.State.EllipsoidAdjust = UtilsStatic.CreateExtendedFit(Shield, DsState.State.GridHalfExtents);
        }

        private void MobileUpdate()
        {
            ShieldComp.ShieldVelocitySqr = MyGrid.Physics.LinearVelocity.LengthSquared();
            _sAvelSqr = MyGrid.Physics.AngularVelocity.LengthSquared();
            if (ShieldComp.ShieldVelocitySqr > 0.00001 || _sAvelSqr > 0.00001 || ComingOnline || _tick600 && MyGrid.Physics.IsMoving)
            {
                ShieldComp.GridIsMoving = true;
                if (DsSet.Settings.FortifyShield && Math.Sqrt(ShieldComp.ShieldVelocitySqr) > 15)
                {
                    FitChanged = true;
                    DsSet.Settings.FortifyShield = false;
                }
            }
            else ShieldComp.GridIsMoving = false;

            _shapeChanged = !DsState.State.EllipsoidAdjust.Equals(_oldEllipsoidAdjust) || !DsState.State.GridHalfExtents.Equals(_oldGridHalfExtents) || !DsState.State.ShieldFudge.Equals(_oldShieldFudge) || _updateMobileShape;
            _entityChanged = ShieldComp.GridIsMoving || ComingOnline || _shapeChanged;
            _oldGridHalfExtents = DsState.State.GridHalfExtents;
            _oldEllipsoidAdjust = DsState.State.EllipsoidAdjust;
            _oldShieldFudge = DsState.State.ShieldFudge;
            if (_entityChanged || BoundingRange <= 0) CreateShieldShape();
        }

        private void CreateShieldShape()
        {
            /*
               Basically find the point on the surface of the ellipsoid given a normal vector.
               Equation for ellipsoid is x^2/a^2 + y^2/b^2 ... = 1
               So given a normal vector x hat, y hat, z hat find the values for x y z that are parallel and satisfy that equation.
               So x = dist*x normVec, y = dist * y normVec
               a,b,c are the radii if the ellipsoid in X Y Z
               So signed distance from surface is distance(point, origin) - distance(support(norm(point-origin)), origin)
               support gives you the point on the surface in the given direction.
            */
            if (GridIsMobile)
            {
                _updateMobileShape = false;
                var shieldGridMatrix = MyGrid.WorldMatrix;
                if (_shapeChanged) CreateMobileShape();
                DetectionMatrix = _shieldShapeMatrix * shieldGridMatrix;
                DetectionCenter = MyGrid.PositionComp.WorldVolume.Center;
                _sQuaternion = Quaternion.CreateFromRotationMatrix(MyGrid.WorldMatrix);
                ShieldSphere.Center = DetectionCenter;
                ShieldSphere.Radius = ShieldSize.AbsMax();
            }
            else
            {
                var emitter = ShieldComp.StationEmitter.Emitter;
                var width = DsSet.Settings.Width;
                var height = DsSet.Settings.Height;
                var depth = DsSet.Settings.Depth;

                var wOffset = DsSet.Settings.ShieldOffset.X;
                var hOffset = DsSet.Settings.ShieldOffset.Y;
                var dOffset = DsSet.Settings.ShieldOffset.Z;

                var blockGridPosMeters = new Vector3D(emitter.Position) * MyGrid.GridSize;
                var localOffsetMeters = new Vector3D(wOffset, hOffset, dOffset) * MyGrid.GridSize; 
                var localOffsetPosMeters = localOffsetMeters + blockGridPosMeters; 
                var emitterCenter = emitter.PositionComp.GetPosition();
                var offsetLMatrix = Matrix.CreateWorld(localOffsetPosMeters, Vector3D.Forward, Vector3D.Up);

                var worldOffset = Vector3D.TransformNormal(localOffsetMeters, MyGrid.WorldMatrix); 
                var translationInWorldSpace = emitterCenter + worldOffset;

                OffsetEmitterWMatrix = MatrixD.CreateWorld(translationInWorldSpace, MyGrid.WorldMatrix.Forward, MyGrid.WorldMatrix.Up);

                DetectionCenter = OffsetEmitterWMatrix.Translation;

                var halfDistToCenter = 600 - Vector3D.Distance(DetectionCenter, emitterCenter);
                var vectorScale = new Vector3D(MathHelper.Clamp(width, 30, halfDistToCenter), MathHelper.Clamp(height, 30, halfDistToCenter), MathHelper.Clamp(depth, 30, halfDistToCenter));

                DetectionMatrix = MatrixD.Rescale(OffsetEmitterWMatrix, vectorScale);
                _shieldShapeMatrix = MatrixD.Rescale(offsetLMatrix, vectorScale);

                ShieldSize = DetectionMatrix.Scale;

                _sQuaternion = Quaternion.CreateFromRotationMatrix(OffsetEmitterWMatrix);
                ShieldSphere.Center = DetectionCenter;
                ShieldSphere.Radius = ShieldSize.AbsMax();
            }

            if (_isServer)
            {
                _pruneSphere1.Center = DetectionCenter;
                _pruneSphere2.Center = DetectionCenter;
            }
            else _clientPruneSphere.Center = DetectionCenter;

            SOriBBoxD.Center = DetectionCenter;
            SOriBBoxD.Orientation = _sQuaternion;

            if (_shapeChanged)
            {
                SOriBBoxD.HalfExtent = ShieldSize;
                ShieldAabb.Min = ShieldSize;
                ShieldAabb.Max = -ShieldSize;

                EllipsoidSa.Update(DetectMatrixOutside.Scale.X, DetectMatrixOutside.Scale.Y, DetectMatrixOutside.Scale.Z);
                BoundingRange = ShieldSize.AbsMax();

                if (_isServer)
                {
                    _pruneSphere1.Radius = BoundingRange + 3000;
                    _pruneSphere2.Radius = BoundingRange + 50;
                }
                else _clientPruneSphere.Radius = BoundingRange + 5;

                _ellipsoidSurfaceArea = EllipsoidSa.Surface;
                EllipsoidVolume = 1.333333 * Math.PI * DetectMatrixOutside.Scale.X * DetectMatrixOutside.Scale.Y * DetectMatrixOutside.Scale.Z;
                _shieldVol = DetectMatrixOutside.Scale.Volume;
                if (Session.IsServer)
                {
                    if (Session.Enforced.Debug >= 2) Log.Line($"StateUpdate: CreateShieldShape - Broadcast:{DsState.State.Message} - ShieldId [{Shield.EntityId}]");
                    ShieldChangeState();
                    ShieldComp.ShieldVolume = DetectMatrixOutside.Scale.Volume;
                }
                if (Session.Enforced.Debug >= 2) Log.Line($"CreateShape: shapeChanged - GridMobile:{GridIsMobile} - ShieldId [{Shield.EntityId}]");
            }
            if (!DsState.State.Lowered) SetShieldShape();
        }

        private void CreateMobileShape()
        {
            var shieldSize = DsState.State.GridHalfExtents * DsState.State.EllipsoidAdjust + DsState.State.ShieldFudge;
            ShieldSize = shieldSize;
            var mobileMatrix = MatrixD.CreateScale(shieldSize);
            mobileMatrix.Translation = MyGrid.PositionComp.LocalVolume.Center;
            _shieldShapeMatrix = mobileMatrix;
        }

        private void SetShieldShape()
        {
            if (_shapeChanged)
            {
                if (!_isDedicated) 
                {
                    _shellPassive.PositionComp.LocalMatrix = Matrix.Zero;  // Bug - Cannot just change X coord, so I reset first.
                    _shellActive.PositionComp.LocalMatrix = Matrix.Zero;
                    _shellPassive.PositionComp.LocalMatrix = _shieldShapeMatrix;
                    _shellActive.PositionComp.LocalMatrix = _shieldShapeMatrix;
                } 
                ShieldEnt.PositionComp.LocalMatrix = Matrix.Zero;
                ShieldEnt.PositionComp.LocalMatrix = _shieldShapeMatrix;
                ShieldEnt.PositionComp.LocalAABB = ShieldAabb;
            }
            ShieldEnt.PositionComp.SetPosition(DetectionCenter);
        }

        private void RefreshDimensions()
        {
            UpdateDimensions = false;
            _shapeChanged = true;
            CreateShieldShape();
            Icosphere.ReturnPhysicsVerts(DetectionMatrix, ShieldComp.PhysicsOutside);
        }
        #endregion
    }
}
