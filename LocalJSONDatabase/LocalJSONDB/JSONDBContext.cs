using System.Reflection;

namespace LocalJSONDatabase
{
    public abstract class DBContext
    {
        private IEnumerable<PropertyInfo>? tablesProperties;

        protected abstract string DBDirectoryPath { get; }

        public void Initialize()
        {
            tablesProperties = GetType().GetProperties().Where(x => x.PropertyType.IsGenericType && x.PropertyType.GetGenericTypeDefinition() == typeof(DBTable<>));

            foreach (PropertyInfo property in tablesProperties)
            {
                var dbTableType = property.PropertyType;
                var ctor = dbTableType.GetConstructor([typeof(string)]) ?? throw new NullReferenceException("No viable constructor found");

                property.SetValue(this, ctor.Invoke([DBDirectoryPath]));
            }
        }
    }
}
