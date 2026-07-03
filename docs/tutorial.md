# Tutorial
---

## How to change fonts of displayed languages

You can change the font used by a specific language either via in-game console commands or by providing Unity `.assetbundle` font files.

### using system font

System fonts are fonts already installed on your operating system. You can apply them to any language via the in-game console.

1. List all available system fonts on your PC using console command: `font listsystem`
1. Apply a system font to a language: `font applysystem [fontName] [languageName]`
   - Use underscore `_` to represent spaces in font and language names.
   - Example: `font applysystem Microsoft_YaHei Chinese`

### using font from AssetBundles (code WIP!)

If you want to use a custom font (e.g., a pixel art font, a font with special glyphs), you need to provide it as a Unity AssetBundle.

1. **Prepare the font files**

   Create a Unity AssetBundle (`.ab`, `.assetbundle` or other extension) that contains:
   - A `.ttf` (TrueType) font file as the regular font asset.
   - A TextMeshPro font asset
   - A meshed font asset

2. **Create a font info JSON file**

   Place a `.json` file with the font metadata in your `[Blasphemous directory]\Modding\data\LocalizationPatcher\` folder, following this structure:

   ```json
   {
       "fontName": "vw-bmp-16",
       "fileName": "vonwaonbitmap-16px",
       "supportedLanguages": [
           "Chinese"
       ]
   }
   ```

   Field descriptions:

   - **`fontName`** — Name used to identify this font in commands (e.g., `font list`, `font apply`).
   - **`fileName`** — Name of the AssetBundle file (without extension), placed in the same directory as the JSON file.
   - **`supportedLanguages`** — List of language names this font can be applied to (e.g., `"Chinese"`, `"English"`, `"Korean"`).

3. **Place the AssetBundle file**

   Put the AssetBundle file in the same directory as its JSON info file:
   `[Blasphemous directory]\Modding\data\LocalizationPatcher\`

   So the folder would contain both files like:
   - `vonwaonbitmap-16px.json` (font info)
   - `vonwaonbitmap-16px.assetbundle` (AssetBundle)

4. **Apply the font via console**

   - List all loaded mod fonts: `font list`
   - List mod fonts applicable to a specific language: `font list [languageName]`
   - Show currently used fonts for a language: `font list [languageName] current`
   - Apply a mod font to a language: `font apply [fontName] [languageName]`
     - Example: `font apply vw-bmp-16 Chinese`
   - Revert to default fonts for a language: `font revert [languageName]`

5. **Registering fonts from other mods (for mod developers)**

   Other mods can register custom fonts programmatically:

   ```csharp
   // Create a ModFontInfo with the font metadata
   ModFontInfo fontInfo = new()
   {
       fontName = "MyCustomFont",
       fileName = "myfontbundle",
       supportedLanguages = ["Chinese", "Korean"]
   };

   // Create a ModFont from the info (loads the AssetBundle)
   ModFont modFont = new(fileHandler, "path/to/fontinfo.json");

   // Register the font using the ModServiceProvider
   provider.RegisterModFont(modFont);
   ```

---

## How to install language patches

1. Choose and download existing language patches [here](https://github.com/EltonZhang777/Blasphemous.LocalizationPatcher/tree/main/Language%20Patches)
2. Move the downloaded patches into `[your Blasphemous directory]\Modding\data\LocalizationPatcher\auto-load language patches`
3. re-launch Blasphemous

---

## How to disable languages/patches

1. Open `LocalizationPatcher.cfg` at `[your Blasphemous directory]\Modding\config`
2. enter the names of the languages/patches you'd like to disable. Make sure to encase each name with quote marks `" "`, and add a comma `,` after each name if it isn't the last name in the list.

	An example would be `"disabledLanguages": ["Portuguese (Brazil)", "Korean", "Italian"]` 
3. Save the config file and re-launch Blasphemous

---

## How to create your own language patch (the easy `txt` way)

1. create a `.txt` text file and name it following the format of `[Language Name]_[Language Code]_[YourPatch Name]` 
	
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

6. Save your `.txt` file, put it in the auto-load directory, and launch Blasphemous to see if the patch runs properly.

	if it does not run properly or triggers any bugs, read the log file at `[your Blasphemous directory]\BepInEx\LogOutput.log` to see what's wrong.

---

## How to create your own language patch (the `json` way)

> It is recommended to use JSON over txt, as JSON supports more features (conditional triggers, manual triggers, priority ordering, etc.) and offers better compatibility.

1. Create a `.json` file as your new patch, following this JSON structure:

	```json
	{
	  "patchName": "MyFirstPatch",
	  "languageName": "English",
	  "languageCode": "en",
	  "patchOrder": 0,
	  "patchType": "OnInitialize",
	  "patchFlag": null,
	  "patchTerms": [
	    {
	      "termKey": "Prayer/PR03_LORE",
	      "termContent": "I replaced the lore!@Ha-ha!",
	      "termOperation": "Replace"
	    },
	    {
	      "termKey": "Prayer/PR03_CAPTION",
	      "termContent": "prefix@",
	      "termOperation": "Prefix"
	    }
	  ]
	}
	```

2. Field descriptions

	- **`patchName`** — Unique name for the patch. Make sure not to use duplicate names when creating multiple patches.
	- **`languageName`** — The language to modify (e.g., `English`, `Spanish`, `Chinese`).
	- **`languageCode`** — Internal language code (e.g., `en`, `es`, `zh`).
	- **`parentModId`** — (Optional, auto-filled) The mod ID that registered this patch.
	- **`patchOrder`** — (Optional, default `0`) Priority for applying this patch. Higher numbers are applied first among patches registered by the same mod.
	- **`patchType`** — Trigger method for the patch:
		- `OnInitialize` — Automatically applied on game startup
		- `OnFlag` — Applied when a specific game flag is set to `true`
		- `Manually` — Triggered manually via the in-game console
	- **`patchFlag`** — (Optional, default `null`) When `patchType` is `OnFlag`, specifies the flag ID to listen for.
	- **`patchTerms`** — List of patch terms, each containing:
		- **`termKey`** — The text key (e.g., `Prayer/PR03_LORE`)
		- **`termContent`** — Custom text content. Use `@` for line breaks, **do not** use `\n` or press Enter directly
		- **`termOperation`** — Edit type:
			- `Replace` — Replace the original text (does not affect prefixes/suffixes added by other mods)
			- `ReplaceAll` — Replace the original text along with all custom prefixes and suffixes
			- `Prefix` — Add text before the original text
			- `Suffix` — Add text after the original text

3. Save the `.json` file, put it in the auto-load directory, and launch Blasphemous to test if the patch works properly.

	If it does not work or triggers any bugs, read the log file at `[Blasphemous directory]\BepInEx\LogOutput.log` to see what went wrong.

4. Advanced usage: trigger patches conditionally with `OnFlag`

	When `patchType` is set to `OnFlag`, the patch is automatically applied when the corresponding game flag becomes `true`, and automatically removed when it becomes `false`.

	```json
	{
	  "patchName": "ConditionalPatch",
	  "languageName": "English",
	  "languageCode": "en",
	  "patchType": "OnFlag",
	  "patchFlag": "EVENT_COMPLETED_DEMO",
	  "patchTerms": [
	    {
	      "termKey": "UI/LABEL_SKIP",
	      "termContent": "Continue",
	      "termOperation": "Replace"
	    }
	  ]
	}
	```

5. Triggering patches manually with `Manually`

	When `patchType` is set to `Manually`, the patch will not apply automatically. You need to use the following commands in the Cheat Console:

	- `languagepatch apply [patchName]` — Manually apply a patch
	- `languagepatch remove [patchName]` — Manually remove an applied patch
	- `languagepatch export [patchName]` — Export a loaded patch to JSON format (useful for migrating txt patches to JSON)

6. Tips

	- You can use multiple JSON patch files simultaneously. They will be applied in order of `patchOrder`.
	- Use command `languagepatch list` to view all currently loaded patches and their status.
	- Use command `languagepatch export [patchName]` to export existing patches to JSON format for easy backup or sharing.

---

## Language patch register for mod developers

Other mods can register language patches programmatically using the `RegisterLanguagePatch` extension method on `ModServiceProvider`.

### Registering from JSON

If you already have a `.json` patch file in your mod's data folder, load it via JSON deserialization:

```csharp
using Blasphemous.LocalizationPatcher.Components;
using Blasphemous.ModdingAPI.Files;

// Load the patch from a JSON file in your mod's data resources
LanguagePatch patch = FileHandler.LoadDataAsJson<LanguagePatch>(
    "path/to/your/patch.json");

// Register the patch with the LocalizationPatcher system
this.RegisterLanguagePatch(patch);
```

### Registering programmatically

You can also construct a `LanguagePatch` object directly in code:

```csharp
using Blasphemous.LocalizationPatcher.Components;
using static Blasphemous.LocalizationPatcher.Components.LanguagePatch;

// Create patch terms
List<PatchTerm> terms =
[
    new("Prayer/PR03_LORE", "Custom lore text!", PatchTerm.TermOperation.Replace),
    new("Prayer/PR03_CAPTION", "[Custom Prefix] ", PatchTerm.TermOperation.Prefix),
];

// Create the patch (will be applied on game startup)
LanguagePatch patch = new(
    patchName: "MyModPatch",
    languageName: "English",
    languageCode: "en",
    patchTerms: terms,
    patchType: PatchType.OnInitialize
);

// Register it
this.RegisterLanguagePatch(patch);
```

### Using flag-triggered patches

For patches that should only appear after a certain game event:

```csharp
LanguagePatch patch = new(
    patchName: "PostEventDialogue",
    languageName: "English",
    languageCode: "en",
    patchTerms: terms,
    patchType: PatchType.OnFlag,
    patchFlag: "EVENT_COMPLETED_DEMO"
);

this.RegisterLanguagePatch(patch);
```

The patch will be automatically applied when the flag `EVENT_COMPLETED_DEMO` is set to `true` in-game, and removed when set to `false`.

### Notes

- The `parentModId` field is automatically set by `RegisterLanguagePatch` to the registering mod's ID — do not set it manually.
- Patches with duplicate `patchName` values are ignored on subsequent registration attempts.
- Patch ordering is determined first by the mod's position in the `patchingModOrder` config, then by `patchOrder` (higher values are applied first within the same mod).
