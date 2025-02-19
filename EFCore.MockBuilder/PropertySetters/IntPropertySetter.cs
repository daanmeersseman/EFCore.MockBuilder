namespace EFCore.MockBuilder.PropertySetters;

internal sealed class IntPropertySetter : PropertySetter<int>
{
    public IntPropertySetter() : base((faker, propertyInfo) =>
    {
        var rangeAttr = propertyInfo.GetCustomAttribute<RangeAttribute>();
        if (rangeAttr is null) 
            return faker.Random.Int();

        var min = Convert.ToInt32(rangeAttr.Minimum);
        var max = Convert.ToInt32(rangeAttr.Maximum);

        return faker.Random.Int(min, max);
    })
    { }
}