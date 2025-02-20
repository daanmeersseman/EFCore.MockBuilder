namespace EFCore.MockBuilder.PropertySetters;

internal sealed class StringPropertySetter : PropertySetter<string>
{
    public StringPropertySetter() : base((faker, propertyInfo) =>
    {
        if (propertyInfo.GetCustomAttribute<EmailAddressAttribute>() != null)
            return faker.Internet.Email();

        if (propertyInfo.GetCustomAttribute<UrlAttribute>() != null)
            return faker.Internet.Url();

        if (propertyInfo.GetCustomAttribute<PhoneAttribute>() != null)
            return faker.Phone.PhoneNumber();

        var maxLength = propertyInfo.GetCustomAttribute<MaxLengthAttribute>()?.Length ??
                        propertyInfo.GetCustomAttribute<StringLengthAttribute>()?.MaximumLength;

        var minLength = propertyInfo.GetCustomAttribute<MinLengthAttribute>()?.Length ??
                        propertyInfo.GetCustomAttribute<StringLengthAttribute>()?.MinimumLength;

        return faker.Random.String2(minLength ?? 1, maxLength ?? 20);
    })
    { }
}