using System.Reflection;

namespace LocalJSONDatabase.Services.ModelBuilder
{
    public class Relationship(Type Type1, Type? Type2, PropertyInfo? Property1, PropertyInfo? Property2)
    {
        public bool IsIncomplete => Type2 is null || Property1 is null || Property2 is null;

        public Type Type1 { get; } = Type1;
        public Type? Type2 { get; set; } = Type2;
        public PropertyInfo? Property1 { get; set; } = Property1;
        public PropertyInfo? Property2 { get; set; } = Property2;
    }
}
