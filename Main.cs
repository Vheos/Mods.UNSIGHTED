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
        public const string NAME = "Vheos Mod Pack";
        public const string VERSION = "0.1.0";
        #endregion

        // User logic
        override protected Assembly CurrentAssembly
        => Assembly.GetExecutingAssembly();
        override protected Type[] ModsOrderingList
        => new[]
        {            
            typeof(Combat),
            typeof(Combo),
            typeof(Guard),            

            typeof(TimeMods),
            typeof(Camera),
            typeof(Visual), 
            
            typeof(Various),
            typeof(ParryChallenge),
        };
        override protected string[] PresetNames
        => Utility.GetEnumValuesAsStrings<Preset>().ToArray();
    }
}