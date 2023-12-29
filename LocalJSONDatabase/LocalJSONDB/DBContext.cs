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

        public async Task Initialize()
        {
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
    }

/*    public class ModelBuilder
    {
        private List<DoubleSidedRelationship<object, object>> expressions;
        public Model<TEntity> Model<TEntity>()
        {
            return new(expressions);
        }
    }

    public class Model<TEntity>
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
}
