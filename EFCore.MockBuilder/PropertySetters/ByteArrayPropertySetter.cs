namespace EFCore.MockBuilder.PropertySetters;

internal sealed class ByteArrayPropertySetter : PropertySetter<byte[]>
{
    public ByteArrayPropertySetter() : base((faker, _) => faker.Random.Bytes(faker.Random.Int(1, 100)))
    { }
}