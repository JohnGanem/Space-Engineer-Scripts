using System;
using System.Collections.Generic;
using System.Text.Json;

// Space Engineers DLLs
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using Sandbox.Game.EntityComponents;
using VRageMath;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using SpaceEngineers.Game.ModAPI.Ingame;

/*
 * Must be unique per each script project.
 * Prevents collisions of multiple `class Program` declarations.
 * Will be used to detect the ingame script region, whose name is the same.
 */
namespace LineManager
{
    /*
     * Do not change this declaration because this is the game requirement.
     */
    public sealed class Program : MyGridProgram
    {
        /*
         * Must be same as the namespace. Will be used for automatic script export.
         * The code inside this region is the ingame script.
         */

        #region LineManager

        int runs_between_loops = 5; // 18= 30 seconds
        int runs_passed = 0;

        IMyRadioAntenna antenna = null;

        List<MARKETPLACE> marketplaces = new List<MARKETPLACE>();

        MARKETPLACE actual_marketplace = null;
        LINE_STATUS actual_line_status = null;

        public WcPbApi WCApi = new WcPbApi();

        public Program()
        {
            antenna = getMyAntenna();
            WCApi.Activate(Me);

            MARKETPLACE Ceres = new MARKETPLACE(
                new Vector3(-1464.95, 1995528.82, 61466.44),
                5000,
                10000,
                new List<ITEM_TO_SELL>() { new ITEM_TO_SELL("Ice", 4000000, 100000) }
            );
            MARKETPLACE TestMarketplace = new MARKETPLACE(
                new Vector3(68662.05, -236724.8, 19868.93), // It's in creative don't go there fool
                1000,
                1500,
                new List<ITEM_TO_SELL>() { new ITEM_TO_SELL("Ice", 1000, 500) }
            );

            marketplaces.Add(Ceres);
            marketplaces.Add(TestMarketplace);

            Runtime.UpdateFrequency = UpdateFrequency.Update100;
            routine();
        }

        public void Main(string argument, UpdateType updateSource)
        {
            if (antenna == null)
            {
                antenna = getMyAntenna();
                if (antenna == null)
                {
                    Echo("No Antenna with \"Line\" in the name.");
                    return;
                }
            }

            if (updateSource == UpdateType.Update100 && runs_passed++ >= runs_between_loops)
            {
                runs_passed = 0;
                routine();
            }
        }

        void routine()
        {
            if (actual_marketplace == null)
            {
                actual_marketplace = enteredMarketplace();
                if (actual_marketplace != null)
                {
                    standInLine(1); // ToDo : only if you have the item to sell in cargo and calculate refresh number
                }
            }
            else if (leftMarketplace())
                quitTheLine();
            else
                refreshLineStatus();
        }

        void standInLine(float myRefreshNeeded)
        {
            Me.CustomData = Me.CubeGrid.CustomName;
            antenna.Enabled = true;
            antenna.EnableBroadcasting = true;
            antenna.Radius = 50000f;
            actual_line_status = new LINE_STATUS(myRefreshNeeded);
        }

        void quitTheLine()
        {
            if (actual_line_status != null)
            {
                actual_line_status = null;
            }
            antenna.Enabled = false;
            antenna.EnableBroadcasting = false;
            antenna.Radius = 0;
        }

        IMyRadioAntenna getMyAntenna()
        {
            List<IMyRadioAntenna> antennas = new List<IMyRadioAntenna>();
            GridTerminalSystem.GetBlocksOfType<IMyRadioAntenna>(antennas);

            for (int i = 0; i < antennas.Count; i++)
                if (antennas[i].IsFunctional && antennas[i].CustomName.ToString().Contains("Line"))
                {
                    Me.CustomData = antennas[i].CustomNameWithFaction.ToString();
                    return antennas[i] as IMyRadioAntenna;
                }
            return null;
        }

        MARKETPLACE enteredMarketplace()
        {
            if (antenna != null)
            {
                Vector3 currentPos = antenna.GetPosition();
                foreach (var marketplace in marketplaces)
                {
                    if (
                        Vector3.Distance(currentPos, marketplace.location)
                        <= marketplace.innerRadius
                    )
                        return marketplace;
                }
            }
            return null;
        }

        public void DisplayBlockProperties(IMyTerminalBlock BlockToCheck)
        {
            List<Sandbox.ModAPI.Interfaces.ITerminalAction> PropList =
                new List<Sandbox.ModAPI.Interfaces.ITerminalAction>();
            BlockToCheck.GetActions(PropList);
            foreach (Sandbox.ModAPI.Interfaces.ITerminalAction Prop in PropList)
            {
                Me.CustomData += Prop.Name + " " + Prop.Id + "\n";
            }
        }

        bool leftMarketplace()
        {
            if (antenna != null && actual_marketplace != null)
            {
                Vector3 currentPos = antenna.GetPosition();
                if (
                    Vector3.Distance(currentPos, actual_marketplace.location)
                    > actual_marketplace.outerRadius
                )
                    return true;
            }
            return false;
        }

        void refreshLineStatus()
        {
            if (actual_line_status != null)
            {
                if (antenna != null)
                    antenna.HudText =
                        "I am " + actual_line_status.myPosition + "/" + actual_line_status.lineSize;
            }
        }

        public class MARKETPLACE
        {
            public Vector3 location;
            public double innerRadius;
            public double outerRadius;

            public List<ITEM_TO_SELL> itemsToSell;

            public MARKETPLACE(
                Vector3 location,
                double innerRadius,
                double outerRadius,
                List<ITEM_TO_SELL> itemsToSell
            )
            {
                this.location = location;
                this.innerRadius = innerRadius;
                this.outerRadius = outerRadius;
                this.itemsToSell = itemsToSell;
            }
        }

        public class ITEM_TO_SELL
        {
            public string name;
            public int refresh_quantity;
            public int minimum_quantity_for_lining;

            public ITEM_TO_SELL(string name, int refresh_quantity, int minimum_quantity_for_lining)
            {
                this.name = name;
                this.refresh_quantity = refresh_quantity;
                this.minimum_quantity_for_lining = minimum_quantity_for_lining;
            }
        }

        public class LINE_STATUS
        {
            public int myPosition = 0;
            public int lineSize = 0;
            public float myRefreshLeft;

            public LINE_STATUS(int myPosition, int lineSize, float myRefreshLeft)
            {
                this.myPosition = myPosition;
                this.lineSize = lineSize;
                this.myRefreshLeft = myRefreshLeft;
            }

            public LINE_STATUS(float myRefreshLeft)
            {
                this.myRefreshLeft = myRefreshLeft;
            }

            public string serialize()
            {
                return this.myPosition + ";" + this.lineSize + ";" + this.myRefreshLeft;
            }

            // public static LINE_STATUS unserialize(string serializedString)
            // {
            //     string[] explodedString = serializedString.Split(";");
            //     if (explodedString.Length == 3 && )
            //     {
            //         this.lineSize = +";" + this.myRefreshLeft;
            //         LINE_STATUS lineStatus = new LINE_STATUS();
            //     }
            // }
        }

        public class WcPbApi
        {
            private Action<ICollection<MyDefinitionId>> _getCoreWeapons;
            private Action<ICollection<MyDefinitionId>> _getCoreStaticLaunchers;
            private Action<ICollection<MyDefinitionId>> _getCoreTurrets;
            private Func<
                Sandbox.ModAPI.Ingame.IMyTerminalBlock,
                IDictionary<string, int>,
                bool
            > _getBlockWeaponMap;
            private Func<long, MyTuple<bool, int, int>> _getProjectilesLockedOn;
            private Action<
                Sandbox.ModAPI.Ingame.IMyTerminalBlock,
                IDictionary<MyDetectedEntityInfo, float>
            > _getSortedThreats;
            private Action<
                Sandbox.ModAPI.Ingame.IMyTerminalBlock,
                ICollection<Sandbox.ModAPI.Ingame.MyDetectedEntityInfo>
            > _getObstructions;
            private Func<long, int, MyDetectedEntityInfo> _getAiFocus;
            private Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, long, int, bool> _setAiFocus;
            private Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, long, bool> _releaseAiFocus;
            private Func<
                Sandbox.ModAPI.Ingame.IMyTerminalBlock,
                int,
                MyDetectedEntityInfo
            > _getWeaponTarget;
            private Action<Sandbox.ModAPI.Ingame.IMyTerminalBlock, long, int> _setWeaponTarget;
            private Action<Sandbox.ModAPI.Ingame.IMyTerminalBlock, bool, int> _fireWeaponOnce;
            private Action<
                Sandbox.ModAPI.Ingame.IMyTerminalBlock,
                bool,
                bool,
                int
            > _toggleWeaponFire;
            private Func<
                Sandbox.ModAPI.Ingame.IMyTerminalBlock,
                int,
                bool,
                bool,
                bool
            > _isWeaponReadyToFire;
            private Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, int, float> _getMaxWeaponRange;
            private Func<
                Sandbox.ModAPI.Ingame.IMyTerminalBlock,
                ICollection<string>,
                int,
                bool
            > _getTurretTargetTypes;
            private Action<
                Sandbox.ModAPI.Ingame.IMyTerminalBlock,
                ICollection<string>,
                int
            > _setTurretTargetTypes;
            private Action<Sandbox.ModAPI.Ingame.IMyTerminalBlock, float> _setBlockTrackingRange;
            private Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, long, int, bool> _isTargetAligned;
            private Func<
                Sandbox.ModAPI.Ingame.IMyTerminalBlock,
                long,
                int,
                MyTuple<bool, Vector3D?>
            > _isTargetAlignedExtended;
            private Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, long, int, bool> _canShootTarget;
            private Func<
                Sandbox.ModAPI.Ingame.IMyTerminalBlock,
                long,
                int,
                Vector3D?
            > _getPredictedTargetPos;
            private Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, float> _getHeatLevel;
            private Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, float> _currentPowerConsumption;
            private Func<MyDefinitionId, float> _getMaxPower;
            private Func<long, bool> _hasGridAi;
            private Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, bool> _hasCoreWeapon;
            private Func<long, float> _getOptimalDps;
            private Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, int, string> _getActiveAmmo;
            private Action<Sandbox.ModAPI.Ingame.IMyTerminalBlock, int, string> _setActiveAmmo;
            private Action<
                Sandbox.ModAPI.Ingame.IMyTerminalBlock,
                int,
                Action<long, int, ulong, long, Vector3D, bool>
            > _monitorProjectile;
            private Action<
                Sandbox.ModAPI.Ingame.IMyTerminalBlock,
                int,
                Action<long, int, ulong, long, Vector3D, bool>
            > _unMonitorProjectile;
            private Func<
                ulong,
                MyTuple<Vector3D, Vector3D, float, float, long, string>
            > _getProjectileState;
            private Func<long, float> _getConstructEffectiveDps;
            private Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, long> _getPlayerController;
            private Func<
                Sandbox.ModAPI.Ingame.IMyTerminalBlock,
                int,
                Matrix
            > _getWeaponAzimuthMatrix;
            private Func<
                Sandbox.ModAPI.Ingame.IMyTerminalBlock,
                int,
                Matrix
            > _getWeaponElevationMatrix;
            private Func<
                Sandbox.ModAPI.Ingame.IMyTerminalBlock,
                long,
                bool,
                bool,
                bool
            > _isTargetValid;
            private Func<
                Sandbox.ModAPI.Ingame.IMyTerminalBlock,
                int,
                MyTuple<Vector3D, Vector3D>
            > _getWeaponScope;
            private Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, MyTuple<bool, bool>> _isInRange;
            private Action<
                Sandbox.ModAPI.Ingame.IMyTerminalBlock,
                int,
                Action<int, bool>
            > _monitorEvents;
            private Action<
                Sandbox.ModAPI.Ingame.IMyTerminalBlock,
                int,
                Action<int, bool>
            > _unmonitorEvents;

            public bool Activate(Sandbox.ModAPI.Ingame.IMyTerminalBlock pbBlock)
            {
                var dict = pbBlock
                    .GetProperty("WcPbAPI")
                    ?.As<IReadOnlyDictionary<string, Delegate>>()
                    .GetValue(pbBlock);
                if (dict == null)
                    throw new Exception("WcPbAPI failed to activate");
                return ApiAssign(dict);
            }

            public bool ApiAssign(IReadOnlyDictionary<string, Delegate> delegates)
            {
                if (delegates == null)
                    return false;

                AssignMethod(delegates, "GetCoreWeapons", ref _getCoreWeapons);
                AssignMethod(delegates, "GetCoreStaticLaunchers", ref _getCoreStaticLaunchers);
                AssignMethod(delegates, "GetCoreTurrets", ref _getCoreTurrets);
                AssignMethod(delegates, "GetBlockWeaponMap", ref _getBlockWeaponMap);
                AssignMethod(delegates, "GetProjectilesLockedOn", ref _getProjectilesLockedOn);
                AssignMethod(delegates, "GetSortedThreats", ref _getSortedThreats);
                AssignMethod(delegates, "GetObstructions", ref _getObstructions);
                AssignMethod(delegates, "GetAiFocus", ref _getAiFocus);
                AssignMethod(delegates, "SetAiFocus", ref _setAiFocus);
                AssignMethod(delegates, "ReleaseAiFocus", ref _releaseAiFocus);
                AssignMethod(delegates, "GetWeaponTarget", ref _getWeaponTarget);
                AssignMethod(delegates, "SetWeaponTarget", ref _setWeaponTarget);
                AssignMethod(delegates, "FireWeaponOnce", ref _fireWeaponOnce);
                AssignMethod(delegates, "ToggleWeaponFire", ref _toggleWeaponFire);
                AssignMethod(delegates, "IsWeaponReadyToFire", ref _isWeaponReadyToFire);
                AssignMethod(delegates, "GetMaxWeaponRange", ref _getMaxWeaponRange);
                AssignMethod(delegates, "GetTurretTargetTypes", ref _getTurretTargetTypes);
                AssignMethod(delegates, "SetTurretTargetTypes", ref _setTurretTargetTypes);
                AssignMethod(delegates, "SetBlockTrackingRange", ref _setBlockTrackingRange);
                AssignMethod(delegates, "IsTargetAligned", ref _isTargetAligned);
                AssignMethod(delegates, "IsTargetAlignedExtended", ref _isTargetAlignedExtended);
                AssignMethod(delegates, "CanShootTarget", ref _canShootTarget);
                AssignMethod(delegates, "GetPredictedTargetPosition", ref _getPredictedTargetPos);
                AssignMethod(delegates, "GetHeatLevel", ref _getHeatLevel);
                AssignMethod(delegates, "GetCurrentPower", ref _currentPowerConsumption);
                AssignMethod(delegates, "GetMaxPower", ref _getMaxPower);
                AssignMethod(delegates, "HasGridAi", ref _hasGridAi);
                AssignMethod(delegates, "HasCoreWeapon", ref _hasCoreWeapon);
                AssignMethod(delegates, "GetOptimalDps", ref _getOptimalDps);
                AssignMethod(delegates, "GetActiveAmmo", ref _getActiveAmmo);
                AssignMethod(delegates, "SetActiveAmmo", ref _setActiveAmmo);
                AssignMethod(delegates, "MonitorProjectile", ref _monitorProjectile);
                AssignMethod(delegates, "UnMonitorProjectile", ref _unMonitorProjectile);
                AssignMethod(delegates, "GetProjectileState", ref _getProjectileState);
                AssignMethod(delegates, "GetConstructEffectiveDps", ref _getConstructEffectiveDps);
                AssignMethod(delegates, "GetPlayerController", ref _getPlayerController);
                AssignMethod(delegates, "GetWeaponAzimuthMatrix", ref _getWeaponAzimuthMatrix);
                AssignMethod(delegates, "GetWeaponElevationMatrix", ref _getWeaponElevationMatrix);
                AssignMethod(delegates, "IsTargetValid", ref _isTargetValid);
                AssignMethod(delegates, "GetWeaponScope", ref _getWeaponScope);
                AssignMethod(delegates, "IsInRange", ref _isInRange);
                AssignMethod(delegates, "RegisterEventMonitor", ref _monitorEvents);
                AssignMethod(delegates, "UnRegisterEventMonitor", ref _unmonitorEvents);
                return true;
            }

            private void AssignMethod<T>(
                IReadOnlyDictionary<string, Delegate> delegates,
                string name,
                ref T field
            )
                where T : class
            {
                if (delegates == null)
                {
                    field = null;
                    return;
                }

                Delegate del;
                if (!delegates.TryGetValue(name, out del))
                    throw new Exception(
                        $"{GetType().Name} :: Couldn't find {name} delegate of type {typeof(T)}"
                    );

                field = del as T;
                if (field == null)
                    throw new Exception(
                        $"{GetType().Name} :: Delegate {name} is not type {typeof(T)}, instead it's: {del.GetType()}"
                    );
            }

            public void GetAllCoreWeapons(ICollection<MyDefinitionId> collection) =>
                _getCoreWeapons?.Invoke(collection);

            public void GetAllCoreStaticLaunchers(ICollection<MyDefinitionId> collection) =>
                _getCoreStaticLaunchers?.Invoke(collection);

            public void GetAllCoreTurrets(ICollection<MyDefinitionId> collection) =>
                _getCoreTurrets?.Invoke(collection);

            public bool GetBlockWeaponMap(
                Sandbox.ModAPI.Ingame.IMyTerminalBlock weaponBlock,
                IDictionary<string, int> collection
            ) => _getBlockWeaponMap?.Invoke(weaponBlock, collection) ?? false;

            public MyTuple<bool, int, int> GetProjectilesLockedOn(long victim) =>
                _getProjectilesLockedOn?.Invoke(victim) ?? new MyTuple<bool, int, int>();

            public void GetSortedThreats(
                Sandbox.ModAPI.Ingame.IMyTerminalBlock pBlock,
                IDictionary<MyDetectedEntityInfo, float> collection
            ) => _getSortedThreats?.Invoke(pBlock, collection);

            public void GetObstructions(
                Sandbox.ModAPI.Ingame.IMyTerminalBlock pBlock,
                ICollection<Sandbox.ModAPI.Ingame.MyDetectedEntityInfo> collection
            ) => _getObstructions?.Invoke(pBlock, collection);

            public MyDetectedEntityInfo? GetAiFocus(long shooter, int priority = 0) =>
                _getAiFocus?.Invoke(shooter, priority);

            public bool SetAiFocus(
                Sandbox.ModAPI.Ingame.IMyTerminalBlock pBlock,
                long target,
                int priority = 0
            ) => _setAiFocus?.Invoke(pBlock, target, priority) ?? false;

            public bool ReleaseAiFocus(
                Sandbox.ModAPI.Ingame.IMyTerminalBlock pBlock,
                long playerId
            ) => _releaseAiFocus?.Invoke(pBlock, playerId) ?? false;

            public MyDetectedEntityInfo? GetWeaponTarget(
                Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon,
                int weaponId = 0
            ) => _getWeaponTarget?.Invoke(weapon, weaponId);

            public void SetWeaponTarget(
                Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon,
                long target,
                int weaponId = 0
            ) => _setWeaponTarget?.Invoke(weapon, target, weaponId);

            public void FireWeaponOnce(
                Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon,
                bool allWeapons = true,
                int weaponId = 0
            ) => _fireWeaponOnce?.Invoke(weapon, allWeapons, weaponId);

            public void ToggleWeaponFire(
                Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon,
                bool on,
                bool allWeapons,
                int weaponId = 0
            ) => _toggleWeaponFire?.Invoke(weapon, on, allWeapons, weaponId);

            public bool IsWeaponReadyToFire(
                Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon,
                int weaponId = 0,
                bool anyWeaponReady = true,
                bool shootReady = false
            ) =>
                _isWeaponReadyToFire?.Invoke(weapon, weaponId, anyWeaponReady, shootReady) ?? false;

            public float GetMaxWeaponRange(
                Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon,
                int weaponId
            ) => _getMaxWeaponRange?.Invoke(weapon, weaponId) ?? 0f;

            public bool GetTurretTargetTypes(
                Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon,
                IList<string> collection,
                int weaponId = 0
            ) => _getTurretTargetTypes?.Invoke(weapon, collection, weaponId) ?? false;

            public void SetTurretTargetTypes(
                Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon,
                IList<string> collection,
                int weaponId = 0
            ) => _setTurretTargetTypes?.Invoke(weapon, collection, weaponId);

            public void SetBlockTrackingRange(
                Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon,
                float range
            ) => _setBlockTrackingRange?.Invoke(weapon, range);

            public bool IsTargetAligned(
                Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon,
                long targetEnt,
                int weaponId
            ) => _isTargetAligned?.Invoke(weapon, targetEnt, weaponId) ?? false;

            public MyTuple<bool, Vector3D?> IsTargetAlignedExtended(
                Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon,
                long targetEnt,
                int weaponId
            ) =>
                _isTargetAlignedExtended?.Invoke(weapon, targetEnt, weaponId)
                ?? new MyTuple<bool, Vector3D?>();

            public bool CanShootTarget(
                Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon,
                long targetEnt,
                int weaponId
            ) => _canShootTarget?.Invoke(weapon, targetEnt, weaponId) ?? false;

            public Vector3D? GetPredictedTargetPosition(
                Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon,
                long targetEnt,
                int weaponId
            ) => _getPredictedTargetPos?.Invoke(weapon, targetEnt, weaponId) ?? null;

            public float GetHeatLevel(Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon) =>
                _getHeatLevel?.Invoke(weapon) ?? 0f;

            public float GetCurrentPower(Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon) =>
                _currentPowerConsumption?.Invoke(weapon) ?? 0f;

            public float GetMaxPower(MyDefinitionId weaponDef) =>
                _getMaxPower?.Invoke(weaponDef) ?? 0f;

            public bool HasGridAi(long entity) => _hasGridAi?.Invoke(entity) ?? false;

            public bool HasCoreWeapon(Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon) =>
                _hasCoreWeapon?.Invoke(weapon) ?? false;

            public float GetOptimalDps(long entity) => _getOptimalDps?.Invoke(entity) ?? 0f;

            public string GetActiveAmmo(
                Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon,
                int weaponId
            ) => _getActiveAmmo?.Invoke(weapon, weaponId) ?? null;

            public void SetActiveAmmo(
                Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon,
                int weaponId,
                string ammoType
            ) => _setActiveAmmo?.Invoke(weapon, weaponId, ammoType);

            public void MonitorProjectileCallback(
                Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon,
                int weaponId,
                Action<long, int, ulong, long, Vector3D, bool> action
            ) => _monitorProjectile?.Invoke(weapon, weaponId, action);

            public void UnMonitorProjectileCallback(
                Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon,
                int weaponId,
                Action<long, int, ulong, long, Vector3D, bool> action
            ) => _unMonitorProjectile?.Invoke(weapon, weaponId, action);

            public MyTuple<Vector3D, Vector3D, float, float, long, string> GetProjectileState(
                ulong projectileId
            ) =>
                _getProjectileState?.Invoke(projectileId)
                ?? new MyTuple<Vector3D, Vector3D, float, float, long, string>();

            public float GetConstructEffectiveDps(long entity) =>
                _getConstructEffectiveDps?.Invoke(entity) ?? 0f;

            public long GetPlayerController(Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon) =>
                _getPlayerController?.Invoke(weapon) ?? -1;

            public Matrix GetWeaponAzimuthMatrix(
                Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon,
                int weaponId
            ) => _getWeaponAzimuthMatrix?.Invoke(weapon, weaponId) ?? Matrix.Zero;

            public Matrix GetWeaponElevationMatrix(
                Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon,
                int weaponId
            ) => _getWeaponElevationMatrix?.Invoke(weapon, weaponId) ?? Matrix.Zero;

            public bool IsTargetValid(
                Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon,
                long targetId,
                bool onlyThreats,
                bool checkRelations
            ) => _isTargetValid?.Invoke(weapon, targetId, onlyThreats, checkRelations) ?? false;

            public MyTuple<Vector3D, Vector3D> GetWeaponScope(
                Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon,
                int weaponId
            ) => _getWeaponScope?.Invoke(weapon, weaponId) ?? new MyTuple<Vector3D, Vector3D>();

            // terminalBlock, Threat, Other, Something
            public MyTuple<bool, bool> IsInRange(Sandbox.ModAPI.Ingame.IMyTerminalBlock block) =>
                _isInRange?.Invoke(block) ?? new MyTuple<bool, bool>();

            public void MonitorEvents(
                Sandbox.ModAPI.Ingame.IMyTerminalBlock entity,
                int partId,
                Action<int, bool> action
            ) => _monitorEvents?.Invoke(entity, partId, action);

            public void UnMonitorEvents(
                Sandbox.ModAPI.Ingame.IMyTerminalBlock entity,
                int partId,
                Action<int, bool> action
            ) => _unmonitorEvents?.Invoke(entity, partId, action);
        }

        #endregion // LineManager
    }
}
