![console overhaul](https://github.com/8BitShadow/media-resources/blob/main/console%20overhaul.png?raw=true)
# Better Auto Complete, a better auto-complete method.
## About
"Better Auto Complete" is a mod which which adds more auto-complete functions to the RoR2 console, allowing a user to press the <kbd>TAB</kbd> key to both auto-complete the word and cycle through the suggestions - instead of exclusively using the <kbd>↓</kbd> key.<br>
The name was made before DebugToolkit was even public...so...
<br><br>

B.A.C. achieves this by first checking if <kbd>TAB</kbd> was pressed every keystroke, if an Auto-Complete Recognition Item (ACRI) is present in the command, parsing the argument into a levinshtein sort where it then replaces the argument with the currently 'tabbed on' suggestion.
<br><br>
This mod is intented to be used alongside mods which add new Console Commands (CCs), streamlining the use of the console for both single-players and admins/hosts.<br>
Due to the mod relying on a 'text-based recognition' algorithm (the ["ACR" system (wiki-todo)]()), the mod is compatible with **all** CC based mods - requiring little to no additional work on the part of the mod's developer to make it compatible.
<br><br>
The mod is also extensible; this means other mods that have their own enumerable sets that their CCs can use may inject that enumerable into B.A.C. For example a mod may add a CC which only targets enemy NPCs, thus this specific CC would want to cycle through enemy NPC names/object-IDs - the mod can generate that list and inject it, along with a recognition token (a new ACRI), directly into B.A.C. allowing for *any* CC with that specific ACRI in its name to be able to cycle through the newly added enumerable.<br>
A mod can also create a 'special fill' extension where B.A.C. will only itterate through the injected enumerable on a specific set of console commands instead of the ACRI, like the CC `COBind` (from ['TSBind'](https://github.com/8BtS-A-to-IA/Console-Overhaul-TSBind)) will be the only case where all possible non-bound key-binds will be cycled through as it is specially filled to only ever run specifically on the `COBind` CC - unless changed by other mods.

## Usage
### At user level
As a user; to use B.A.C you must first have at least one mod installed which adds new console commands.<br>
The only commands which will be compatibile 'out of the box' will be any that have [these (todo)]() words in them.<br>
You can test if the mod is working by just typing any of these words into the command console, then--with a space sperating the command-- a word or letters like 'ay' and pressing the <kbd>TAB</kbd> key.

### At modder level
B.A.C. can work seamlessly with any new CC from any mod as long as it follows the simple naming convention; in the name of the CC, have the order of [identifiers (todo)]() be in the same order as the arguments for your CC. That's it.<br>
If you want to make an extention mod for B.A.C. [read this guide (todo)]()

## due to an issue with uploading files directly into the repo via the github website, the files have been temporarily placed into a .zip file.

## development
### How can I develop for this project?
After cloning the repository and ensuring you have any version of [VS 2017/2019](https://visualstudio.microsoft.com/) installed, you should be able to simply open the `.snl` file to open the project in VS.
<br><br>
Before posting a merge request, please ensure you've:
- Adequately checked for 'top level' bugs
- Provided enough commenting/sudo-code for other contributers to quickly understand the process (if necessary)

For the sake of documenting bugfixes, when posting a merge request, please ensure you detail any changes by:
- Describing what was changed (in the head)
- How the changes where made (in the 'extended description')
  - If the merge request only adds new code and does not edit any pre-existing code, feel free to only fill the head.

### How do I compile and run this?
There are no special steps to building and compiling the code, simply press 'run' in <abbr title="Visual Studio">VS</abbr>.<br>
If you do not have the [export helper](https://github.com/8BtS-A-to-IA/VS.DLL-export-helper) installed; simply press 'Ok' if an error appears saying "A project with an Output Type of Class Library cannot be started directly". Visual Studio will have the `.dll` file you need generated in `bin>Debug` for VS 2017 or `bin>Debug>netstandard2.0` for VS 2019, simply copy the `.dll` file into the BepInEx `plugins` folder and start RoR2.<br>
If you have the exporter helper tool setup correctly; after pressing 'run' in VS, simply start <abbr title="Risk of Rain 2">RoR2</abbr>.

### How can I help without any programming 'know-how'?
Simply install the mod/modpack from [the modpacks main page](https://github.com/8BtS-A-to-IA/Console-Overhaul) and play. If you encounter any issues make sure to log it and provide as much relevant detail as possible in the relevant mods' `issue` page--or the main page if you don't know which mod is the problem--after checking if the same issue has not already been encountered, you can use the formatting guide to help with this.<br>
Don't worry about if you predict the wrong mod as the cause, it's more important to just have the report out there.

## Legals:
- This project makes use of the [levinshtein sort](https://www.dotnetperls.com/levenshtein).<br>

## Changelog:
<details>
    <summary>V1.0.0 (unreleased):</summary>
  
  - none yet!
</details>
