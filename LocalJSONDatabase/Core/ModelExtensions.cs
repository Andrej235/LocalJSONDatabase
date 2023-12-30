using LocalJSONDatabase.Services.ModelBuilder;
using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

namespace LocalJSONDatabase.Core
{
    public static class ModelExtensions
    {
        public static Model<TEntity2> HasOne<TEntity1, TEntity2>(this Model<TEntity1> model, Expression<Func<TEntity1, TEntity2>> expression)
        {
            Relationship relationship = model.Relationship;
            relationship.Property1 = GetPropertyInfo(expression);

            var type2 = typeof(TEntity2);
            relationship.Type2 = !type2.IsGenericType ? type2 : type2.GetGenericTypeDefinition() == typeof(IEnumerable) ? type2.GetGenericArguments()[0] : throw new NotImplementedException();

            return new Model<TEntity2>(relationship);
        }

        public static Relationship WithMany<TEntity1, TEntity2>(this Model<TEntity2> model, Expression<Func<TEntity2, IEnumerable<TEntity1>>> expression)
        {
            Relationship relationship = model.Relationship;
            relationship.Property2 = GetPropertyInfo(expression);

            return relationship;
        }

        public static Relationship WithOne<TEntity1, TEntity2>(this Model<TEntity2> model, Expression<Func<TEntity1, TEntity2>> expression)
        {
            Relationship relationship = model.Relationship;
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
}
