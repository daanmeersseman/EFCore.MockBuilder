using Bogus;

namespace EFCore.MockBuilder.Abstractions;

public interface IPropertySetter
{
    Type ForType { get; }

    Func<Faker, PropertyInfo, object> Setter { get; }
}

public abstract class PropertySetter<T>: IPropertySetter
{
    public Type ForType => typeof(T);

    public Func<Faker, PropertyInfo, object> Setter { get; }

    protected PropertySetter(Func<Faker, PropertyInfo, object> setter)
    {
        Setter = setter;
    }
}