
using System;
using UnityEngine;

namespace Vheos.Mods.UNSIGHTED;
public abstract class AGetter
{
    protected bool _hasBeenSet;
    protected bool TestForWarnings(Type type) => _hasBeenSet && WarningInputAlreadySet(type);
    protected bool WarningInputAlreadySet(Type type)
    {
        Debug.LogWarning($"InputAlreadySet:\ttrying to override an already defined component input of type {type.Name}\n" +
        $"Fallback:\treturn without changing anything");
        return true;
    }
}

public class Getter<TReturn> : AGetter
{
    // Publics
    public void Set(Func<TReturn> getFunction)
    {
        if (TestForWarnings(typeof(TReturn)))
            return;

        _getFunction = getFunction;
        _hasBeenSet = true;
    }
    public TReturn Value
    => _getFunction();
    public static implicit operator TReturn(Getter<TReturn> t)
    => t._getFunction();

    // Privates   
    private Func<TReturn> _getFunction = () => default;
}

public class Getter<T1, TReturn> : AGetter
{
    // Publics
    public void Set(Func<T1, TReturn> getFunction)
    {
        if (TestForWarnings(typeof(TReturn)))
            return;

        _getFunction = getFunction;
        _hasBeenSet = true;
    }
    public TReturn Value(T1 arg1)
    => _getFunction(arg1);
    public TReturn this[T1 arg1]
    => _getFunction(arg1);

    // Privates   
    private Func<T1, TReturn> _getFunction = (arg1) => default;
}