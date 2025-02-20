namespace EFCore.MockBuilder.PropertySetters;

internal sealed class DoublePropertySetter : PropertySetter<double>
{
    public DoublePropertySetter() : base((faker, propertyInfo) =>
    {
        var rangeAttr = propertyInfo.GetCustomAttribute<RangeAttribute>();
        if (rangeAttr is null)
            return faker.Random.Double();

        var min = Convert.ToDouble(rangeAttr.Minimum);
        var max = Convert.ToDouble(rangeAttr.Maximum);

        return faker.Random.Double(min, max);
    })
    { }
}