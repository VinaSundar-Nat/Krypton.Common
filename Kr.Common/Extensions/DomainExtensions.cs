namespace Kr.Common.Extensions;

public static class DomainExtensions
{
	public static IEnumerable<T> Guard<T>(this IEnumerable<T> source)
		=> source ?? Enumerable.Empty<T>() ;
}

