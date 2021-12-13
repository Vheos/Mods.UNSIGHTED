namespace Vheos.Mods.UNSIGHTED
{
    using System;
    using System.Collections;
    using System.Reflection;
    using BepInEx;
    using Tools.ModdingCore;

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
        override public Assembly CurrentAssembly
        => Assembly.GetExecutingAssembly();
        public override Type[] ModsOrderingList
        => new[]
        {
            typeof(Time),
            typeof(Combat),
            typeof(Combo),
            typeof(Camera),
            typeof(ParryChallenge),
            typeof(Various),
        };

    }

    static public class Extensions
    {
        static public object MoveNextThenGetCurrent(this IEnumerator t)
        {
            t.MoveNext();
            return t.Current;
        }

        static public T ChooseThresholdValue<T>(this float t, T defaultValue, params (float Threshold, T Value)[] thresholdValuePairs)
        {
            for (int i = thresholdValuePairs.Length - 1; i >= 0; i--)
                if (t >= thresholdValuePairs[i].Threshold)
                    return thresholdValuePairs[i].Value;
            return defaultValue;
        }
    }
}