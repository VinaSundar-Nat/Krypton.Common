using System;

namespace Kr.Common.Extensions;

public static class ConversionExtensions
{
    public static T ToEnum<T>(this string value) where T : struct
    {
        if (string.IsNullOrEmpty(value))
            ArgumentNullException.ThrowIfNull(value, nameof(value));

        if (Enum.TryParse<T>(value, true, out var result))
            return result;

        throw new ArgumentException("Invalid enum string", nameof(value));
    }

}
