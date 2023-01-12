
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Vheos.Mods.UNSIGHTED;
public static class InternalUtility
{
    public static object MoveNextThenGetCurrent(this IEnumerator t)
    {
        t.MoveNext();
        return t.Current;
    }
    public static T ChooseThresholdValue<T>(this float t, T defaultValue, params (float Threshold, T Value)[] thresholdValuePairs)
    {
        for (int i = thresholdValuePairs.Length - 1; i >= 0; i--)
            if (t >= thresholdValuePairs[i].Threshold)
                return thresholdValuePairs[i].Value;
        return defaultValue;
    }
    public static string FirstLetterCapitalized(this string t) => string.IsNullOrEmpty(t) ? t : t[0].ToString().ToUpper() + t.Substring(1);
    public static bool TryGetComponentInChildren<T>(this GameObject t, out T a) where T : Component
    {
        a = t.GetComponentInChildren<T>();
        return a != null;
    }
    public static bool TryGetComponentInChildren<T>(this Component t, out T a) where T : Component
    => t.gameObject.TryGetComponentInChildren(out a);
    public static T[][] ToArray2D<T>(this Vector2Int t)
    {
        var r = new T[t.x][];
        for (int i = 0; i < t.x; i++)
            r[i] = new T[t.y];
        return r;
    }
    public static int RandomRange(this Vector2Int t)
    => Random.Range(t.x, t.y + 1);
    public static float RandomRangeFloat(this Vector2Int t)
    => Random.Range(t.x, (float)t.y);
    public static float RandomRange(this Vector2 t)
    => Random.Range(t.x, t.y);
    public static float RandomRangeInt(this Vector2 t)
    => Random.Range(t.x.Round(), t.y.Round() + 1);
    public static bool RandomFlip()
    => Random.value < 0.5f;
    public static T RandomElement<T>(this IList<T> t)
    => t[Random.Range(0, t.Count)];
    public static void SetClampMax(this ref int t, int a)
    => t = t.ClampMax(a);
    public static void SetClampMin(this ref int t, int a)
    => t = t.ClampMin(a);
    public static bool IsHex(this char t)
    => t is >= '0' and <= '9'
    or >= 'a' and <= 'f'
    or >= 'A' and <= 'F';

    public static IEnumerable<T> GetComponentsInHierarchy<T>(this Component t, int fromDepth, int toDepth) where T : Component
    {
        if (fromDepth <= 0 && toDepth >= 0)
            foreach (var component in t.GetComponents<T>())
                yield return component;

        foreach (Transform child in t.transform)
            foreach (var component in child.GetComponentsInHierarchy<T>(fromDepth - 1, toDepth - 1))
                yield return component;
    }
    public static RectTransform Rect(this GameObject t)
    => t.GetComponent<RectTransform>();
    public static RectTransform Rect(this Component t)
    => t.gameObject.Rect();
    public static TextAnchor FlipHorizontally(this TextAnchor t)
    => t switch
    {
        TextAnchor.UpperLeft => TextAnchor.UpperRight,
        TextAnchor.UpperRight => TextAnchor.UpperLeft,
        TextAnchor.MiddleLeft => TextAnchor.MiddleRight,
        TextAnchor.MiddleRight => TextAnchor.MiddleLeft,
        TextAnchor.LowerLeft => TextAnchor.LowerRight,
        TextAnchor.LowerRight => TextAnchor.LowerLeft,
        _ => t,
    };
    public static void FlipAlignmentHorizontally(this LayoutGroup t)
    => t.childAlignment = t.childAlignment.FlipHorizontally();
    public static void CreateMutualLinkWith(this TButtonNavigation t, TButtonNavigation a, AxisDirections direction)
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
    public static void CreateMutualLinkWith(this GameObject t, GameObject a, AxisDirections direction)
    {
        if (t.TryGetComponent(out TButtonNavigation tButtonNav)
        && a.TryGetComponent(out TButtonNavigation aButtonNav))
            tButtonNav.CreateMutualLinkWith(aButtonNav, direction);
    }
    public static void CreateMutualLinkWith(this Component t, Component a, AxisDirections direction)
    => t.gameObject.CreateMutualLinkWith(a.gameObject, direction);

    public static void CreateMutualLinks<T>(this IList<IList<T>> t, bool isLooping = false) where T : Component
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
    public static bool TryFind<T>(this IEnumerable<T> t, Func<T, bool> test, out T r)
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
    public static bool TryFindIndex<T>(this IEnumerable<T> t, T a, out int r)
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