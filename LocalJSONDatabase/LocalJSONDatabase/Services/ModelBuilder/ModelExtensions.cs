using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

namespace LocalJSONDatabase.Services.ModelBuilder
{
    public static class ModelExtensions
    {
        public static OneSidedRelationship<TEntity1, TEntity2> HasOne<TEntity1, TEntity2>(this Model<TEntity1> model, Expression<Func<TEntity1, TEntity2>> expression)
        {
            Relationship relationship = model.Relationship;
            relationship.Property1 = GetPropertyInfo(expression);

            var type2 = typeof(TEntity2);
            relationship.Type2 = !type2.IsGenericType ? type2 : type2.GetGenericTypeDefinition() == typeof(IEnumerable) ? type2.GetGenericArguments()[0] : throw new NotImplementedException();

            return new OneSidedRelationship<TEntity1, TEntity2>(relationship);
        }

        public static Relationship WithMany<TEntity1, TEntity2>(this OneSidedRelationship<TEntity1, TEntity2> oneSidedRelationship, Expression<Func<TEntity2, IEnumerable<TEntity1>>> expression) => oneSidedRelationship.FinishRelationship(GetPropertyInfo(expression));

        public static Relationship WithOne<TEntity1, TEntity2>(this OneSidedRelationship<TEntity1, TEntity2> oneSidedRelationship, Expression<Func<TEntity2, TEntity1>> expression) => oneSidedRelationship.FinishRelationship(GetPropertyInfo(expression));

        private static PropertyInfo GetPropertyInfo<TEntity1, TEntity2>(Expression<Func<TEntity1, TEntity2>> expression)
        {
            return expression.Body is MemberExpression memberExpression && memberExpression.Member is PropertyInfo propertyInfo
                ? propertyInfo
                : throw new NotSupportedException();
        }
    }
}
