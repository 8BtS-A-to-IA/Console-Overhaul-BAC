using BepInEx;
using Rewired;
using RoR2;
using RoR2.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

namespace BetterAutoComplete
{
    [BepInPlugin("com.ML.BAC", "Console Overhaul: Better Auto-Complete", "1.0")]
    [BepInDependency(MUTDep, BepInDependency.DependencyFlags.SoftDependency)]
    public class BAC : BaseUnityPlugin
    {
        private const string MUTDep = "com.ML.MUT";

        /// <summary>
        /// Entry point used to access non-static methods.
        /// </summary>
        public static BAC entryPoint = new BAC();

        public void Awake()
        {
            Hooks.InitializeHooks();
        }
        public void OnDisable()
        {
            Hooks.DisableHooks();//drop hooks
        }

        /// <summary>
        /// Reads the text from the CC's input field and splits the command from the entryPoint.instructions; finding what suggestion fills need to be run and the current instrcution being tabbed on.
        ///  This is the first method of AC that is run (runs when text is added), the "ACFillWith..." methods run next then the "RunAutoComplete" method runs last when tab is pressed.
        /// </summary>
        internal void AutoComplete()
        {
            //need to refactor this badly

            //Event current = Event.current;//get the current unity event
            List<int> ACRIPoss = new List<int>();
            bool specialCase = false;

            if (ConsoleWindow.instance)
            {//if the console window is currently opened
                entryPoint.instructions.Clear();
                string text = "";//prepare to temporarly get the current text in the consoleWindow's textbox
                //example data = CCCOGivePlayerBuff "8Bit Shadow" c
                //foreach (string text2 in ConsoleWindow.instance.inputField.text.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                foreach (string text2 in ConsoleWindow.instance.inputField.text.Split(' '))
                {//foreach entryPoint.instruction (splitting by a space)
                    //"CCCOGivePlayerBuff", ""8Bit Shadow"", "c"
                    if (text2.StartsWith("\"") && !text2.EndsWith("\""))
                    {//if the entryPoint.instruction doesn't end with a quotation mark but starts with one (e.g. a name)
                        //run1: false
                        //run2: true
                        //run3: false
                        //run4: false
                        text = text2;//set the temporary text to the current entryPoint.instruction
                        //run2: text = ""8Bit"
                    }
                    else if (!text2.StartsWith("\"") && text2.EndsWith("\"") && !text.Equals(""))
                    {//otherwise if the entryPoint.instruction inversly does not start with a quotation mark but does end with one AND the current tmp text is not null
                        //run1: false
                        //run2: false
                        //run3: true
                        //run4: false
                        text = text + " " + text2;//add a space and the entryPoint.instruction to the already existing entryPoint.instructions in the tmp text
                        entryPoint.instructions.Add(text);
                        text = "";//reset the tmp text
                        //run3: text = ""8Bit Shadow""
                        //  list = "CCCOGivePlayerBuff", ""8Bit Shadow""
                        //      text = ""
                    }
                    else if (!text2.StartsWith("\"") && !text2.EndsWith("\"") && text.Equals(""))
                    {//if the entryPoint.instruction does not start with a quotation mark nor end with one AND the tmp text is null
                        //run1: true
                        //run2: false
                        //run3: false
                        //run4: true
                        entryPoint.instructions.Add(text2);
                        //run1: list = "CCCOGivePlayerBuff"
                        //run4: list = "CCCOGivePlayerBuff", ""8Bit Shadow"", "c"
                    }
                    else if (text2.StartsWith("\"") && text2.EndsWith("\""))
                    {//if the entryPoint.instruction starts with, and ends with, a quotation mark
                        entryPoint.instructions.Add(text2);
                    }
                    else if (!text2.StartsWith("\"") && !text2.EndsWith("\"") && !text.Equals(""))
                    {//if there is text and no quotation marks at neither the beginning nor end AND the tmp text is NOT null
                        //never reached
                        text += text2;//simply add the entryPoint.instruction
                    }
                }
                Print("BAC: amount of words inputted:" + (entryPoint.instructions.Count - 1), 6);
            }

            if (entryPoint.instructions.Count >= 2 && (entryPoint.tabIndicatorPos == 0u || entryPoint.instructions.Count >= entryPoint.previousParamCount) && !Input.GetKeyDown(KeyCode.DownArrow) 
            && !Input.GetKeyDown(KeyCode.UpArrow) && !Input.GetKeyDown(KeyCode.LeftArrow) && !Input.GetKeyDown(KeyCode.RightArrow) && !Input.GetKeyDown(KeyCode.End) && !Input.GetKeyDown(KeyCode.Home))
            {//if a key has been pressed that is NOT an arrow key or the end/home key AND there is at least 2 instructiosn AND tab has not been pressed PREVIOSULY OR there are MORE instructions than before
                entryPoint.previousParamCount = entryPoint.instructions.Count;//store the amount of instructions there currently are, to be read in the next run
                foreach (string RI in entryPoint.ACRI)
                {//foreach recognition item
                    if (!RI.Contains("CC"))
                    {//if the entryPoint.ACRI is not a special case
                        ACRIPoss.Add(entryPoint.instructions[0].ToLowerInvariant().IndexOf(RI.ToLowerInvariant()));//store the possition the entryPoint.ACRI appears in the CC
                    }
                    else if (entryPoint.instructions[0].ToLowerInvariant().Equals(RI.ToLowerInvariant().Remove(0, 3), StringComparison.InvariantCultureIgnoreCase))
                    {//otherwise if is a special case, remove the "CC " and check if the current CC is the special case
                        ACRIPoss.Add(0);//store the possition as the start
                        specialCase = true;//flag as running a special case autocomplete
                    }
                }
                if (ACRIPoss.FindIndex((int z) => z > -1) > -1)
                {//if there is at least one item with a possition more than -1 (-1 = no items recognized by the entryPoint.ACRI)
                    if (entryPoint.autoCompleteResults.Count > 0)
                    {//if there are already stored results
                        entryPoint.autoCompleteResults.Clear();//clear them
                    }
                    if (!specialCase)
                    {//if not running in a special case
                        List<int> TMPACRIPoss = new List<int>(ACRIPoss.Capacity);//create a temporary list of entryPoint.ACRI poss's
                        TMPACRIPoss.AddRange(ACRIPoss);//add each ACRIPoss to the tmp list
                        ACRIPoss.Sort();//sort the possitions so that the -1's are at the top (float the -1's) and so that it's in order
                        for (int k = 0; k <= TMPACRIPoss.Count; k++)
                        {//foreach non-special ACRI
                            if (ACRIPoss[0] == -1)
                            {//if the current ACRI was not detected
                                ACRIPoss.RemoveAt(0);//remove it from the 'found entryPoint.ACRI' list
                            }
                            else
                            {
                                break;//otherwise we've finsished sanitisation
                            }
                        }
                        List<string> foundARCI = new List<string>(entryPoint.ACRI.Capacity);//create a new temporary list of ACRI's that where found
                        for (int l = 0; l < ACRIPoss.Count; l++)
                        {//foreach ACRIPoss (if the above sanitisation removed all, then there where no recognized ACRI)
                            foundARCI.Add(entryPoint.ACRI[TMPACRIPoss.IndexOf(ACRIPoss[l])]);//add each ACRI that was found to the 'found ACRI' list
                        }
                        for (int m = 0; m < ACRIPoss.Count; m++)
                        {//foreach ACRIPoss
                            //this requires that the first set of parameters for a CC is an AC Recognized Item (if the CC has any ACRecognitionItems in it)
                            if (entryPoint.instructions.Count - 2 == m)
                            {//if the current loop is at the current parameter (e.g. when the user is at the 2nd parameter, m must be 1)
                                //note: only one case of this for loop will enter here (restricting this to only run on the second last instruction)
                                //e.g. for "COGivePlayerBuff "8Bit Shadow" c", only when m is 1 will it enter here but wont when m is 0
                                entryPoint.currentFoundACRI = foundARCI[m];//the type found
                                entryPoint.instruction = entryPoint.instructions[m + 1];//the current text being tabbed on
                                break;
                                //Hooks->OnInputFieldValueChanged hook handles calling to fill the results list
                            }
                        }
                    }
                    else
                    {//if running in a special case
                        entryPoint.currentFoundACRI = "CC " + entryPoint.instructions.First();//get the CC to check
                        entryPoint.instruction = entryPoint.instructions.Last();//get the characters the user is tabbing on
                    }
                }
            }
            //it is impossible to reach this point when instructions.count is < 2
        }

        internal void ClearAutoComplete(bool checkCount) 
        {
            if ((entryPoint.instructions.Count < 3) || !checkCount)
            {//if there is only the command
                if ((entryPoint.instructions.Count == 2 && entryPoint.instructions[entryPoint.instructions.Count - 1] != "") || entryPoint.instructions.Count < 2 || !checkCount)
                {
                    entryPoint.previousParamCount = 0;//reset the vars
                    entryPoint.tabIndicatorPos = 0u;
                    entryPoint.autoCompleteResults.Clear();
                    entryPoint.instructions.Clear();
                    entryPoint.currentFoundACRI = "";
                    entryPoint.instruction = "";
                }
            }

        }

        /// <summary>
        /// Preforms the Auto complete function (applies the text to the inputText and itterates the suggestion index).
        /// </summary>
        internal void RunAutoComplete()
        {
            if (entryPoint.autoCompleteResults.Count > 0)
            {//if the autocomplete system has detected an entryPoint.ACRI (AutoCompleteRecognitionItem) and either entryPoint.tabIndicatorPos is 0 or the command hasn't been discarded/executed yet
                //replace the last entryPoint.instruction (parameter) with the suggestion

                entryPoint.tabIndicatorPos += 1u;//itterate the amount of times tab has been pressed (relative to the amount of items avalible) starting at 1
                if ((ulong)entryPoint.tabIndicatorPos > (ulong)((long)entryPoint.autoCompleteResults.Count))
                {//if the current amount of times tab has been pressed is past the end of the list
                    entryPoint.tabIndicatorPos = 1u;//cycle back so that this can be run infinetly
                }
                //string text3 = ConsoleWindow.instance.inputField.text + " " + entryPoint.autoCompleteResults[(int)(entryPoint.tabIndicatorPos - 1u)];
                string text3 = "";
                for (int j = 0; j < entryPoint.instructions.Count - 1; j++)
                {//foreach entryPoint.instruction
                    text3 = entryPoint.instructions[j] + " ";//text3 += current entryPoint.instruction + a space
                }
                text3 += entryPoint.autoCompleteResults[(int)(entryPoint.tabIndicatorPos - 1u)];//add the suggestion
                ConsoleWindow.instance.inputField.text = text3;//update the console windows' text
                ConsoleWindow.instance.inputField.MoveToEndOfLine(false, false);//move the cartet to the end of the text
            }
            else
            {
                //MonoBehaviour.print("BAC: Tab was pressed when no suggestion data was avalible!");
            }
        }

        /// <summary>
        /// Preform a levenshtein sort (in Generic.Compute) and output the result reversed.
        /// </summary>
        /// <param name="list"></param>
        /// <param name="comparision"></param>
        /// <returns></returns>
        internal List<string> LevenshteinSort(List<string> list, string comparision)
        {
            List<string> resList = new List<string>();
            List<string> tmpList = new List<string>(list);
            List<int> simularity = new List<int>();

            foreach (var item in list)
            {//foreach item in the list
                simularity.Add(Generic.Compute(comparision, item));//find the simularity of the item to the comparision item
            }

            foreach (var item in list)
            {//foreach item
                for (int i = 0; i < simularity.Count; i++)
                {//and foreach simularity
                    if (simularity[i] == simularity.Min())
                    {//if simularity[i] is largest
                        resList.Add(tmpList[i]);//add tmpList[i] to the results List
                        simularity.RemoveAt(i);//remove item from simularity list & tmp list
                        tmpList.RemoveAt(i);
                        break;//restart the for loop as simularity.count has changed.
                    }

                }
            }

            return resList;
        }

        /// <summary>
        /// Preform a levenshtein sort on the <c>entryPoint.autoCompleteResults</c> list.
        /// </summary>
        /// <param name="list"></param>
        /// <param name="comparision"></param>
        /// <returns></returns>
        public void levenshteinSort() 
        {
            entryPoint.autoCompleteResults = LevenshteinSort(entryPoint.autoCompleteResults, entryPoint.instruction);
        }

        /// <summary>
        /// A method which fills the B.A.C. systems' results list with players' usernames that have the characters present in the parameter.
        /// </summary>
        internal void ACFillWithPlayers()
        {
            if (entryPoint.currentFoundACRI.ToLowerInvariant().Equals("player"))
            {//if the entryPoint.ACRI is 'player'
                if (BAC.entryPoint.Instruction != "")
                {
                    foreach (NetworkUser networkUser in NetworkUser.readOnlyInstancesList)
                    {//foreach networkUser
                        if (networkUser.master)
                        {
                            CharacterBody body = networkUser.master.GetBody();//get the user's body
                            if (!body)
                            {//if the player does not have a body assigned (e.g. they are dead)
                                Print("BAC: players' body is null", 3);
                            }
                            else if (body.GetUserName().ToLowerInvariant().Contains(entryPoint.instruction.ToLowerInvariant()))
                            {//if the current entryPoint.instruction is not empty and the current body contains the text of the entryPoint.instruction
                                entryPoint.autoCompleteResults.Add("\"" + body.GetUserName() + "\"");//add the result to the results list
                            }
                        }
                        else
                        {
                            Trace.TraceInformation("ACFillWithPlayers can't fill due to the master object not existing for the network user: " + networkUser.userName);
                            MonoBehaviour.print("BAC: This command cannot be used while not in a mission, as such the auto complete system will not function.");
                            return;
                        }
                    }
                }
                else
                {
                    foreach (NetworkUser networkUser in NetworkUser.readOnlyInstancesList)
                    {//foreach networkUser
                        if (networkUser.master)
                        {
                            CharacterBody body = networkUser.master.GetBody();//get the user's body
                            if (!body)
                            {//if the player does not have a body assigned (e.g. they are dead)
                                Print("BAC: players' body is null", 3);
                            }
                            else
                            {
                                entryPoint.autoCompleteResults.Add("\"" + body.GetUserName() + "\"");//add the result to the results list
                            }
                        }
                        else
                        {
                            Trace.TraceInformation("ACFillWithPlayers can't fill due to the master object not existing for the network user: " + networkUser.userName);
                            MonoBehaviour.print("BAC: This command cannot be used while not in a mission, as such the auto complete system will not function.");
                            return;
                        }
                    }

                    if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(MUTDep))
                    {//if M.U.T. is installed; add the M.U.T. shorthands to the list
                        entryPoint.autoCompleteResults.Add("*");
                        entryPoint.autoCompleteResults.Add("All");
                        entryPoint.autoCompleteResults.Add("Me");
                        entryPoint.autoCompleteResults.Add("\"All:Alive==true\"");
                        entryPoint.autoCompleteResults.Add("\"All:spectator==true\"");
                        entryPoint.autoCompleteResults.Add("\"All:hasbuffany==true\"");
                        entryPoint.autoCompleteResults.Add("\"All:outofdanger==true\"");
                    }
                }

                Print("BAC: amount of results found:" + entryPoint.autoCompleteResults.Count, 4);
                entryPoint.autoCompleteResults = LevenshteinSort(entryPoint.autoCompleteResults, entryPoint.instruction);
            }
        }

        /// <summary>
        /// A method which fills the B.A.C. systems' results list with item names that have the characters present in the parameter.
        /// </summary>
        internal void ACFillWithItems()
        {
            if (entryPoint.currentFoundACRI.ToLowerInvariant().Equals("item"))
            {//if the entryPoint.ACRI is 'item'
                Print("BAC: got item", 5);
                if (BAC.entryPoint.Instruction != "")
                {
                    foreach (ItemIndex itemIndex in ItemCatalog.allItems)
                    {//foreach item
                        if (entryPoint.instruction.Length != 0 && itemIndex.ToString().ToLowerInvariant().Contains(entryPoint.instruction.ToLowerInvariant())
                            && !itemIndex.ToString().ToLowerInvariant().Contains("count"))
                        {//if the current entryPoint.instruction is not empty and the current item contains the text of the entryPoint.instruction
                            entryPoint.autoCompleteResults.Add(itemIndex.ToString());//add the result to the results list
                        }
                    }
                }
                else
                {
                    foreach (ItemIndex itemIndex in ItemCatalog.allItems)
                    {//foreach item
                        if (!itemIndex.ToString().ToLowerInvariant().Contains("count"))
                        {
                            entryPoint.autoCompleteResults.Add(itemIndex.ToString());//add the result to the results list
                        }
                    }
                }
                Print("BAC: amount of results found:" + entryPoint.autoCompleteResults.Count, 4);
                entryPoint.autoCompleteResults = LevenshteinSort(entryPoint.autoCompleteResults, entryPoint.instruction);
            }
        }

        /// <summary>
        /// A method which fills the B.A.C. systems' results list with buff names that have the characters present in the parameter.
        /// </summary>
        internal void ACFillWithBuffs()
        {
            if (entryPoint.currentFoundACRI.ToLowerInvariant().Equals("buff"))
            {//if the entryPoint.ACRI is 'buff'
                Print("BAC: got buff", 5);

                if (BAC.entryPoint.Instruction != "")
                {
                    foreach (BuffIndex buffIndex in (BuffIndex[])Enum.GetValues(typeof(BuffIndex)))
                    {//foreach buffIndex
                        if (entryPoint.instruction.Length != 0 && buffIndex.ToString().ToLowerInvariant().Contains(entryPoint.instruction.ToLowerInvariant())
                            && !buffIndex.ToString().ToLowerInvariant().Contains("count"))
                        {//if the current entryPoint.instruction is not empty and the current buff contains the text of the entryPoint.instruction
                            entryPoint.autoCompleteResults.Add(buffIndex.ToString());//add the result to the results list
                        }
                    }
                }
                else
                {
                    foreach (BuffIndex buffIndex in (BuffIndex[])Enum.GetValues(typeof(BuffIndex)))
                    {//foreach buffIndex
                        if (!buffIndex.ToString().ToLowerInvariant().Contains("count"))
                        {
                            entryPoint.autoCompleteResults.Add(buffIndex.ToString());//add the result to the results list
                        }
                    }
                }
                Print("BAC: amount of results found:" + entryPoint.autoCompleteResults.Count, 4);
                entryPoint.autoCompleteResults = LevenshteinSort(entryPoint.autoCompleteResults, entryPoint.instruction);
            }
        }

        /// <summary>
        /// A method which fills the B.A.C. systems' results list with equipment names that have the characters present in the parameter.
        /// </summary>
        internal void ACFillWithEquipment()
        {
            if (entryPoint.currentFoundACRI.ToLowerInvariant().Equals("equipment"))
            {//if the entryPoint.ACRI is 'equipment'

                if (BAC.entryPoint.Instruction != "")
                {
                    foreach (EquipmentIndex equipmentIndex in EquipmentCatalog.allEquipment)
                    {//foreach equipment
                        if (entryPoint.instruction.Length != 0 && equipmentIndex.ToString().ToLowerInvariant().Contains(entryPoint.instruction.ToLowerInvariant())
                            && !equipmentIndex.ToString().ToLowerInvariant().Contains("count"))
                        {//if the current entryPoint.instruction is not empty and the current equipment contains the text of the entryPoint.instruction
                            entryPoint.autoCompleteResults.Add(equipmentIndex.ToString());//add the result to the results list
                        }
                    }
                }
                else
                {
                    foreach (EquipmentIndex equipmentIndex in EquipmentCatalog.allEquipment)
                    {//foreach equipment
                        if (!equipmentIndex.ToString().ToLowerInvariant().Contains("count"))
                        {
                            entryPoint.autoCompleteResults.Add(equipmentIndex.ToString());//add the result to the results list
                        }
                    }
                }
                Print("BAC: amount of results found:" + entryPoint.autoCompleteResults.Count, 4);
                entryPoint.autoCompleteResults = LevenshteinSort(entryPoint.autoCompleteResults, entryPoint.instruction);
            }
        }

        /// <summary>
        /// A method which fills the B.A.C. systems' results list with team names that have the characters present in the parameter.
        /// </summary>
        /// <param name="getAll"></param>
        internal void ACFillWithTeams()
        {
            if (entryPoint.currentFoundACRI.ToLowerInvariant().Equals("team"))
            {//if the entryPoint.ACRI is 'item'
                Print("BAC: got team", 5);
                if (BAC.entryPoint.Instruction != "")
                {
                    foreach (TeamIndex teamIndex in (TeamIndex[])Enum.GetValues(typeof(TeamIndex)))
                    {//foreach item
                        if (entryPoint.instruction.Length != 0 && teamIndex.ToString().ToLowerInvariant().Contains(entryPoint.instruction.ToLowerInvariant())
                            && !teamIndex.ToString().ToLowerInvariant().Contains("count"))
                        {//if the current entryPoint.instruction is not empty and the current item contains the text of the entryPoint.instruction
                            entryPoint.autoCompleteResults.Add(teamIndex.ToString());//add the result to the results list
                        }
                    }
                }
                else
                {
                    foreach (TeamIndex teamIndex in (TeamIndex[])Enum.GetValues(typeof(TeamIndex)))
                    {//foreach item
                        if (!teamIndex.ToString().ToLowerInvariant().Contains("count"))
                        {
                            entryPoint.autoCompleteResults.Add(teamIndex.ToString());//add the result to the results list
                        }
                    }
                }
                Print("BAC: amount of results found:" + entryPoint.autoCompleteResults.Count, 4);
                entryPoint.autoCompleteResults = LevenshteinSort(entryPoint.autoCompleteResults, entryPoint.instruction);
            }
        }

        /// <summary>
        /// Provides extenstion mods a listen port to extend the B.A.C. system (the extensible listen point). This should only ever run once per extention.
        /// </summary>
        /// <param name="args">Argument 1 (List[string]): The list of all custom ACRI the extention uses in argument 2 (both normal and special.) All ACRI in this list will have the same functionality provided in argument 2. 
        /// <br/>Argument 2 (Action): The "ACFillWith..." method containing the custom functionality of the extention mod.</param>
        public void BACListener(List<object> args)
        {
            if (args.Count < 2) 
            {
                throw new ConCommandException("A mod providing an extention to B.A.C. has not provided enough arguments for \"BACListener\". " +
                    "There must be at least 2 arguments; List<string> \"CustomACRI\", Action \"ACFillWith...\"");
            }

            if (!(args[0] is List<string>)) 
            {
                throw new ConCommandException("A mod providing an extention to B.A.C. has not provided the correct argument type, \"List<string>\", for argument 1 of \"BACListener\".");
            }

            if (!(args[1] is Action)) 
            {
                throw new ConCommandException("A mod providing an extention to B.A.C. has not provided the correct argument type, \"Action\", for argument 2 of \"BACListener\".");
            }

            List<string> customACRI = (List<string>)args[0];
            Action ACFillWithCustomAction = (Action)args[1];
            bool normalAdded = false;//only allow the ACFillWith... be added to the normal method pool once
            bool specialAdded = false;//only allow the ACFillWith... be added to the special method pool once

            entryPoint.ACRI.AddRange(customACRI);//add the custom ACRI to the ACRI pool
            foreach (var item in customACRI)
            {
                if (normalAdded && specialAdded) { break; }

                if (!normalAdded && !item.StartsWith("CC "))
                {
                    entryPoint.methods.Add(() => ACFillWithCustomAction());
                    normalAdded = true;
                }
                else if(!specialAdded && item.StartsWith("CC "))
                {
                    entryPoint.specialMethods.Add(() => ACFillWithCustomAction());
                    specialAdded = true;
                }
            }

            //call heirarchy:
            //extention -> BACListener (extention adds to BAC's internal)
            //BAC internal -> extention's "ACFillWith..." (when the user inputs the second parameter into the CC; extention mod initializes the custom code)
            //extention's "ACFillWith..." -> ACFillWithCustom (extention mod tells B.A.C. to then preform checks)
                //ACFillWithCustom -> extention's "CustomStringFromList" (B.A.C. asks the extention mod for the string conversion of each the custom enumerables)
                //extention's "CustomStringFromList" -> ACCustomStringFromListReturnPoint (extention returns string conversion to B.A.C.)
            //ACFillWithCustom -> extentions defined custom code (if checks return true; calls extentions custom code)
                //Extention's custom code -> || ->! AutoCompleteResultsAdd (extention finally either does (->) or does not (->!) add the current enumerable as a string to the results list, depending on the custom code)
        }

        /// <summary>
        /// Only use if creating a softly dependent extention via; "pluginInfo.Instance.SendMessage("ACCustomStringFromListReturnPoint", #your string#);" where #your string# is the object (from "CustomStringFromList") returned as a string.
        /// <br/>For example; most of the time you'd do item.ToString(), but if the 'item' object is a unity object (like a player) you can still inline the extention through this.
        /// <br/>This Simply returns the input string into the "CustomReturnedString" property to be used by "ACFillWithCustom".
        /// </summary>
        /// <param name="ReturningString"></param>
        /// <returns></returns>
        public void ACCustomStringFromListReturnPoint(string ReturningString)
        {
            entryPoint.CustomReturnedString = ReturningString;
        }

        /// <summary>
        /// Tether runner of extensible; simply runs the extentions' code.
        /// </summary>
        /// <param name="args">Argument 1 (Action[object]): The custom code which is expected to add its result to the results list which runs when the current ACRI is one of the custom ACRI (in argument 2).
        /// <br/>Argument 2: List[string]): The ACRI which must be present in the current console command for the custom code (in argument 1) to run.
        /// <br/>Argumetn 3: List[object]): The custom enumerable which is looped through; this should be the list which is enumerated through when the player presses 'TAB' in the console.
        /// <br/>Argument 4: Action[object]): The conversion method for converting the current enumerated item into a string. For example converting the current player object into a string by getting the players' username.</param>
        public void ACFillWithCustom(List<object> args)
        {
            if (args.Count < 4) 
            {
                throw new ConCommandException("A mod providing an extention to B.A.C. has not provided enough arguments for \"ACFillWithCustom\". " +
                    "There must be at least 4 arguments; Action<object> \"CustomAction\", List<string> \"CustomACRIMatch\", List<object> \"CustomEnumerable\" and Action<object> \"CustomStringFromListByAction\".");
            }

            if (!args[0].GetType().FullName.StartsWith("System.Action") && !args[0].GetType().FullName.Contains("[[")) 
            {
                throw new ConCommandException("A mod providing an extention to B.A.C. has not provided the correct argument type, \"Action<object>\", for argument 1 of \"ACFillWithCustom\".");
            }

            if (!(args[1] is List<string>))
            {
                throw new ConCommandException("A mod providing an extention to B.A.C. has not provided the correct argument type, \"List<string>\", for argument 2 of \"ACFillWithCustom\".");
            }

            if (!args[2].GetType().FullName.StartsWith("System.List") && !args[2].GetType().FullName.Contains("[["))
            {
                throw new ConCommandException("A mod providing an extention to B.A.C. has not provided the correct argument type, \"List<object>\", for argument 3 of \"ACFillWithCustom\".");
            }

            if (!args[3].GetType().FullName.StartsWith("System.Action") && !args[3].GetType().FullName.Contains("[["))
            {
                throw new ConCommandException("A mod providing an extention to B.A.C. has not provided the correct argument type, \"Action<object>\", for argument 4 of \"ACFillWithCustom\".");
            }

            Action<object> CustomAction = (Action<object>)args[0];
            List<string> customACRI = (List<string>)args[1];
            List<object> list = (List<object>)args[2];
            Action<object> CustomStringFromList = (Action<object>)args[3];

            bool ACRICheck = false;
            foreach (var ACRI in customACRI)
            {//for each custom ACRI check
                if (entryPoint.CurrentFoundACRI.Equals(ACRI, StringComparison.InvariantCultureIgnoreCase))
                {//if the current ACRI matches any of the custom ACRI checks
                    ACRICheck = true;
                    break;
                }
            }

            if (ACRICheck)
            {
                foreach (object item in list)
                {//for each item in the custom enumerable
                    if (entryPoint.Instruction != "")
                    {//if there is an entryPoint.instruction (if the current parameter is not empty)
                        CustomStringFromList(item);//tell the extention mod to convert the input item into a string (returned via "ACCustomStringFromListReturnPoint()" method into the "CustomReturnedString" property)
                        if (entryPoint.CustomReturnedString.ToLowerInvariant().Contains(entryPoint.instruction.ToLowerInvariant()))
                        {//if the current instuction is contained in the current item as a string
                            CustomAction(item);//add the matching item
                        }
                    }
                    else
                    {//otherwise if the entryPoint.instruction is empty
                        CustomAction(item);//add the item
                    }
                }

                levenshteinSort();//sort the list
            }
        }

        //for the sake of keeping this from requiring R2API unessasarily; debug and verbos must be pre-set or "CO-Misc" must be installed.
        public static void Print(string message, ushort verbosReq, [System.Runtime.CompilerServices.CallerMemberName] string callerName = "")
        {
            if (debug && verbos >= (uint)verbosReq)
            {
                MonoBehaviour.print(callerName + ": " + message);
            }
        }

        public static bool debug = false;

        public static uint verbos = 1u;

        private string CustomReturnedString = "";

        /// <summary>
        /// The result suggestions created from B.A.C.
        /// </summary>
        private List<string> autoCompleteResults = new List<string>();

        public List<string> AutoCompleteResults { get => entryPoint.autoCompleteResults; internal set => entryPoint.autoCompleteResults = value; }

        public void AutoCompleteResultsAdd(string item) => entryPoint.autoCompleteResults.Add(item);

        private uint tabIndicatorPos = 0u;

        /// <summary>
        /// The current index the B.A.C system is currently at within the <c>AutoCompleteResults</c> list.
        /// </summary>
        public uint TabIndicatorPos { get => entryPoint.tabIndicatorPos; internal set => entryPoint.tabIndicatorPos = value; }

        /// <summary>
        /// The amount of parameters there currently are (or previously where if the user has removed text)
        /// </summary>
        internal int previousParamCount = 0;

        /// <summary>
        /// A list called the "Auto Complete Recognition Item" used to determine what to search for in a CC, 
        /// if a CC contains any items in this list, the B.A.C system will attempt to generate the suggestions (pushed into <c>AutoCompleteResults</c>) to itterate through.
        /// </summary>
        public List<string> ACRI = new List<string> { "Player", "Equipment", "Item", "Buff", "Team"};

        /// <summary>
        /// A list of the methods to call when preforming a B.A.C fill. NEVER CLEAR THIS!
        /// </summary>
        public List<Action> methods = new List<Action>();

        /// <summary>
        /// A list of the special case methods to call when preforming a B.A.C fill, for CCs that require a fill not provided here or that have strict naming. NEVER CLEAR THIS!
        /// </summary>
        public List<Action> specialMethods = new List<Action>();

        private List<string> instructions = new List<string>();//list of all entryPoint.instructions

        /// <summary>
        /// A list of all entryPoint.instructions the B.A.C. system has found in the current command
        /// </summary>
        public List<string> Instructions { get => entryPoint.instructions; internal set => entryPoint.instructions = value; }

        private string currentFoundACRI = "";

        /// <summary>
        /// The currently found "Auto-Complete Recognition Items" which will be used to fill the suggestions
        /// </summary>
        public string CurrentFoundACRI { get => entryPoint.currentFoundACRI; internal set => entryPoint.currentFoundACRI = value; }

        private string instruction = "";

        /// <summary>
        /// The current parameter being tabbed on
        /// </summary>
        public string Instruction { get => entryPoint.instruction; internal set => entryPoint.instruction = value; }
    }
}
