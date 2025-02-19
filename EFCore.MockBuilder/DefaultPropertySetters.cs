using System.Collections.Concurrent;
using Bogus;
using Microsoft.EntityFrameworkCore;

namespace EFCore.MockBuilder;

public static class DefaultPropertySetters
{
    private static readonly ConcurrentDictionary<Type, Func<Faker, PropertyInfo, object>> PropertySetters = new();

    static DefaultPropertySetters()
    {
        var propertySetters = typeof(IPropertySetter).Assembly.GetTypes().Where(x =>
            x is { IsClass: true, IsAbstract: false } && x.IsAssignableTo(typeof(IPropertySetter)));

        foreach (var propertySetterType in propertySetters)
        {
            var propertySetter = (IPropertySetter)Activator.CreateInstance(propertySetterType)!;
            PropertySetters[propertySetter.ForType] = propertySetter.Setter;
        }
    }

    public static void AddPropertySetter<TPropertySetter>() where TPropertySetter : IPropertySetter
    {
        var propertySetter = (IPropertySetter)Activator.CreateInstance(typeof(TPropertySetter))!;
        PropertySetters[propertySetter.ForType] = propertySetter.Setter;
    }

    public static void AddPropertySetter<TProperty>(Func<Faker, PropertyInfo, object> setter)
    {
        PropertySetters[typeof(TProperty)] = setter;
    }

    public static Dictionary<Type, Func<Faker, PropertyInfo, object>> Get()
    {
        return new Dictionary<Type, Func<Faker, PropertyInfo, object>>(PropertySetters);
    }
}