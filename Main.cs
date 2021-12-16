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
        public const string VERSION = "1.0.0";
        #endregion

        // User logic
        override protected Assembly CurrentAssembly
        => Assembly.GetExecutingAssembly();
        override protected Type[] ModsOrderingList
        => new[]
        {
            typeof(TimeMods),
            typeof(Movement),
            typeof(Guard),
            typeof(Combo),
            typeof(ChipsCogs),          
            typeof(Camera),

            typeof(UI),
            typeof(Audiovisual),

            typeof(Various),
            typeof(ParryChallenge),
        };
        override protected string[] PresetNames
        => Utility.GetEnumValuesAsStrings<Preset>().ToArray();
    }
}