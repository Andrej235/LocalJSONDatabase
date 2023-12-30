namespace LocalJSONDatabase.Services.ModelBuilder
{
    public class Model<TEntity>(Relationship relationship)
    {
        public Relationship Relationship { get; } = relationship;
    }
}
