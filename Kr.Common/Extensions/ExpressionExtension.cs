using System.Linq.Expressions;

namespace Kr.Common.Extensions;

public static class ExpressionExtension
{
    public static IEnumerable<MemberExpression> BuildMemberExpressions<T>(this T source, string expression = "meta")
        where T : class
    {
        var type = source.GetType();
        foreach (var property in type.GetProperties())
        {
            yield return CreateMemberExpression(type, property.Name, expression);
        }
    }

    public static MemberExpression CreateMemberExpression(Type type, string propertyName, string expression = "meta")
    {
        ParameterExpression parameter = Expression.Parameter(type, expression);
        MemberExpression propertyExpression = Expression.Property(parameter, propertyName);

        return propertyExpression;
    }

    public static TValue GetCustomAttributeValue<T, TAttribute, TValue>(this MemberExpression expression,
    Func<TAttribute, TValue> getValueOps)
    where TAttribute : Attribute
    {
        var attribute = expression.Member.GetCustomAttributes(typeof(TAttribute), true).FirstOrDefault() as TAttribute;
        if (attribute != null)
            return getValueOps(attribute);

        throw new ArgumentException("Operation error. Expression cannot be created.");
    }

    public static void ExpressionSetter<T>(this T source, string property, string value)
    {
        MemberExpression propertyExpression = Expression.Property(Expression.Constant(source), property);
        ConstantExpression valueExp = Expression.Constant(value);
        BinaryExpression assignment = Expression.Assign(propertyExpression, valueExp);
        Expression.Lambda<Action>(assignment).Compile().Invoke();
    }
}