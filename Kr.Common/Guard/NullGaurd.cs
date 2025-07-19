using System;
namespace Kr.Common.Guard;

public static class NullGaurd
{
    public static object Gaurd(this object? context, string message = "")
    {
        if (context != null)
            return context;

        throw new ArgumentNullException(message);
    }

    public static Type Gaurd(this Type? context, string message = "")
    {
        if (context != null)
            return context;

        throw new ArgumentNullException(message);
    }


    public static bool AnyGaurd<T>(this IEnumerable<T>? context)
        => context?.Any() ?? false;
}


