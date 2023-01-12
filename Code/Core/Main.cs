
using BepInEx;
using System;
using System.Linq;
using System.Reflection;
using Vheos.Mods.Core;
using Utility = Vheos.Helpers.Common.Utility;

namespace Vheos.Mods.UNSIGHTED;
[BepInDependency("com.bepis.bepinex.configurationmanager", BepInDependency.DependencyFlags.SoftDependency)]
[BepInPlugin(GUID, NAME, VERSION)]
public class Main : BepInExEntryPoint
{
    // Metadata
    public const string GUID = "Vheos.Mods.UNSIGHTED";
    public const string NAME = "UNSIGHTED++";
    public const string VERSION = "1.7.0";

    // User logic
    protected override Assembly CurrentAssembly
    => Assembly.GetExecutingAssembly();
    protected override void Initialize()
    {
        Log.Debug($"Initializing {typeof(CustomControls).Name}...");
        new CustomControls();

        Log.Debug($"Initializing {typeof(CustomSaves).Name}...");
        new CustomSaves();
    }
    protected override bool DelayedInitializeCondition
    => CustomControls.IsFullyInitialized;
    protected override Type[] ModsOrderingList
    => new[]
    {
        // Difficulty
        typeof(TimeMods),
        typeof(Movement),
        typeof(Guard),
        typeof(Combo),
        typeof(Enemies),
        typeof(ChipsCogs),
        typeof(Fishing),
        typeof(ParryChallenge),

        // QoL
        typeof(Menus),
        typeof(Camera),
        typeof(UI),
        typeof(Audiovisual),

        // Various
        typeof(Various),
        typeof(SFXPlayer),
    };
    protected override string[] PresetNames
    => Utility.GetEnumValuesAsStrings<SettingsPreset>().ToArray();
}