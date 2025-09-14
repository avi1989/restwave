using System;

namespace RestWave.Views.Components;

public static class Extensions
{
    public static void Let<T>(this T obj, Action<T> action) where T : class?
    {
        if (obj != null)
        {
            action(obj);
        }
    }

    public static TResult? Let<T, TResult>(this T obj, Func<T, TResult> func) where T : class?
    {
        return obj != null ? func(obj) : default(TResult);
    }
}