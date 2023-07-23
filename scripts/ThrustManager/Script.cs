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
namespace ThrustManager
{
    /*
     * Do not change this declaration because this is the game requirement.
     */
    public sealed class Program : MyGridProgram
    {
        #region ThrustManager

        class THRUSTER
        {
            public string ID;
            public int MAX_SIGNAL;

            public THRUSTER(string id, int max_signal)
            {
                ID = id;
                MAX_SIGNAL = max_signal;
            }
        }

        THRUSTER[] thrusters_efficiency_ordered = new THRUSTER[]
        {
            new THRUSTER("SILVERSMITH", 40),
            new THRUSTER("SCIRCOCCO", 105),
            new THRUSTER("Mega", 275),
            new THRUSTER("MUNR", 13),
            new THRUSTER("RZB", 6),
            new THRUSTER("YATCH", 2),
            new THRUSTER("Leo", 60),
            new THRUSTER("DRUMMER", 63),
            new THRUSTER("ROCI", 72),
            new THRUSTER("PNDR", 19),
            new THRUSTER("ARYLNX_Epstein", 24),
            new THRUSTER("RAIDER", 30),
            new THRUSTER("QUADRA", 20)
        };

        Dictionary<string, List<IMyThrust>> thrusters = new Dictionary<string, List<IMyThrust>>();

        bool shut_down_unused = false;

        public Program()
        {
            if (Me.CustomData == "")
                Me.CustomData = "----- Thrust Manager -----\n\nShut down unused thrusters=true";
        }

        public void Main(string argument, UpdateType updateSource)
        {
            if (argument == "")
                return;

            float signal_wanted;
            try
            {
                if (argument.ToLower() == "max" || argument.ToLower() == "full")
                    signal_wanted = 9999f;
                else if (argument.ToLower() == "silent" || argument.ToLower() == "stop")
                    signal_wanted = 0f;
                else if (argument.ToLower() == "stealth")
                    signal_wanted = 16f;
                else
                    signal_wanted = float.Parse(argument);
            }
            catch (FormatException e)
            {
                Echo("Argument not recognized");
                return;
            }

            string[] custom_data_lines = Me.CustomData.Split('\n');

            for (int i = 0; i < custom_data_lines.Length; i++)
                if (custom_data_lines[i].Contains("="))
                {
                    string[] data_value = custom_data_lines[i].Split('=');
                    string data = data_value[0];

                    if (data_value.Length == 1)
                        Echo("Value for " + data + " not set");
                    else
                    {
                        string value = data_value[1].ToLower();

                        switch (data)
                        {
                            case "Shut down unused thrusters":
                                if (value == "true")
                                    shut_down_unused = true;
                                else
                                    shut_down_unused = false;
                                break;
                        }
                    }
                }

            for (int i = 0; i < thrusters_efficiency_ordered.Length; i++)
                thrusters[thrusters_efficiency_ordered[i].ID] = new List<IMyThrust>();

            List<IMyThrust> all_thrusters = new List<IMyThrust>();
            GridTerminalSystem.GetBlocksOfType<IMyThrust>(all_thrusters);
            for (int i = 0; i < all_thrusters.Count; i++)
            {
                if (
                    all_thrusters[i].IsWorking
                    || (shut_down_unused && all_thrusters[i].IsFunctional)
                )
                    for (int j = 0; j < thrusters_efficiency_ordered.Length; j++)
                    {
                        string thruster_id = thrusters_efficiency_ordered[j].ID;
                        if (all_thrusters[i].BlockDefinition.ToString().Contains(thruster_id))
                        {
                            thrusters[thruster_id].Add(all_thrusters[i]);
                            all_thrusters[i].ThrustOverridePercentage = 0;
                            break;
                        }
                    }
                if (
                    shut_down_unused && !all_thrusters[i].BlockDefinition.ToString().ToLower().Contains("rcs")
                )
                {
                    Me.CustomData += "\n"+all_thrusters[i].BlockDefinition.ToString();
                    all_thrusters[i].Enabled = false;
                    all_thrusters[i].ThrustOverridePercentage = 0;
                }
            }

            if (signal_wanted == 0)
                return;

            for (int i = 0; i < thrusters_efficiency_ordered.Length; i++)
            {
                string thruster_id = thrusters_efficiency_ordered[i].ID;
                int thruster_signal = thrusters_efficiency_ordered[i].MAX_SIGNAL;
                int count_thrusters = thrusters[thruster_id].Count;
                if (count_thrusters > 0)
                {
                    int max_signal = count_thrusters * thruster_signal;
                    float delta = signal_wanted / max_signal;

                    if (delta == 0)
                        return;
                    if (delta > 1)
                        delta = 1;

                    for (int j = 0; j < count_thrusters; j++)
                    {
                        thrusters[thruster_id][j].Enabled = true;
                        thrusters[thruster_id][j].ThrustOverridePercentage = delta;
                    }

                    if (delta < 1)
                        return;
                    else
                        signal_wanted -= max_signal;
                }
            }
        }

        #endregion // ThrustManager
    }
}
