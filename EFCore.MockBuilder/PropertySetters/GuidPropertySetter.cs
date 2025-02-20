namespace EFCore.MockBuilder.PropertySetters;

internal sealed class GuidPropertySetter : PropertySetter<Guid>
{
    public GuidPropertySetter() : base((faker, _) => faker.Random.Guid())
    { }
}