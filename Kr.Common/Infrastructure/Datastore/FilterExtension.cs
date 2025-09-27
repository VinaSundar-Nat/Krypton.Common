using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
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
            .Select(filter =>
                BuildFilterExpression<TEntity, TValue>(parameter,
                filter, caseInsensitiveStrings))
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

        var property = BuildNestedPropertyExpression(parameter, filter.Property);
        var propertyInfo = GetPropertyInfo(property);

        var propertyType = propertyInfo.PropertyType;
        var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
        var valueType = typeof(TValue);
        var underlyingValueType = Nullable.GetUnderlyingType(valueType) ?? valueType;

        if (!IsCompatibleType(underlyingType, underlyingValueType))
        {
            throw new ArgumentException($"Value type {valueType.Name} is not compatible with property type {propertyType.Name} for property '{filter.Property}'.");
        }

        var convertedValue = ConvertValue(filter.Value, propertyType);
        var constant = Expression.Constant(convertedValue, propertyType);

        return CreateOperationExpression(property, constant, filter.Operation, propertyType, caseInsensitiveStrings);
    }

    private static MemberExpression BuildNestedPropertyExpression(Expression expression, string propertyName)
    {
        Expression property = expression;
        var type = expression.Type;

        foreach (var member in propertyName.Split('.'))
        {
            var propertyInfo = type.GetProperty(member, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (propertyInfo == null)
            {
                throw new ArgumentException($"Property '{member}' not found on type {type.Name}");
            }

            property = Expression.Property(property, propertyInfo);
            type = propertyInfo.PropertyType;
        }

        return (MemberExpression)property;
    }

    private static PropertyInfo GetPropertyInfo(MemberExpression memberExpression)
    {
        return memberExpression.Member as PropertyInfo
            ?? throw new ArgumentException($"Member '{memberExpression.Member.Name}' is not a property.");
    }

    private static bool IsCompatibleType(Type propertyType, Type valueType)
    {
        if (propertyType == valueType)
            return true;

        if (propertyType.IsAssignableFrom(valueType))
            return true;

        return CanImplicitlyConvert(valueType, propertyType);
    }

    private static bool CanImplicitlyConvert(Type from, Type to)
    {
        var numericTypes = new[]
        {
            typeof(byte), typeof(sbyte), typeof(short), typeof(ushort),
            typeof(int), typeof(uint), typeof(long), typeof(ulong),
            typeof(float), typeof(double), typeof(decimal)
        };

        if (!numericTypes.Contains(from) || !numericTypes.Contains(to))
            return false;

        return to == typeof(double) || to == typeof(decimal) || 
               (from == typeof(int) && (to == typeof(long) || to == typeof(float)));
    }

    private static object? ConvertValue(object? value, Type targetType)
    {
        if (value == null)
            return null;

        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        
        try
        {
            if (value.GetType() == underlyingType)
                return value;

            return Convert.ChangeType(value, underlyingType);
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"Cannot convert value '{value}' to type {targetType.Name}", ex);
        }
    }

    private static void ValidateFilter<TValue>(DbFilter<TValue> filter)
    {
        if (string.IsNullOrEmpty(filter.Property))
            throw new ArgumentException("Property name cannot be null or empty.", nameof(filter));
        if (!Enum.IsDefined(typeof(FilterOperation), filter.Operation))
            throw new ArgumentException($"Invalid filter operation: {filter.Operation}", nameof(filter));
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
            FilterOperation.GreaterThan => BuildComparisonExpression(property, constant, propertyType, Expression.GreaterThan),
            FilterOperation.GreaterThanOrEqual => BuildComparisonExpression(property, constant, propertyType, Expression.GreaterThanOrEqual),
            FilterOperation.LessThan => BuildComparisonExpression(property, constant, propertyType, Expression.LessThan),
            FilterOperation.LessThanOrEqual => BuildComparisonExpression(property, constant, propertyType, Expression.LessThanOrEqual),
            _ => throw new ArgumentException($"Unsupported filter operation: {operation}")
        };
    }

    private static Expression BuildComparisonExpression(
        MemberExpression property,
        ConstantExpression constant,
        Type propertyType,
        Func<Expression, Expression, BinaryExpression> comparisonFunc)
    {
        // Handle nullable types in comparisons
        if (Nullable.GetUnderlyingType(propertyType) != null)
        {
            var hasValueProperty = Expression.Property(property, "HasValue");
            var valueProperty = Expression.Property(property, "Value");
            var comparison = comparisonFunc(valueProperty, constant);
            return Expression.AndAlso(hasValueProperty, comparison);
        }

        return comparisonFunc(property, constant);
    }

    private static Expression BuildEqualExpression(
        MemberExpression property,
        ConstantExpression constant,
        Type propertyType,
        bool caseInsensitiveStrings)
    {
        // Handle null comparisons
        if (constant.Value == null)
        {
            return Expression.Equal(property, constant);
        }

        // Handle string comparisons with case insensitivity
        var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
        if (underlyingType == typeof(string) && caseInsensitiveStrings)
        {
            var toLowerMethod = typeof(string).GetMethod("ToLower", Type.EmptyTypes)
                ?? throw new MissingMethodException(nameof(String), "ToLower");

            Expression propertyExpression = property;
            
            // Handle nullable strings
            if (Nullable.GetUnderlyingType(propertyType) != null)
            {
                var hasValueProperty = Expression.Property(property, "HasValue");
                var valueProperty = Expression.Property(property, "Value");
                var propertyLower = Expression.Call(valueProperty, toLowerMethod);
                var constantValue = constant.Value as string;
                var constantLower = Expression.Constant(constantValue?.ToLower());
                var comparison = Expression.Equal(propertyLower, constantLower);
                return Expression.AndAlso(hasValueProperty, comparison);
            }
            else
            {
                var propertyLower = Expression.Call(propertyExpression, toLowerMethod);
                var constantValue = constant.Value as string;
                var constantLower = Expression.Constant(constantValue?.ToLower());
                return Expression.Equal(propertyLower, constantLower);
            }
        }

        return Expression.Equal(property, constant);
    }
}