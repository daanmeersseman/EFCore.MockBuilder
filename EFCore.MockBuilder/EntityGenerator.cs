using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Bogus;

namespace EFCore.MockBuilder;

public static class EntityGenerator
{
    public static TEntity Generate<TEntity>() where TEntity : class, new()
    {
        var faker = new Faker<TEntity>()
            .StrictMode(false);

        var type = typeof(TEntity);
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var propertyInfo in properties)
        {
            if (propertyInfo.CanWrite)
            {
                if (HandleStringProperty(faker, propertyInfo)) continue;
                if (HandleNumericProperty(faker, propertyInfo)) continue;
                if (HandleEmailProperty(faker, propertyInfo)) continue;
                if (HandleDateTimeProperty(faker, propertyInfo)) continue;
                if (HandleGuidProperty(faker, propertyInfo)) continue;
                if (HandleBooleanProperty(faker, propertyInfo)) continue;
                if (HandleEnumProperty(faker, propertyInfo)) continue;
                // Add more handlers as needed
            }
        }

        return faker.Generate();
    }

    private static bool HandleStringProperty<TEntity>(Faker<TEntity> faker, PropertyInfo propertyInfo)
        where TEntity : class
    {
        if (propertyInfo.PropertyType != typeof(string))
            return false;

        var maxLength = GetMaxLength(propertyInfo);
        var minLength = GetMinLength(propertyInfo);

        int min = minLength ?? 1;
        int max = maxLength ?? 50; // Default max length if not specified

        faker.RuleFor(propertyInfo.Name, f => f.Random.String2(min, max));

        return true;
    }

    private static bool HandleNumericProperty<TEntity>(Faker<TEntity> faker, PropertyInfo propertyInfo)
        where TEntity : class
    {
        if (!IsNumericType(propertyInfo.PropertyType))
            return false;

        var rangeAttribute = propertyInfo.GetCustomAttribute<RangeAttribute>();
        double min = rangeAttribute != null ? Convert.ToDouble(rangeAttribute.Minimum) : 0;
        double max = rangeAttribute != null ? Convert.ToDouble(rangeAttribute.Maximum) : 100;

        faker.RuleFor(propertyInfo.Name, f => GenerateRandomNumber(f, propertyInfo.PropertyType, min, max));

        return true;
    }

    private static bool HandleEmailProperty<TEntity>(Faker<TEntity> faker, PropertyInfo propertyInfo)
        where TEntity : class
    {
        if (propertyInfo.GetCustomAttribute<EmailAddressAttribute>() != null)
        {
            faker.RuleFor(propertyInfo.Name, f => f.Internet.Email());
            return true;
        }

        return false;
    }

    private static bool HandleDateTimeProperty<TEntity>(Faker<TEntity> faker, PropertyInfo propertyInfo)
        where TEntity : class
    {
        if (propertyInfo.PropertyType == typeof(DateTime))
        {
            faker.RuleFor(propertyInfo.Name, f => f.Date.Past());
            return true;
        }

        return false;
    }

    private static bool HandleGuidProperty<TEntity>(Faker<TEntity> faker, PropertyInfo propertyInfo)
        where TEntity : class
    {
        if (propertyInfo.PropertyType == typeof(Guid))
        {
            faker.RuleFor(propertyInfo.Name, f => f.Random.Guid());
            return true;
        }

        return false;
    }

    private static bool HandleBooleanProperty<TEntity>(Faker<TEntity> faker, PropertyInfo propertyInfo)
        where TEntity : class
    {
        if (propertyInfo.PropertyType == typeof(bool))
        {
            faker.RuleFor(propertyInfo.Name, f => f.Random.Bool());
            return true;
        }

        return false;
    }

    private static bool HandleEnumProperty<TEntity>(Faker<TEntity> faker, PropertyInfo propertyInfo)
        where TEntity : class
    {
        if (propertyInfo.PropertyType.IsEnum)
        {
            var enumValues = Enum.GetValues(propertyInfo.PropertyType).Cast<object>();
            faker.RuleFor(propertyInfo.Name, f => f.PickRandom(enumValues));
            return true;
        }

        return false;
    }

    private static int? GetMaxLength(PropertyInfo propertyInfo)
    {
        var maxLengthAttr = propertyInfo.GetCustomAttribute<MaxLengthAttribute>();
        if (maxLengthAttr != null)
        {
            return maxLengthAttr.Length;
        }

        var stringLengthAttr = propertyInfo.GetCustomAttribute<StringLengthAttribute>();
        if (stringLengthAttr != null)
        {
            return stringLengthAttr.MaximumLength;
        }

        return null;
    }

    private static int? GetMinLength(PropertyInfo propertyInfo)
    {
        var minLengthAttr = propertyInfo.GetCustomAttribute<MinLengthAttribute>();
        if (minLengthAttr != null)
        {
            return minLengthAttr.Length;
        }

        var stringLengthAttr = propertyInfo.GetCustomAttribute<StringLengthAttribute>();
        if (stringLengthAttr != null)
        {
            return stringLengthAttr.MinimumLength;
        }

        return null;
    }

    private static bool IsNumericType(Type type)
    {
        return type == typeof(int) || type == typeof(long) || type == typeof(float) ||
               type == typeof(double) || type == typeof(decimal) || type == typeof(short) ||
               type == typeof(byte) || type == typeof(uint) || type == typeof(ulong) ||
               type == typeof(ushort) || type == typeof(sbyte);
    }

    private static object GenerateRandomNumber(Faker faker, Type type, double min, double max)
    {
        if (type == typeof(int))
            return faker.Random.Int((int)min, (int)max);
        if (type == typeof(long))
            return faker.Random.Long((long)min, (long)max);
        if (type == typeof(float))
            return faker.Random.Float((float)min, (float)max);
        if (type == typeof(double))
            return faker.Random.Double(min, max);
        if (type == typeof(decimal))
            return (decimal)faker.Random.Decimal((decimal)min, (decimal)max);
        if (type == typeof(short))
            return (short)faker.Random.Int((short)min, (short)max);
        if (type == typeof(byte))
            return (byte)faker.Random.Int((byte)min, (byte)max);
        // Handle other numeric types as needed
        return 0;
    }
}