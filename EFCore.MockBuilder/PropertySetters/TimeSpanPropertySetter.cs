namespace EFCore.MockBuilder.PropertySetters;

internal sealed class TimeSpanPropertySetter : PropertySetter<TimeSpan>
{
    public TimeSpanPropertySetter() : base((faker, _) => faker.Date.Timespan())
    { }
}