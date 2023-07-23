using System;

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
namespace utils
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
        #region utils

        /*
         * The constructor, called only once every session and always before any
         * other method is called. Use it to initialize your script.
         *
         * The constructor is optional and can be removed if not needed.
         *
         * It's recommended to set RuntimeInfo.UpdateFrequency here, which will
         * allow your script to run itself without a timer block.
         */
        public Program()
        {
            Me.CustomData = "----- Extractor Manager -----\n\nGridSize (SG or LG)=";
        }

        /*
         * Called when the program needs to save its state. Use this method to save
         * your state to the Storage field or some other means.
         *
         * This method is optional and can be removed if not needed.
         */
        public void Save() { }

        /*
         * The main entry point of the script, invoked every time one of the
         * programmable block's Run actions are invoked, or the script updates
         * itself. The updateSource argument describes where the update came from.
         *
         * The method itself is required, but the arguments above can be removed
         * if not needed.
         */
        public void Main(string argument, UpdateType updateSource)
        {
            string[] custom_data_lines = Me.CustomData.Split('\n');

            for (int i = 0; i < custom_data_lines.Length; i++)
            {
                if (custom_data_lines[i].Contains("="))
                {
                    string[] data_value = custom_data_lines[i].Split('=');
                    string data = data_value[0];

                    if (data_value.Length == 1 || data_value[1] == "")
                    {
                        Echo("Value for " + data + " not set");
                    }
                    else
                    {
                        string value = data_value[1];

                        switch (data)
                        {
                            case "GridSize (SG or LG)":
                                Echo("GridSize=" + value);
                                break;
                        }
                    }
                }
            }
        }

        #endregion // utils
    }
}
