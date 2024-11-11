using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace EFCore.MockBuilder
{
    /// <summary>
    /// Builds a mocked DbContext with entities and relationships for unit testing.
    /// </summary>
    /// <typeparam name="TContext">The type of the DbContext.</typeparam>
    public class DbContextBuilder<TContext>(TContext context)
        where TContext : DbContext
    {
        internal TContext Context { get; } = context ?? throw new ArgumentNullException(nameof(context), "DbContext cannot be null.");

        private readonly Dictionary<Type, long> _primaryKeyCounters = new();

        /// <summary>
        /// Adds entities with dummy data to the DbContext.
        /// </summary>
        public EntityBuilder<TEntity, TContext> Add<TEntity>(int count = 1)
            where TEntity : class, new()
        {
            TEntity entity = null!;

            for (int i = 0; i < count; i++)
            {
                entity = GenerateEntityWithDummyData<TEntity>();
                SetPrimaryKey(entity);
                Context.Set<TEntity>().Add(entity);
            }

            return new EntityBuilder<TEntity, TContext>(entity, this);
        }

        /// <summary>
        /// Saves all changes to the DbContext.
        /// </summary>
        public TContext Build()
        {
            Context.SaveChanges();
            return Context;
        }

        /// <summary>
        /// Generates an entity with dummy data, ignoring navigation properties and respecting data annotations.
        /// </summary>
        internal TEntity GenerateEntityWithDummyData<TEntity>()
            where TEntity : class, new()
        {
            var entityType = Context.Model.FindEntityType(typeof(TEntity))
                ?? throw new InvalidOperationException($"Entity type {typeof(TEntity).Name} not found in DbContext model.");

            var navigationProperties = entityType.GetNavigations().Select(n => n.Name).ToList();

            var entity = EntityGenerator.Generate<TEntity>();

            // Ignore navigation properties
            foreach (var navPropName in navigationProperties)
            {
                var propertyInfo = typeof(TEntity).GetProperty(navPropName);
                if (propertyInfo != null && propertyInfo.CanWrite)
                {
                    var defaultValue = propertyInfo.PropertyType.IsValueType
                        ? Activator.CreateInstance(propertyInfo.PropertyType)
                        : null;
                    propertyInfo.SetValue(entity, defaultValue);
                }
            }

            return entity;
        }

        /// <summary>
        /// Sets unique primary key values for an entity.
        /// </summary>
        private void SetPrimaryKey<TEntity>(TEntity entity)
            where TEntity : class
        {
            var entityType = Context.Model.FindEntityType(typeof(TEntity))
                ?? throw new InvalidOperationException($"Entity type {typeof(TEntity).Name} not found in DbContext model.");

            var keyProperties = entityType.FindPrimaryKey()?.Properties
                ?? throw new InvalidOperationException($"No primary key defined for {typeof(TEntity).Name}.");

            foreach (var keyProperty in keyProperties)
            {
                var propertyInfo = typeof(TEntity).GetProperty(keyProperty.Name);
                if (propertyInfo?.CanWrite == true)
                {
                    var value = GenerateKeyValue(propertyInfo.PropertyType, typeof(TEntity));
                    propertyInfo.SetValue(entity, value);
                }
            }
        }

        /// <summary>
        /// Generates default key values based on property type.
        /// </summary>
        private object GenerateKeyValue(Type propertyType, Type entityType)
        {
            if (!_primaryKeyCounters.ContainsKey(entityType))
                _primaryKeyCounters[entityType] = 1L;

            var counterValue = _primaryKeyCounters[entityType];

            object value = propertyType switch
            {
                var t when t == typeof(int) => Convert.ToInt32(counterValue),
                var t when t == typeof(long) => counterValue,
                var t when t == typeof(short) => Convert.ToInt16(counterValue),
                var t when t == typeof(byte) => Convert.ToByte(counterValue),
                var t when t == typeof(uint) => Convert.ToUInt32(counterValue),
                var t when t == typeof(ulong) => Convert.ToUInt64(counterValue),
                var t when t == typeof(ushort) => Convert.ToUInt16(counterValue),
                var t when t == typeof(sbyte) => Convert.ToSByte(counterValue),
                var t when t == typeof(decimal) => Convert.ToDecimal(counterValue),
                var t when t == typeof(float) => Convert.ToSingle(counterValue),
                var t when t == typeof(double) => Convert.ToDouble(counterValue),
                var t when t == typeof(Guid) => Guid.NewGuid(),
                var t when t == typeof(string) => Guid.NewGuid().ToString(),
                _ => propertyType.IsValueType ? Activator.CreateInstance(propertyType)! : null!
            };

            _primaryKeyCounters[entityType] = counterValue + 1;
            return value;
        }
    }

    /// <summary>
    /// Provides methods to configure an entity and its relationships.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    public class EntityBuilder<TEntity, TContext>
        where TEntity : class
        where TContext : DbContext
    {
        public TEntity Entity { get; }
        internal DbContextBuilder<TContext> Builder { get; }

        public EntityBuilder(TEntity entity, DbContextBuilder<TContext> builder)
        {
            Entity = entity ?? throw new ArgumentNullException(nameof(entity));
            Builder = builder ?? throw new ArgumentNullException(nameof(builder));
        }

        /// <summary>
        /// Configures the entity using the provided setup action.
        /// </summary>
        public EntityBuilder<TEntity, TContext> With(Action<TEntity> setup)
        {
            setup?.Invoke(Entity);
            return this;
        }

        /// <summary>
        /// Adds a related entity with dummy data and establishes a relationship.
        /// </summary>
        public EntityBuilder<TRelated, TContext> AddRelated<TRelated>(
            Expression<Func<TEntity, object>>? mainEntityKeySelector = null,
            Expression<Func<TRelated, object>>? relatedEntityKeySelector = null)
            where TRelated : class, new()
        {
            var relatedEntity = Builder.GenerateEntityWithDummyData<TRelated>();
            Builder.Context.Set<TRelated>().Add(relatedEntity);

            RelateWith(relatedEntity, mainEntityKeySelector, relatedEntityKeySelector);

            return new EntityBuilder<TRelated, TContext>(relatedEntity, Builder);
        }

        /// <summary>
        /// Establishes a relationship with an existing entity.
        /// </summary>
        public EntityBuilder<TRelated, TContext> RelateWith<TRelated>(
            TRelated relatedEntity,
            Expression<Func<TEntity, object>>? mainEntityKeySelector = null,
            Expression<Func<TRelated, object>>? relatedEntityKeySelector = null)
            where TRelated : class
        {
            if (relatedEntity == null) throw new ArgumentNullException(nameof(relatedEntity));

            var (mainKeySelector, relatedKeySelector) = GetRelationshipSelectors<TRelated>();

            mainEntityKeySelector ??= mainKeySelector;
            relatedEntityKeySelector ??= relatedKeySelector;

            var mainEntityKey = mainEntityKeySelector.Compile()(Entity);
            var relatedPropertyInfo = GetPropertyInfo(relatedEntityKeySelector);

            relatedPropertyInfo?.SetValue(relatedEntity, mainEntityKey);

            return new EntityBuilder<TRelated, TContext>(relatedEntity, Builder);
        }

        /// <summary>
        /// Retrieves key selectors for establishing relationships based on the DbContext model.
        /// </summary>
        private (Expression<Func<TEntity, object>>, Expression<Func<TRelated, object>>) GetRelationshipSelectors<TRelated>()
            where TRelated : class
        {
            var entityType = Builder.Context.Model.FindEntityType(typeof(TEntity))
                ?? throw new InvalidOperationException($"Entity type {typeof(TEntity).Name} not found in DbContext model.");

            var relatedEntityType = Builder.Context.Model.FindEntityType(typeof(TRelated))
                ?? throw new InvalidOperationException($"Related entity type {typeof(TRelated).Name} not found in DbContext model.");

            // Attempt to find a foreign key from related entity to main entity
            var foreignKey = relatedEntityType.GetForeignKeys()
                .FirstOrDefault(fk => fk.PrincipalEntityType == entityType);

            bool isInverse = false;

            // If not found, attempt to find a foreign key from main entity to related entity
            if (foreignKey == null)
            {
                foreignKey = entityType.GetForeignKeys()
                    .FirstOrDefault(fk => fk.PrincipalEntityType == relatedEntityType);
                isInverse = true;
            }

            if (foreignKey == null)
            {
                throw new InvalidOperationException($"No foreign key relationship found between {typeof(TEntity).Name} and {typeof(TRelated).Name}.");
            }

            var principalKey = foreignKey.PrincipalKey.Properties.First();
            var dependentKey = foreignKey.Properties.First();

            if (!isInverse)
            {
                return (
                    CreatePropertyExpression<TEntity>(principalKey.Name),
                    CreatePropertyExpression<TRelated>(dependentKey.Name)
                );
            }
            else
            {
                return (
                    CreatePropertyExpression<TEntity>(dependentKey.Name),
                    CreatePropertyExpression<TRelated>(principalKey.Name)
                );
            }
        }

        /// <summary>
        /// Creates a property expression for a given property name.
        /// </summary>
        private static Expression<Func<T, object>> CreatePropertyExpression<T>(string propertyName)
        {
            var param = Expression.Parameter(typeof(T), "x");
            var property = Expression.Property(param, propertyName);
            var converted = Expression.Convert(property, typeof(object));
            return Expression.Lambda<Func<T, object>>(converted, param);
        }

        /// <summary>
        /// Retrieves the PropertyInfo from a property expression.
        /// </summary>
        private static PropertyInfo? GetPropertyInfo<T>(Expression<Func<T, object>> expression)
        {
            if (expression.Body is MemberExpression memberExpr)
            {
                return memberExpr.Member as PropertyInfo;
            }
            if (expression.Body is UnaryExpression unaryExpr && unaryExpr.Operand is MemberExpression operandExpr)
            {
                return operandExpr.Member as PropertyInfo;
            }
            return null;
        }
    }
}
