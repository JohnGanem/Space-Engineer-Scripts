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
namespace ExtractorManager
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
        #region ExtractorManager

        int runs_between_loops = 4;
        int runs_passed = 0;

        int fuel_tank_capacity = 600000;
        int jerry_can_capacity = 5000;

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
            List<IMyGasGenerator> extractors = new List<IMyGasGenerator>();
            GridTerminalSystem.GetBlocksOfType<IMyGasGenerator>(extractors);

            if (extractors.Count == 0)
                return;

            IMyGasGenerator extractor = null;
            string tank_type = null;
            int refuel_quantity = 0;
            for (int i = 0; i < extractors.Count; i++)
            {
                if (extractors[i].BlockDefinition.ToString().Contains("Extractor"))
                {
                    extractor = extractors[i];

                    if (extractor.BlockDefinition.ToString().Contains("Small"))
                    {
                        tank_type = "SG_Fuel_Tank";
                        refuel_quantity = jerry_can_capacity;
                    }
                    else
                    {
                        tank_type = "Fuel_Tank";
                        refuel_quantity = fuel_tank_capacity;
                    }
                    break;
                }
            }

            if (extractor == null)
                return;

            IMyInventory extractor_inventory = extractor.GetInventory();
            if (extractor_inventory.CurrentVolume > 0)
                return;

            int tank_h2_total = 0;
            int tank_h2_actual = 0;

            List<IMyGasTank> tanks = new List<IMyGasTank>();
            GridTerminalSystem.GetBlocksOfType<IMyGasTank>(tanks);

            for (int i = 0; i < tanks.Count; i++)
            {
                string blockId = tanks[i].BlockDefinition.ToString();
                if (blockId.Contains("Hydro"))
                {
                    IMyGasTank HydroTank = tanks[i] as IMyGasTank;
                    if (HydroTank.IsWorking)
                    {
                        tank_h2_total += (int)HydroTank.Capacity;
                        tank_h2_actual += (int)(HydroTank.Capacity * HydroTank.FilledRatio);
                    }
                }
            }

            if (tank_h2_actual < (tank_h2_total - refuel_quantity))
            {
                List<IMyCargoContainer> cargos = new List<IMyCargoContainer>();
                GridTerminalSystem.GetBlocksOfType<IMyCargoContainer>(cargos);
                List<IMyInventory> inventories = new List<IMyInventory>();

                for (int i = 0; i < cargos.Count; i++)
                    inventories.Add(cargos[i].GetInventory());

                for (int i = 0; i < inventories.Count; i++)
                {
                    IMyInventory inventory = inventories[i];

                    MyInventoryItem? check = inventory.FindItem(
                        new MyItemType("MyObjectBuilder_Component", tank_type)
                    );
                    if (check != null)
                    {
                        string[] item_info = check.ToString().Split('x');
                        double item_count = double.Parse(item_info[0]);
                        if (item_count >= 1)
                        {
                            List<MyInventoryItem> items = new List<MyInventoryItem>();
                            inventory.GetItems(items);
                            for (int j = 0; j < items.Count; j++)
                                if ((items[j].ToString().Contains(tank_type)))
                                {
                                    extractor
                                        .GetInventory()
                                        .TransferItemFrom(inventory, items[j], 1);
                                    return;
                                }
                        }
                    }
                }
            }
        }
        #endregion // ExtractorManager
    }
}
