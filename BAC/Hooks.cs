using RoR2.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BetterAutoComplete
{
    class Hooks
    {

        internal static void InitializeHooks()
        {
            On.RoR2.Console.Update += Console_Update;
            On.RoR2.UI.ConsoleWindow.OnInputFieldValueChanged += ConsoleWindow_OnInputFieldChanged;

            //On.RoR2.Networking.GameNetworkManager.OnClientConnect += (self, user, t) => { };//mutiplayer dbg testing
        }

        internal static void DisableHooks()
        {
            On.RoR2.Console.Update -= Console_Update;
            On.RoR2.UI.ConsoleWindow.OnInputFieldValueChanged -= ConsoleWindow_OnInputFieldChanged;

            //On.RoR2.Networking.GameNetworkManager.OnClientConnect -= (self, user, t) => { };//mutiplayer dbg testing
        }

        /// <summary>
        /// runs autoComplete checks.
        /// </summary>
        /// <param name="o"></param>
        /// <param name="s"></param>
        private static void Console_Update(On.RoR2.Console.orig_Update o, RoR2.Console s)
        {
            o.Invoke(s);
            if (Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.BackQuote) || Input.GetKey(KeyCode.Backspace))
            {
                BAC.entryPoint.ClearAutoComplete(false);
            }

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                BAC.entryPoint.RunAutoComplete();//run the autocomplete code
            }
        }

        /// <summary>
        /// Preforms special case checks for Autocomplete and Updates its data for when AC is run (filling the list with which ACFillWith... methods to run).
        /// </summary>
        /// <param name="o"></param>
        /// <param name="s"></param>
        /// <param name="input"></param>
        private static void ConsoleWindow_OnInputFieldChanged(On.RoR2.UI.ConsoleWindow.orig_OnInputFieldValueChanged o, ConsoleWindow s, string input)
        {
            o.Invoke(s, input);//allow original code to run first

            Event current = Event.current;//get the current unity event
            if (!Input.GetKey(KeyCode.Tab) && !Input.GetKey(KeyCode.KeypadEnter) && !Input.GetKey(KeyCode.Return)
                && !Input.GetKey(KeyCode.Escape) && !Input.GetKey(KeyCode.Backspace) && ConsoleWindow.instance.inputField.text.Contains(" "))
            {//if tab, backspace, either enter nor escape is being pressed and at least one space exists
                BAC.entryPoint.AutoComplete();//initialize the autocomplete code (gets instruction and currentFoundARCI for the methods later on to run)

                //as this is only ever run after the game is loaded, extention mods will have their methods added first.
                //if (BAC.entryPoint.methods.Count > 0)//use this to check if an extention mod has added a new method
                //if (!BAC.entryPoint.methods[BAC.entryPoint.methods.Count - 1].Method.DeclaringType.ToString().StartsWith("Console_Overhaul"))//use this if you need this code to only go through once

                if (BAC.entryPoint.methods.Count == 0)
                {
                    BAC.entryPoint.methods.Add(() => BAC.entryPoint.ACFillWithPlayers());//add only relevent items
                    BAC.entryPoint.methods.Add(() => BAC.entryPoint.ACFillWithBuffs());
                    BAC.entryPoint.methods.Add(() => BAC.entryPoint.ACFillWithEquipment());
                    BAC.entryPoint.methods.Add(() => BAC.entryPoint.ACFillWithItems());
                    BAC.entryPoint.methods.Add(() => BAC.entryPoint.ACFillWithTeams());
                }

                //now the code will run, meaning even though the extention mods will have their methods added (not run) prior to currentFoundARCI and instruction being properly defined
                //the data has NOW been generated, meaning it's now safe to run. This must be run here and not on an awake monomethod due to the load order B.A.C requires.
                if (BAC.entryPoint.CurrentFoundACRI != "")
                {//if there is a AC Recognized Item in the command
                    if (!BAC.entryPoint.CurrentFoundACRI.Contains("CC"))
                    {//and if the ACRI is not a special case (e.g. "CC COBind")
                        foreach (Action method in BAC.entryPoint.methods)
                        {
                            method.Invoke();//run each AutoComplete method, updating the results list
                        }
                        //this is the equivalent of:
                        //any extention mods' ACFillWith...() method for this mod that are not special case ACFillWith...() method.
                        //ACFillWithPlayers();
                        //ACFillWithItems();
                        //ACFillWithBuffs();
                        //ACFillWithEquipment();
                    }
                    else
                    {//if the ACRI is a special case
                        foreach (Action method in BAC.entryPoint.specialMethods)
                        {
                            method.Invoke();//run each special case AutoComplete method
                        }
                    }
                }
            }
        }

    }
}
