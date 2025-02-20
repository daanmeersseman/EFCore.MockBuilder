namespace EFCore.MockBuilder.PropertySetters;

internal sealed class ShortPropertySetter : PropertySetter<short>
{
    public ShortPropertySetter() : base((faker, propertyInfo) =>
    {
        var rangeAttr = propertyInfo.GetCustomAttribute<RangeAttribute>();
        if (rangeAttr == null) 
            return faker.Random.Short();

        var min = Convert.ToInt16(rangeAttr.Minimum);
        var max = Convert.ToInt16(rangeAttr.Maximum);

        return faker.Random.Short(min, max);
    })
    { }
}