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
namespace LineManagerV1
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

        IMyRadioAntenna antenna;

        string enteringLineBroadCastTag = "EnteringLine";
        string quittingLineBroadCastTag = "QuittingLine";
        string unicastTag = "ShareLineStatus";
        IMyBroadcastListener enteringLineBroadcastListener;
        IMyBroadcastListener quittingLineBroadcastListener;
        IMyUnicastListener unicastListener;

        List<MARKETPLACE> marketplaces = new List<MARKETPLACE>();

        MARKETPLACE actual_marketplace;
        LINE_STATUS actual_line_status;

        bool marketplaceIsEmpty = false;

        public Program()
        {
            antenna = getMyAntenna();

            enteringLineBroadcastListener = IGC.RegisterBroadcastListener(enteringLineBroadCastTag);
            quittingLineBroadcastListener = IGC.RegisterBroadcastListener(quittingLineBroadCastTag);
            unicastListener = IGC.UnicastListener;

            MARKETPLACE Ceres = new MARKETPLACE(
                new Vector3(-1464.95, 1995528.82, 61466.44),
                25000,
                30000,
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
            while (unicastListener.HasPendingMessage)
            {
                var msg = unicastListener.AcceptMessage();
                handleUnicast(msg);
            }
            while (quittingLineBroadcastListener.HasPendingMessage)
            {
                var msg = quittingLineBroadcastListener.AcceptMessage();
                handleQuittingBroadcast(msg);
            }
            while (enteringLineBroadcastListener.HasPendingMessage)
            {
                var msg = enteringLineBroadcastListener.AcceptMessage();
                handleEnteringBroadcast(msg);
            }

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
            actual_line_status = new LINE_STATUS(myRefreshNeeded);
            IGC.SendBroadcastMessage(enteringLineBroadCastTag, "NEW");
            marketplaceIsEmpty = true;
        }

        void quitTheLine()
        {
            if (actual_line_status != null)
            {
                IGC.SendBroadcastMessage(quittingLineBroadCastTag, actual_line_status.myPosition);
                actual_line_status = null;
            }
        }

        void myPlaceInLine(long receiver)
        {
            if (actual_line_status != null)
                IGC.SendUnicastMessage(receiver, unicastTag, actual_line_status.myPosition);
        }

        IMyRadioAntenna getMyAntenna()
        {
            List<IMyRadioAntenna> antennas = new List<IMyRadioAntenna>();
            GridTerminalSystem.GetBlocksOfType<IMyRadioAntenna>(antennas);

            for (int i = 0; i < antennas.Count; i++)
                if (antennas[i].IsFunctional && antennas[i].CustomName.ToString().Contains("Line"))
                    return antennas[i] as IMyRadioAntenna;
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
                    {
                        antenna.Enabled = true;
                        antenna.EnableBroadcasting = true;
                        antenna.Radius = (float)marketplace.outerRadius;
                        return marketplace;
                    }
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
                {
                    antenna.Enabled = false;
                    antenna.EnableBroadcasting = false;
                    antenna.Radius = 0;
                    return true;
                }
            }
            return false;
        }

        void refreshLineStatus()
        {
            if (actual_line_status != null)
            {
                if (marketplaceIsEmpty)
                {
                    actual_line_status.myPosition = actual_line_status.lineSize = 1;
                    marketplaceIsEmpty = false;
                }
                if (antenna != null)
                    antenna.HudText =
                        "I am " + actual_line_status.myPosition + "/" + actual_line_status.lineSize;
            }
        }

        void handleUnicast(MyIGCMessage msg)
        {
            if (msg.Tag == unicastTag && msg.Data is int && actual_line_status != null)
            {
                marketplaceIsEmpty = false;
                if (actual_line_status.myPosition == 0)
                {
                    actual_line_status.lineSize = actual_line_status.myPosition = (int)msg.Data;
                }
                else if (actual_line_status.lineSize != (int)msg.Data)
                {
                    actual_line_status.lineSize = (int)msg.Data;
                }
            }
        }

        void handleQuittingBroadcast(MyIGCMessage msg)
        {
            if (msg.Data is int && actual_line_status != null)
            {
                actual_line_status.lineSize--;
                if ((int)msg.Data < actual_line_status.myPosition)
                    actual_line_status.myPosition--;
            }
        }

        void handleEnteringBroadcast(MyIGCMessage msg)
        {
            if ((string)msg.Data == "NEW" && actual_line_status != null)
            {
                actual_line_status.lineSize++;
                IGC.SendUnicastMessage(msg.Source, unicastTag, actual_line_status.lineSize);
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

            public LINE_STATUS(float myRefreshLeft)
            {
                this.myRefreshLeft = myRefreshLeft;
            }
        }

        #endregion // LineManager
    }
}
