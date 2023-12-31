using System.Reflection;

namespace LocalJSONDatabase.Services.ModelBuilder
{
    public class OneSidedRelationship<TEntity1, TEntity2>(Relationship unfinishedRelationship)
    {
        private readonly Relationship unfinishedRelationship = unfinishedRelationship;
        public PropertyInfo? Property1 { get; set; } = unfinishedRelationship.Property1;
        public Relationship FinishRelationship(PropertyInfo property2)
        {
            unfinishedRelationship.Property2 = property2;
            return unfinishedRelationship;
        }
    }
}
