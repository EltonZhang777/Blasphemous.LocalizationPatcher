# Blasphemous LocalizationPatcher
---

## Features

- Supports adding custom language localization to Blasphemous
- Supports editing existing localization texts by customizable patches
- Supports customizing patching order and language order (as appeared in the "Settings" tab of Blasphemous)
- Supports disabling patches or languages (vanilla or custom languages) by editing the config file `LocalizationPatcher.cfg`	

## Bug report

There is a rare potential that this mod, when an error occured, will remove all languages in the game, leaving the whole game without any text.

If this (or any other bug) occurs, locate your log file at `[your Blasphemous directory]\BepInEx\LogOutput.log`, and send it as a discussion thread [here](https://github.com/EltonZhang777/Blasphemous.LocalizationPatcher/discussions) or DM NewbieElton (discord id: eltonzhang777) on Discord.

## Tutorial

### how to install language patches

1. Choose and download existing language patches [here](https://github.com/EltonZhang777/Blasphemous.LocalizationPatcher/tree/main/Language%20Patches)
2. Move the downloaded patches into `[your Blasphemous directory]\Modding\data\LocalizationPatcher\`
3. re-launch Blasphemous

### how to disable languages/patches

1. Open `LocalizationPatcher.cfg` at `[your Blasphemous directory]\Modding\config`
2. enter the names of the languages/patches you'd like to disable. Make sure to encase each name with quote marks `" "`, and add a comma `,` after each name if it isn't the last name in the list.

	An example would be  `"disabledLanguages": ["Portuguese (Brazil)", "Korean", "Italian"]` 
3. Save the config file and re-launch Blasphemous

### how to create your own language patch

1. create a `.txt` text file under `[your Blasphemous directory]\Modding\data\LocalizationPatcher\` and name it following the format of `[Language Name]_[Language Code]_[YourPatch Name]` 
	
	(e.g., `English_en_MyPatch.txt`). 

2. Locate the key of the specific string you want to edit. 

	e.g., if you want to edit the lore of the prayer "Debla of the Lights", you can first open `English_en_base.txt`, search (`Ctrl + F`) for the original text (*"Eyes I have, but thee I can't see"*). The key for this string is `Prayer/PR03_LORE`

3. Specify the type of edit you want to make

	There are currently 4 types of edits supported by this mod:
	
	- `Prefix` or `AppendAtBeginning`
	 - Add your custom text as a prefix, attached to the beginning of the original text.
	- `Suffix` or `AppendAtEnd`
	 - Add your custom text as a suffix, attached to the end of the original text.
	- `Replace`
	 - Replace the original text with your custom text. Will not affect prefixes and suffixes added by this mod.
	- `ReplaceAll`
	 - Replace the original text and all of its custom prefixes & suffixes with your custom text.
	
	Note that prefixes and suffixes currently does not override each other, and can only stack on top of each other.
	- e.g., if two prefixes `prefixA@` and `prefixB@` are edited for `Prayer/PR03_CAPTION`, the final result will be `prefixA@prefixB@Debla of the Lights`

4. Edit your custom text. 

	Line-break character is `@`. Do **NOT** use `Enter`, `Shift + Enter`, or `\n`, those will not be interpreted as line-breaks.
	
	Also note that prefixes and suffixes do not automatically add a line-break to separate themselves from the main text. Pay attention to adding your own line-breaks if you intend to do so.

5. Format your edit as a line in the format of `[text key] -> [type of edit] : [your custom text]`

	e.g., `Prayer/PR03_LORE -> Replace : I replaced the lore!@Ha-ha! `

6. Save your `.txt` file and launch Blasphemous to see if the patch runs properly.

	if it does not run properly or triggers any bugs, read the log file at `[your Blasphemous directory]\BepInEx\LogOutput.log` to see what's wrong.


## Planned Features

- more compatible API with other mods
- console command support
- disable specific patch through config file (without disabling the whole language or other languages of this patch) 

if you want to suggest any features, send it as a discussion thread [here](https://github.com/EltonZhang777/Blasphemous.LocalizationPatcher/discussions) or DM NewbieElton (discord id: eltonzhang777) on Discord.

## Credits

- This mod is built on [Damocles](https://github.com/BrandenEK/)'s mod [Blasphemous.Andalusian](https://github.com/BrandenEK/Blasphemous.Andalusian)
