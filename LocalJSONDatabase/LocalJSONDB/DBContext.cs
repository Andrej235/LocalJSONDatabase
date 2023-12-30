using LocalJSONDatabase.Services;
using LocalJSONDatabase.Services.Serialization;
using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

namespace LocalJSONDatabase
{
    public abstract class DBContext
    {
        private IEnumerable<PropertyInfo>? tablesProperties;

        protected abstract string DBDirectoryPath { get; }

        protected abstract void OnConfiguring(ModelBuilder modelBuilder);

        readonly ModelBuilder modelBuilder;

        protected DBContext(ModelBuilder modelBuilder)
        {
            this.modelBuilder = modelBuilder;
        }

        public async Task Initialize()
        {
            OnConfiguring(modelBuilder);

            tablesProperties = GetType().GetProperties().Where(x => x.PropertyType.IsGenericType && x.PropertyType.GetGenericTypeDefinition() == typeof(DBTable<>));

            foreach (PropertyInfo property in tablesProperties)
            {
                var dbTableType = property.PropertyType;
                var ctor = dbTableType.GetConstructor([typeof(string), typeof(DBContext)]) ?? throw new NullReferenceException("No viable constructor found");

                property.SetValue(this, ctor.Invoke([DBDirectoryPath, this]));
            }

            await DeserializationService.LoadDatabase(this);
        }

        public void Add(object entity, bool serialize)
        {
            var entityType = entity.GetType();
            tablesProperties = GetType().GetProperties().Where(x => x.PropertyType.IsGenericType && x.PropertyType.GetGenericTypeDefinition() == typeof(DBTable<>));
            foreach (PropertyInfo property in tablesProperties)
            {
                var tableEntityType = property.PropertyType.GetGenericArguments()[0];
                if (tableEntityType == entityType)
                {
                    var table = property.GetValue(this) ?? throw new NullReferenceException();
                    var addMethod = table.GetType().GetMethod("Add");
                    addMethod?.Invoke(table, [entity, serialize]);
                }
            }
            UpdateRelationships(entity);
        }

        public DBTable<T> TableGeneric<T>() where T : class
        {
            tablesProperties = GetType().GetProperties().Where(x => x.PropertyType.IsGenericType && x.PropertyType.GetGenericTypeDefinition() == typeof(DBTable<>));
            foreach (PropertyInfo property in tablesProperties)
            {
                var tableEntityType = property.PropertyType.GetGenericArguments()[0];
                if (tableEntityType == typeof(T))
                {
                    return ((DBTable<T>?)property.GetValue(this)) ?? throw new NullReferenceException();
                }
            }
            throw new NullReferenceException();
        }

        public object Table(Type entityType)
        {
            return GetType().GetMethod("TableGeneric")?.MakeGenericMethod(entityType).Invoke(this, []) ?? throw new NullReferenceException();
        }

        public void UpdateRelationships(object entity)
        {
            //TODO: Update the relationships in the files as well
            try
            {
                IEnumerable<Relationship>? relationships = modelBuilder.GetRelationships(entity.GetType()) ?? throw new NullReferenceException();
                foreach (var relationship in relationships)
                {
                    object referencedEntity = relationship.Property1?.GetValue(entity) ?? throw new NullReferenceException();
                    var referencedEntityType = referencedEntity.GetType();
                    if (referencedEntityType.IsGenericType && referencedEntityType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                    {
                        //Many to one or many to many
                    }
                    else
                    {
                        //One to many or one to one
                        object referencedEntityRelationshipValue = relationship.Property2?.GetValue(referencedEntity) ?? throw new NullReferenceException();
                        var referencedEntityRelationshipValueType = referencedEntityRelationshipValue.GetType();
                        if (referencedEntityRelationshipValue is IEnumerable<object> values)
                        {
                            //One to many
                            var valueType = values.FirstOrDefault()?.GetType();
                            if (valueType is null)
                            {
                                //No elements / values is empty
                                var newValues = values.ToList();
                                newValues.Add(entity);

                                var castMethod = typeof(Enumerable)
                                                .GetMethod("Cast")?
                                                .MakeGenericMethod(entity.GetType());

                                relationship.Property2.SetValue(referencedEntity, castMethod?.Invoke(null, new object[] { newValues }));
                            }
                            else
                            {
                                foreach (var value in values)
                                {
                                    //Go through each element already referenced and compare primary key to entity
                                    //If an entity with entity's id doesn't exist in values collection add it and set the value
                                }
                            }
                        }
                        else
                        {
                            //One to one
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                LogDebugger.LogError(ex);
            }
        }
    }

    public class ModelBuilder
    {
        private readonly List<Relationship> relationships = [];
        public Relationship Model<TEntity>()
        {
            Relationship newRelationship = new(typeof(TEntity), null, null, null);
            relationships.Add(newRelationship);
            return newRelationship;
        }

        public IEnumerable<Relationship> GetRelationships(Type type)
        {
            return relationships.Where(x => x.Type1 == type);
        }
    }

    /*    public class Model<TEntity>
        {
            private List<DoubleSidedRelationship<object, object>> expressions;

            public Model(List<DoubleSidedRelationship<object, object>> expressions)
            {
                this.expressions = expressions;
            }

            public Relationship<TEntity, TRelationship> HasOne<TRelationship>(Expression<Func<TEntity, TRelationship>> expression)
            {
                return new(expression);
            }
        }

        public class Relationship<TEntity1, TEntity2>
        {
            private Expression<Func<TEntity1, TEntity2>> RelationshipExpression { get; init; }
            public Relationship(Expression<Func<TEntity1, TEntity2>> relationshipExpression)
            {
                RelationshipExpression = relationshipExpression;
            }

            public DoubleSidedRelationship<TEntity1, TEntity2> WithMany(Expression<Func<TEntity2, IEnumerable<TEntity1>>> expression)
            {
                return new(RelationshipExpression, expression);
            }
        }

        public class DoubleSidedRelationship<TEntity1, TEntity2>
        {
            public DoubleSidedRelationship(Expression<Func<TEntity1, TEntity2>> leftSideRelationshipExpression, Expression<Func<TEntity2, IEnumerable<TEntity1>>> rightSideRelationshipExpression)
            {
                LeftSideRelationshipExpression = leftSideRelationshipExpression;
                RightSideRelationshipExpression = rightSideRelationshipExpression;
            }

            private Expression<Func<TEntity1, TEntity2>> LeftSideRelationshipExpression { get; init; }
            private Expression<Func<TEntity2, TEntity1>> RightSideRelationshipExpression { get; init; }

        }*/

    public static class RelationshipExtensions
    {
        public static Relationship HasOne<TEntity1, TEntity2>(this Relationship relationship, Expression<Func<TEntity1, TEntity2>> expression)
        {
            relationship.Property1 = GetPropertyInfo(expression);

            var type2 = typeof(TEntity2);
            relationship.Type2 = !type2.IsGenericType ? type2 : (type2.GetGenericTypeDefinition() == typeof(IEnumerable) ? type2.GetGenericArguments()[0] : throw new NotImplementedException());

            return relationship;
        }

        public static Relationship WithMany<TEntity1, TEntity2>(this Relationship relationship, Expression<Func<TEntity1, IEnumerable<TEntity2>>> expression)
        {
            relationship.Property2 = GetPropertyInfo(expression);

            return relationship;
        }

        private static PropertyInfo GetPropertyInfo<TEntity1, TEntity2>(Expression<Func<TEntity1, TEntity2>> expression)
        {
            if (expression.Body is MemberExpression memberExpression)
            {
                if (memberExpression.Member is PropertyInfo propertyInfo)
                {
                    return propertyInfo;
                }
            }

            throw new NotSupportedException();
        }
    }

    public class Relationship(Type Type1, Type? Type2, PropertyInfo? Property1, PropertyInfo? Property2)
    {
        public bool IsIncomplete => Type2 is null || Property1 is null || Property2 is null;

        public Type Type1 { get; } = Type1;
        public Type? Type2 { get; set; } = Type2;
        public PropertyInfo? Property1 { get; set; } = Property1;
        public PropertyInfo? Property2 { get; set; } = Property2;
    }
}
