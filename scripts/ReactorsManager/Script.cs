using System;
using System.Collections.Generic;

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
namespace ReactorsManager
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
        #region ReactorsManager

        int runs_between_loops = 20;
        int runs_passed = 0;

        MyItemType fusion_fuel = new MyItemType("MyObjectBuilder_Ingot", "FusionFuel");

        int fuel_quantity_wanted = 1;

        public Program()
        {
            refresh();
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            if (updateSource == UpdateType.Update100 && runs_passed++ >= runs_between_loops)
            {
                runs_passed = 0;
                refresh();
            }
        }

        void refresh()
        {
            List<IMyReactor> reactors = new List<IMyReactor>();
            GridTerminalSystem.GetBlocksOfType<IMyReactor>(reactors);
            if (reactors.Count == 0)
                return;

            List<IMyCargoContainer> cargos = new List<IMyCargoContainer>();
            GridTerminalSystem.GetBlocksOfType<IMyCargoContainer>(cargos);
            IMyCargoContainer fuel_cargo = null;

            for (int i = 0; i < cargos.Count; i++)
                if (cargos[i].CustomName.Contains("Fuel"))
                {
                    fuel_cargo = cargos[i] as IMyCargoContainer;
                    break;
                }
            IMyInventory fuel_cargo_inventory = fuel_cargo.GetInventory();

            for (int i = 0; i < reactors.Count; i++)
            {
                IMyReactor reactor = reactors[i];
                reactor.UseConveyorSystem = false;
                IMyInventory reactor_inventory = reactor.GetInventory();
                float fuel_quantity = (float)reactor_inventory.GetItemAmount(fusion_fuel);
                MyFixedPoint excess_fuel_quantity =
                    (MyFixedPoint)fuel_quantity - fuel_quantity_wanted;

                if (excess_fuel_quantity > 0)
                {
                    MyInventoryItem? fuel_in_reactor = reactor_inventory.FindItem(fusion_fuel);
                    if (!fuel_in_reactor.HasValue)
                        continue;
                    reactor_inventory.TransferItemTo(
                        fuel_cargo_inventory,
                        fuel_in_reactor.Value,
                        excess_fuel_quantity
                    );
                }
                else if (excess_fuel_quantity < 0)
                {
                    MyFixedPoint needed_fuel_quantity = -excess_fuel_quantity;
                    MyInventoryItem? fuel_in_cargo = fuel_cargo_inventory.FindItem(fusion_fuel);
                    if (!fuel_in_cargo.HasValue)
                        continue;
                    if (fuel_in_cargo.Value.Amount < needed_fuel_quantity)
                        needed_fuel_quantity = fuel_in_cargo.Value.Amount;
                    reactor_inventory.TransferItemFrom(
                        fuel_cargo_inventory,
                        fuel_in_cargo.Value,
                        needed_fuel_quantity
                    );
                }
            }
        }
        #endregion // ReactorsManager
    }
}
