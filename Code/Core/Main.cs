namespace Vheos.Mods.UNSIGHTED
{
    using System;
    using System.Linq;
    using System.Reflection;
    using BepInEx;
    using Tools.ModdingCore;
    using Utility = Tools.UtilityN.Utility;

    [BepInDependency("com.bepis.bepinex.configurationmanager", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin(GUID, NAME, VERSION)]
    public class Main : BepInExEntryPoint
    {
        #region SETTINGS
        public const string GUID = "Vheos.Mods.UNSIGHTED";
        public const string NAME = "UNSIGHTED++";
        public const string VERSION = "1.6.0";
        #endregion

        // User logic
        override protected Assembly CurrentAssembly
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
        override protected Type[] ModsOrderingList
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
        override protected string[] PresetNames
        => Utility.GetEnumValuesAsStrings<SettingsPreset>().ToArray();
    }
}