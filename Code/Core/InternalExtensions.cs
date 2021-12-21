namespace Vheos.Mods.UNSIGHTED
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using BepInEx;
    using Tools.ModdingCore;
    using UnityEngine;
    using Vheos.Tools.Extensions.General;

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
        static public string FirstLetterCapitalized(this string t)
        {
            if (string.IsNullOrEmpty(t))
                return t;
            return t[0].ToString().ToUpper() + t.Substring(1);
        }
        static public bool TryGetComponentInChildren<T>(this GameObject t, out T a) where T : Component
        {
            a = t.GetComponentInChildren<T>();
            return a != null;
        }
        static public bool TryGetComponentInChildren<T>(this Component t, out T a) where T : Component
        => t.gameObject.TryGetComponentInChildren(out a);
        static public T[,] ToArray2D<T>(this Vector2Int t)
        => new T[t.x, t.y];
        static public IEnumerable<T> GetComponentsInHierarchy<T>(this Component t, int fromDepth, int toDepth) where T : Component
        {
            if (fromDepth <= 0 && toDepth >= 0)
                foreach (var component in t.GetComponents<T>())
                    yield return component;

            foreach (Transform child in t.transform)
                foreach (var component in child.GetComponentsInHierarchy<T>(fromDepth - 1, toDepth - 1))
                    yield return component;
        }
        static public RectTransform Rect(this GameObject t)
        => t.GetComponent<RectTransform>();
        static public RectTransform Rect(this Component t)
        => t.gameObject.Rect();

        // IEnumerable
        static public bool TryFind<T>(this IEnumerable<T> t, Func<T, bool> test, out T r)
        {
            foreach (var element in t)
                if (test(element))
                {
                    r = element;
                    return true;
                }
            r = default;
            return false;
        }
        static public bool TryFindIndex<T>(this IEnumerable<T> t, T a, out int r)
        {
            r = 0;
            foreach (var element in t)
            {
                if (element.Equals(a))
                    return true;
                r++;
            }
            return false;
        }
    }
}