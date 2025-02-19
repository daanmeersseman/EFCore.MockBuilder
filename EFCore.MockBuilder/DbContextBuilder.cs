using System.Linq.Expressions;
using Bogus;
using Microsoft.EntityFrameworkCore;

namespace EFCore.MockBuilder;

public class DbContextBuilder<TContext> where TContext : DbContext
{
    private readonly TContext _context;

    private readonly Dictionary<Type, object> _customGenerators = new();
    
    private readonly Dictionary<Type, Func<Faker, PropertyInfo, object>> _propertySetters;

    public DbContextBuilder(TContext context)
    {
        _context = context;
        _propertySetters = DefaultPropertySetters.Get();
    }

    public DbContextBuilder<TContext> WithPropertySetter<TPropertySetter>() where TPropertySetter : IPropertySetter
    {
        var propertySetter = (IPropertySetter)Activator.CreateInstance(typeof(TPropertySetter))!;
        _propertySetters[propertySetter.ForType] = propertySetter.Setter;
        return this;
    }

    public DbContextBuilder<TContext> WithPropertySetter<TProperty>(Func<Faker, PropertyInfo, object> setter)
    {
        _propertySetters[typeof(TProperty)] = setter;
        return this;
    }

    public EntityBuilder<TEntity> Add<TEntity>() where TEntity : class, new()
    {
        var entity = CreateFaker<TEntity>().Generate();
        _context.Set<TEntity>().Add(entity);
        return new EntityBuilder<TEntity>(this, entity);
    }

    public EntityBuilder<TEntity>[] Add<TEntity>(int count) where TEntity : class, new()
    {
        var entities = CreateFaker<TEntity>().Generate(count);
        _context.Set<TEntity>().AddRange(entities);
        return entities.Select(e => new EntityBuilder<TEntity>(this, e)).ToArray();
    }

    public void ConfigureGenerator<TEntity>(Action<Faker<TEntity>> configure) where TEntity : class
    {
        _customGenerators[typeof(TEntity)] = configure;
    }

    public TContext Build()
    {
        _context.SaveChanges();
        return _context;
    }

    private Faker<TEntity> CreateFaker<TEntity>() where TEntity : class, new()
    {
        var faker = new Faker<TEntity>().StrictMode(true);

        if (_customGenerators.TryGetValue(typeof(TEntity), out var customGeneratorObj))
        {
            var customGenerator = (Action<Faker<TEntity>>)customGeneratorObj;
            customGenerator.Invoke(faker);
            return faker;
        }

        var properties = typeof(TEntity).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite);

        foreach (var prop in properties)
        {
            var propName = prop.Name;
            var propType = prop.PropertyType;
            var underlyingType = Nullable.GetUnderlyingType(propType) ?? propType;

            if (_propertySetters.TryGetValue(underlyingType, out var propertySetter))
            {
                faker.RuleFor(propName, f => propertySetter(f, prop));
                continue;
            }

            if (underlyingType.IsEnum)
            {
                var values = Enum.GetValues(underlyingType);
                faker.RuleFor(propName, f => values
                    .GetValue(f.Random.Int(0, values.Length - 1)));
                continue;
            }

            var param = Expression.Parameter(typeof(TEntity), "e");
            var property = Expression.PropertyOrField(param, propName);
            var lambda = Expression.Lambda<Func<TEntity, object>>(Expression.Convert(property, typeof(object)), param);
            faker.Ignore(lambda);
        }

        return faker;
    }

    public class EntityBuilder<TEntity> where TEntity : class
    {
        private readonly DbContextBuilder<TContext> _dbContextBuilder;
        public TEntity Entity { get; }

        public EntityBuilder(DbContextBuilder<TContext> dbContextBuilder, TEntity entity)
        {
            _dbContextBuilder = dbContextBuilder;
            Entity = entity;
        }

        public EntityBuilder<TEntity> With(Action<TEntity> configureEntity)
        {
            configureEntity(Entity);
            return this;
        }

        public EntityBuilder<TRelated> AddRelated<TRelated>() where TRelated : class, new()
        {
            var relatedEntity = _dbContextBuilder.CreateFaker<TRelated>().Generate();
            _dbContextBuilder._context.Set<TRelated>().Add(relatedEntity);
            EstablishRelationship(Entity, relatedEntity);
            return new EntityBuilder<TRelated>(_dbContextBuilder, relatedEntity);
        }

        public EntityBuilder<TRelated> AddRelated<TRelated>(
            Expression<Func<TEntity, object>> mainEntityKeySelector,
            Expression<Func<TRelated, object>> relatedEntityKeySelector)
            where TRelated : class, new()
        {
            var relatedEntity = _dbContextBuilder.CreateFaker<TRelated>().Generate();
            _dbContextBuilder._context.Set<TRelated>().Add(relatedEntity);

            var mainKeyProperty = GetPropertyInfo(mainEntityKeySelector);
            var relatedKeyProperty = GetPropertyInfo(relatedEntityKeySelector);

            var mainKeyValue = mainKeyProperty.GetValue(Entity);
            relatedKeyProperty.SetValue(relatedEntity, mainKeyValue);

            return new EntityBuilder<TRelated>(_dbContextBuilder, relatedEntity);
        }

        public EntityBuilder<TEntity> RelateWith<TRelated>(TRelated relatedEntity) where TRelated : class
        {
            EstablishRelationship(Entity, relatedEntity);
            return this;
        }

        public EntityBuilder<TEntity> RelateWith<TRelated>(
            TRelated relatedEntity,
            Expression<Func<TEntity, object>> mainEntityKeySelector,
            Expression<Func<TRelated, object>> relatedEntityKeySelector) where TRelated : class
        {
            var mainKeyProperty = GetPropertyInfo(mainEntityKeySelector);
            var relatedKeyProperty = GetPropertyInfo(relatedEntityKeySelector);

            var mainKeyValue = mainKeyProperty.GetValue(Entity);
            relatedKeyProperty.SetValue(relatedEntity, mainKeyValue);

            return this;
        }

        private void EstablishRelationship<TPrincipal, TDependent>(TPrincipal principalEntity, TDependent dependentEntity)
            where TPrincipal : class
            where TDependent : class
        {
            var principalType = _dbContextBuilder._context.Model.FindEntityType(typeof(TPrincipal));
            var dependentType = _dbContextBuilder._context.Model.FindEntityType(typeof(TDependent));

            var foreignKeys = dependentType.GetForeignKeys();

            foreach (var fk in foreignKeys)
            {
                if (fk.PrincipalEntityType == principalType)
                {
                    var principalKey = fk.PrincipalKey.Properties.First();
                    var dependentForeignKey = fk.Properties.First();

                    var principalKeyValue = principalKey.PropertyInfo.GetValue(principalEntity);
                    dependentForeignKey.PropertyInfo.SetValue(dependentEntity, principalKeyValue);

                    break;
                }
            }
        }

        private PropertyInfo GetPropertyInfo<TSource>(Expression<Func<TSource, object>> propertyLambda)
        {
            var type = typeof(TSource);

            MemberExpression member = propertyLambda.Body as MemberExpression;
            if (member == null)
            {
                UnaryExpression unary = propertyLambda.Body as UnaryExpression;
                if (unary != null && unary.NodeType == ExpressionType.Convert)
                {
                    member = unary.Operand as MemberExpression;
                }
            }

            if (member == null)
                throw new ArgumentException($"Expression '{propertyLambda}' refers to a method, not a property.");

            var propInfo = member.Member as PropertyInfo;
            if (propInfo == null)
                throw new ArgumentException($"Expression '{propertyLambda}' refers to a field, not a property.");

            return propInfo;
        }
    }
}