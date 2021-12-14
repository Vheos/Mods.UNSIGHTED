namespace Vheos.Mods.UNSIGHTED
{
    using System;
    using System.Collections;
    using BepInEx;
    using Tools.ModdingCore;

    static public class InternalExtensions
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