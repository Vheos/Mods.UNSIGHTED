namespace Vheos.Mods.UNSIGHTED
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using BepInEx;
    using Tools.ModdingCore;
    using UnityEngine;
    using UnityEngine.UI;
    using Vheos.Tools.Extensions.General;
    using Vheos.Tools.Extensions.Math;
    using Vheos.Tools.Extensions.UnityObjects;

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
        static public T[][] ToArray2D<T>(this Vector2Int t)
        {
            var r = new T[t.x][];
            for (int i = 0; i < t.x; i++)
                r[i] = new T[t.y];
            return r;
        }

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
        static public TextAnchor FlipHorizontally(this TextAnchor t)
        {
            switch (t)
            {
                case TextAnchor.UpperLeft: return TextAnchor.UpperRight;
                case TextAnchor.UpperRight: return TextAnchor.UpperLeft;
                case TextAnchor.MiddleLeft: return TextAnchor.MiddleRight;
                case TextAnchor.MiddleRight: return TextAnchor.MiddleLeft;
                case TextAnchor.LowerLeft: return TextAnchor.LowerRight;
                case TextAnchor.LowerRight: return TextAnchor.LowerLeft;
                default: return t;
            }
        }
        static public void FlipAlignmentHorizontally(this LayoutGroup t)
        => t.childAlignment = t.childAlignment.FlipHorizontally();
        static public void CreateMutualLinkWith(this TButtonNavigation t, TButtonNavigation a, AxisDirections direction)
        {
            switch (direction)
            {
                case AxisDirections.UP:
                    t.onUp = a.gameObject;
                    a.onDown = t.gameObject;
                    break;
                case AxisDirections.RIGHT:
                    t.onRight = a.gameObject;
                    a.onLeft = t.gameObject;
                    break;
                case AxisDirections.LEFT:
                    t.onLeft = a.gameObject;
                    a.onRight = t.gameObject;
                    break;
                case AxisDirections.DOWN:
                    t.onDown = a.gameObject;
                    a.onUp = t.gameObject;
                    break;
            }
        }
        static public void CreateMutualLinkWith(this GameObject t, GameObject a, AxisDirections direction)
        {
            if (t.TryGetComponent(out TButtonNavigation tButtonNav)
            && a.TryGetComponent(out TButtonNavigation aButtonNav))
                tButtonNav.CreateMutualLinkWith(aButtonNav, direction);
        }
        static public void CreateMutualLinkWith(this Component t, Component a, AxisDirections direction)
        => t.gameObject.CreateMutualLinkWith(a.gameObject, direction);

        static public void CreateMutualLinks<T>(this IList<IList<T>> t, bool isLooping = false) where T : Component
        {
            // get button navs
            var gameObjects = new GameObject[t.Count][];
            for (int ix = 0; ix < t.Count; ix++)
            {
                gameObjects[ix] = new GameObject[t[ix].Count];
                for (int iy = 0; iy < t[ix].Count; iy++)
                    if (t[ix][iy].TryNonNull(out var component)
                    && component.TryGetComponent(out TButtonNavigation buttonNav))
                        gameObjects[ix][iy] = buttonNav.gameObject;
            }

            for (int ix = 0; ix < gameObjects.Length; ix++)
                for (int iy = 0; iy < gameObjects[ix].Length; iy++)
                    if (gameObjects[ix][iy] != null)
                    {
                        var buttonNav = gameObjects[ix][iy].GetComponent<TButtonNavigation>();
                        buttonNav.reafirmNeighboors = false;
                        buttonNav.onLeft = isLooping || ix > 0
                                         ? gameObjects[ix.Add(-1).PosMod(gameObjects.Length)][iy]
                                         : null;
                        buttonNav.onRight = isLooping || ix < gameObjects.Length - 1
                                          ? gameObjects[ix.Add(+1).PosMod(gameObjects.Length)][iy]
                                          : null;
                        buttonNav.onUp = isLooping || iy > 0
                                       ? gameObjects[ix][iy.Add(-1).PosMod(gameObjects[ix].Length)]
                                       : null;
                        buttonNav.onDown = isLooping || iy < gameObjects[ix].Length - 1
                                         ? gameObjects[ix][iy.Add(+1).PosMod(gameObjects[ix].Length)]
                                         : null;
                    }
        }

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