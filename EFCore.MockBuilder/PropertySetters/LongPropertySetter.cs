namespace EFCore.MockBuilder.PropertySetters;

internal sealed class LongPropertySetter : PropertySetter<long>
{
    public LongPropertySetter() : base((faker, propertyInfo) =>
    {
        var rangeAttr = propertyInfo.GetCustomAttribute<RangeAttribute>();
        if (rangeAttr is null) 
            return faker.Random.Long();

        var min = Convert.ToInt64(rangeAttr.Minimum);
        var max = Convert.ToInt64(rangeAttr.Maximum);

        return faker.Random.Long(min, max);
    })
    { }
}