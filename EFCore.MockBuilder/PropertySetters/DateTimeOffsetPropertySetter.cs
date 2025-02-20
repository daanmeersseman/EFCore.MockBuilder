namespace EFCore.MockBuilder.PropertySetters;

internal sealed class DateTimeOffsetPropertySetter : PropertySetter<DateTimeOffset>
{
    public DateTimeOffsetPropertySetter() : base((faker, _) => faker.Date.PastOffset(20))
    { }
}