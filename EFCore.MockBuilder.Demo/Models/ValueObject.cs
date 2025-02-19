using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace EFCore.MockBuilder.Demo.Models;

public class ValueObject
{
    private readonly string _value;

    private ValueObject(string value)
    {
        _value = value;
    }

    public static ValueObject Parse(string value)
    {
        return new ValueObject(value);
    }

    public override string ToString()
    {
        return _value;
    }
}

public class ValueObjectConverter() : ValueConverter<ValueObject, string>
(
    convertToProviderExpression: x => x.ToString(),
    convertFromProviderExpression: x => ValueObject.Parse(x)
);