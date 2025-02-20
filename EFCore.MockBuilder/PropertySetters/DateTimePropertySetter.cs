namespace EFCore.MockBuilder.PropertySetters;

internal sealed class DateTimePropertySetter : PropertySetter<DateTime>
{
    public DateTimePropertySetter() : base((faker, _) => faker.Date.Past(20))
    { }
}