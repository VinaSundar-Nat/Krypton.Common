using System;
using System.Linq.Expressions;
using Kr.Common.Infrastructure.Datastore.Model;
using Microsoft.EntityFrameworkCore;

namespace Kr.Common.Infrastructure;

public static class QueryableExtensions
{
    public static IQueryable<TEntity> ApplyFilters<TEntity, TValue>(
        this IQueryable<TEntity> source,
        IEnumerable<DbFilter<TValue>> filters,
        bool caseInsensitiveStrings = true) where TEntity : class
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (filters == null || !filters.Any())
            return source;

        var parameter = Expression.Parameter(typeof(TEntity), "x");
        var combinedExpression = filters
            .Select(filter => BuildFilterExpression<TEntity, TValue>(parameter, filter, caseInsensitiveStrings))
            .Aggregate((current, next) => Expression.AndAlso(current, next));

        var lambda = Expression.Lambda<Func<TEntity, bool>>(combinedExpression, parameter);
        return source.Where(lambda);
    }

    private static Expression BuildFilterExpression<TEntity, TValue>(
        ParameterExpression parameter,
        DbFilter<TValue> filter,
        bool caseInsensitiveStrings)
    {
        ValidateFilter(filter);

        var propertyInfo = typeof(TEntity).GetProperty(filter.Property)
            ?? throw new ArgumentException($"Property '{filter.Property}' not found on type {typeof(TEntity).Name}");

        object convertedValue;
        var valueType = filter.Value.GetType();
        if (propertyInfo.PropertyType.IsAssignableFrom(valueType))
        {
            convertedValue = filter.Value;
        }
        else if (!TryConvert(filter.Value, propertyInfo.PropertyType, out convertedValue))
        {
            throw new ArgumentException($"Value type {valueType.Name} is not compatible with property type {propertyInfo.PropertyType.Name} for property '{filter.Property}'.");
        }

        var property = Expression.Property(parameter, propertyInfo);
        var constant = Expression.Constant(convertedValue, propertyInfo.PropertyType);

        return CreateOperationExpression(property, constant, filter.Operation, propertyInfo.PropertyType, caseInsensitiveStrings);
    }

    private static void ValidateFilter<TValue>(DbFilter<TValue> filter)
    {
        if (string.IsNullOrEmpty(filter.Property))
            throw new ArgumentException("Property name cannot be null or empty.", nameof(filter.Property));
        if (filter.Value == null)
            throw new ArgumentException($"Value for property '{filter.Property}' cannot be null.", nameof(filter.Value));
    }

    private static bool TryConvert(object value, Type targetType, out object convertedValue)
    {
        convertedValue = null;
        try
        {
            convertedValue = Convert.ChangeType(value, targetType);
            return true;
        }
        catch (Exception ex) when (ex is InvalidCastException || ex is FormatException || ex is OverflowException)
        {
            return false;
        }
    }

    private static Expression CreateOperationExpression(
        MemberExpression property,
        ConstantExpression constant,
        FilterOperation operation,
        Type propertyType,
        bool caseInsensitiveStrings)
    {
        return operation switch
        {
            FilterOperation.Equal => BuildEqualExpression(property, constant, propertyType, caseInsensitiveStrings),
            FilterOperation.NotEqual => Expression.Not(BuildEqualExpression(property, constant, propertyType, caseInsensitiveStrings)),
            FilterOperation.GreaterThan => Expression.GreaterThan(property, constant),
            FilterOperation.GreaterThanOrEqual => Expression.GreaterThanOrEqual(property, constant),
            FilterOperation.LessThan => Expression.LessThan(property, constant),
            FilterOperation.LessThanOrEqual => Expression.LessThanOrEqual(property, constant),
            _ => throw new ArgumentException($"Unsupported filter operation: {operation}")
        };
    }

    private static Expression BuildEqualExpression(
        MemberExpression property,
        ConstantExpression constant,
        Type propertyType,
        bool caseInsensitiveStrings)
    {
        if (propertyType == typeof(string) && caseInsensitiveStrings)
        {
            var toLowerMethod = typeof(string).GetMethod("ToLower", Type.EmptyTypes);
            var propertyLower = Expression.Call(property, toLowerMethod);
            var constantValue = (string)constant.Value;
            var constantLower = Expression.Constant(constantValue.ToLower());
            return Expression.Equal(propertyLower, constantLower);
        }

        return Expression.Equal(property, constant);
    }
}
