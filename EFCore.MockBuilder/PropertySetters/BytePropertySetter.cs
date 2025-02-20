namespace EFCore.MockBuilder.PropertySetters;

internal sealed class BytePropertySetter : PropertySetter<byte>
{
    public BytePropertySetter() : base((faker, propertyInfo) =>
    {
        var rangeAttr = propertyInfo.GetCustomAttribute<RangeAttribute>();

        if (rangeAttr is null) 
            return faker.Random.Byte();

        var min = Convert.ToByte(rangeAttr.Minimum);
        var max = Convert.ToByte(rangeAttr.Maximum);

        return faker.Random.Byte(min, max);
    })
    { }
}