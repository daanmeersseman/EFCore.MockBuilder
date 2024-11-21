using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Bogus;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace EFCore.MockBuilder
{
    public class DbContextBuilder<TContext> where TContext : DbContext
    {
        private readonly TContext _context;
        private readonly Dictionary<Type, object> _customGenerators = new();

        public DbContextBuilder(TContext context)
        {
            _context = context;
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
            Faker<TEntity> faker;

            if (_customGenerators.TryGetValue(typeof(TEntity), out var customGeneratorObj))
            {
                var customGenerator = customGeneratorObj as Action<Faker<TEntity>>;
                faker = new Faker<TEntity>().StrictMode(true);
                customGenerator?.Invoke(faker);
                return faker;
            }

            faker = new Faker<TEntity>().StrictMode(true);
            var properties = typeof(TEntity).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite);

            void IgnoreProperty(string propertyName)
            {
                var param = Expression.Parameter(typeof(TEntity), "e");
                var property = Expression.PropertyOrField(param, propertyName);
                var lambda = Expression.Lambda<Func<TEntity, object>>(Expression.Convert(property, typeof(object)), param);
                faker.Ignore(lambda);
            }

            foreach (var prop in properties)
            {
                var propName = prop.Name;
                var propType = prop.PropertyType;
                var underlyingType = Nullable.GetUnderlyingType(propType) ?? propType;

                if (IsNavigationProperty(prop))
                {
                    IgnoreProperty(propName);
                    continue;
                }

                var rangeAttr = prop.GetCustomAttribute<RangeAttribute>();

                if (underlyingType == typeof(string))
                {
                    var maxLength = prop.GetCustomAttribute<MaxLengthAttribute>()?.Length
                        ?? prop.GetCustomAttribute<StringLengthAttribute>()?.MaximumLength;
                    var minLength = prop.GetCustomAttribute<MinLengthAttribute>()?.Length
                        ?? prop.GetCustomAttribute<StringLengthAttribute>()?.MinimumLength;

                    if (prop.GetCustomAttribute<EmailAddressAttribute>() != null)
                    {
                        faker.RuleFor(propName, f => f.Internet.Email());
                    }
                    else if (prop.GetCustomAttribute<UrlAttribute>() != null)
                    {
                        faker.RuleFor(propName, f => f.Internet.Url());
                    }
                    else if (prop.GetCustomAttribute<PhoneAttribute>() != null)
                    {
                        faker.RuleFor(propName, f => f.Phone.PhoneNumber());
                    }
                    else
                    {
                        faker.RuleFor(propName, f => f.Random.String2(minLength ?? 1, maxLength ?? 20));
                    }
                }
                else if (underlyingType == typeof(bool))
                {
                    faker.RuleFor(propName, f => f.Random.Bool());
                }
                else if (underlyingType.IsEnum)
                {
                    faker.RuleFor(propName, f => Enum.GetValues(underlyingType)
                        .GetValue(f.Random.Int(0, Enum.GetValues(underlyingType).Length - 1)));
                }
                else if (underlyingType == typeof(Guid))
                {
                    faker.RuleFor(propName, f => f.Random.Guid());
                }
                else if (underlyingType == typeof(DateTime))
                {
                    faker.RuleFor(propName, f => f.Date.Past(20));
                }
                else if (underlyingType == typeof(DateTimeOffset))
                {
                    faker.RuleFor(propName, f => f.Date.PastOffset(20));
                }
                else if (underlyingType == typeof(TimeSpan))
                {
                    faker.RuleFor(propName, f => f.Date.Timespan());
                }
                else if (underlyingType == typeof(byte[]))
                {
                    faker.RuleFor(propName, f => f.Random.Bytes(f.Random.Int(1, 100)));
                }
                else if (underlyingType == typeof(byte))
                {
                    if (rangeAttr != null)
                    {
                        var min = Convert.ToByte(rangeAttr.Minimum);
                        var max = Convert.ToByte(rangeAttr.Maximum);
                        faker.RuleFor(propName, f => f.Random.Byte(min, max));
                    }
                    else
                    {
                        faker.RuleFor(propName, f => f.Random.Byte());
                    }
                }
                else if (underlyingType == typeof(short))
                {
                    if (rangeAttr != null)
                    {
                        var min = Convert.ToInt16(rangeAttr.Minimum);
                        var max = Convert.ToInt16(rangeAttr.Maximum);
                        faker.RuleFor(propName, f => f.Random.Short(min, max));
                    }
                    else
                    {
                        faker.RuleFor(propName, f => f.Random.Short());
                    }
                }
                else if (underlyingType == typeof(int))
                {
                    if (rangeAttr != null)
                    {
                        var min = Convert.ToInt32(rangeAttr.Minimum);
                        var max = Convert.ToInt32(rangeAttr.Maximum);
                        faker.RuleFor(propName, f => f.Random.Int(min, max));
                    }
                    else
                    {
                        faker.RuleFor(propName, f => f.Random.Int());
                    }
                }
                else if (underlyingType == typeof(long))
                {
                    if (rangeAttr != null)
                    {
                        var min = Convert.ToInt64(rangeAttr.Minimum);
                        var max = Convert.ToInt64(rangeAttr.Maximum);
                        faker.RuleFor(propName, f => f.Random.Long(min, max));
                    }
                    else
                    {
                        faker.RuleFor(propName, f => f.Random.Long());
                    }
                }
                else if (underlyingType == typeof(float))
                {
                    if (rangeAttr != null)
                    {
                        var min = Convert.ToSingle(rangeAttr.Minimum);
                        var max = Convert.ToSingle(rangeAttr.Maximum);
                        faker.RuleFor(propName, f => f.Random.Float(min, max));
                    }
                    else
                    {
                        faker.RuleFor(propName, f => f.Random.Float());
                    }
                }
                else if (underlyingType == typeof(double))
                {
                    if (rangeAttr != null)
                    {
                        var min = Convert.ToDouble(rangeAttr.Minimum);
                        var max = Convert.ToDouble(rangeAttr.Maximum);
                        faker.RuleFor(propName, f => f.Random.Double(min, max));
                    }
                    else
                    {
                        faker.RuleFor(propName, f => f.Random.Double());
                    }
                }
                else if (underlyingType == typeof(decimal))
                {
                    if (rangeAttr != null)
                    {
                        var min = Convert.ToDecimal(rangeAttr.Minimum);
                        var max = Convert.ToDecimal(rangeAttr.Maximum);
                        faker.RuleFor(propName, f => f.Random.Decimal(min, max));
                    }
                    else
                    {
                        faker.RuleFor(propName, f => f.Random.Decimal());
                    }
                }
                else
                {
                    IgnoreProperty(propName);
                }
            }

            return faker;
        }

        private bool IsNavigationProperty(PropertyInfo prop)
        {
            var propType = prop.PropertyType;
            var underlyingType = Nullable.GetUnderlyingType(propType) ?? propType;

            if (typeof(IEnumerable).IsAssignableFrom(underlyingType) && underlyingType != typeof(string))
            {
                if (!underlyingType.IsArray && underlyingType != typeof(byte[]))
                {
                    return true;
                }
            }

            if (underlyingType.IsClass && underlyingType != typeof(string) && underlyingType != typeof(byte[]))
            {
                if (!IsSimpleType(underlyingType))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsSimpleType(Type type)
        {
            return type.IsPrimitive || type.IsEnum || new Type[]
            {
                typeof(string),
                typeof(decimal),
                typeof(DateTime),
                typeof(DateTimeOffset),
                typeof(TimeSpan),
                typeof(Guid),
                typeof(byte[])
            }.Contains(type);
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
}
