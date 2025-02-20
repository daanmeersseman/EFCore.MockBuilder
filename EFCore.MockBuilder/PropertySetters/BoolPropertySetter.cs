namespace EFCore.MockBuilder.PropertySetters;

internal sealed class BoolPropertySetter : PropertySetter<bool>
{
    public BoolPropertySetter() : base((faker, _) => faker.Random.Bool()) 
    { }
}