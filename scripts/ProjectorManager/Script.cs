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
namespace ProjectorManager
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
        #region ProjectorManager

        public Program() { }

        public void Main(string argument, UpdateType updateSource)
        {
            if (argument == "")
                return;

            List<IMyProjector> projectors = new List<IMyProjector>();
            GridTerminalSystem.GetBlocksOfType<IMyProjector>(projectors);
            IMyProjector projector = null;

            for (int i = 0; i < projectors.Count; i++)
                if (projectors[i].CustomName.ToString().Contains("Main"))
                {
                    projector = projectors[i];
                    break;
                }

            if (projector == null)
                return;

            Echo(projector.Position.ToString());
            IMyCubeGrid grid = projector.CubeGrid;
            Echo(grid.Max.CompareTo(grid.Min).ToString());

        }

        #endregion // ProjectorManager
    }
}
