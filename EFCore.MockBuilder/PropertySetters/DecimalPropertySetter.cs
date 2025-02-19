namespace EFCore.MockBuilder.PropertySetters;

internal sealed class DecimalPropertySetter : PropertySetter<decimal>
{
    public DecimalPropertySetter() : base((faker, propertyInfo) =>
    {
        var rangeAttr = propertyInfo.GetCustomAttribute<RangeAttribute>();
        if (rangeAttr is null) 
            return faker.Random.Decimal();

        var min = Convert.ToDecimal(rangeAttr.Minimum);
        var max = Convert.ToDecimal(rangeAttr.Maximum);

        return faker.Random.Decimal(min, max);
    })
    { }
}