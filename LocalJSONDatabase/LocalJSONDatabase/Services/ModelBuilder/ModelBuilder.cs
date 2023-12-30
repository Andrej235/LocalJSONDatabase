namespace LocalJSONDatabase.Services.ModelBuilder
{
    public class ModelBuilder
    {
        private readonly List<Relationship> relationships = [];
        public Model<TEntity> Model<TEntity>()
        {
            Relationship newRelationship = new(typeof(TEntity), null, null, null);
            relationships.Add(newRelationship);
            return new(newRelationship);
        }

        public IEnumerable<Relationship> GetRelationships(Type type)
        {
            return relationships.Where(x => x.Type1 == type);
        }
    }
}
