namespace EFCore.MockBuilder.PropertySetters;

internal sealed class FloatPropertySetter : PropertySetter<float>
{
    public FloatPropertySetter() : base((faker, propertyInfo) =>
    {
        var rangeAttr = propertyInfo.GetCustomAttribute<RangeAttribute>();

        if (rangeAttr is null)
            return faker.Random.Float();

        var min = Convert.ToSingle(rangeAttr.Minimum);
        var max = Convert.ToSingle(rangeAttr.Maximum);

        return faker.Random.Float(min, max);
    })
    { }
}