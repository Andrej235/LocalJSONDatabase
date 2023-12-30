using LocalJSONDatabase.Attributes;
using LocalJSONDatabase.Exceptions;
using LocalJSONDatabase.Services.Files;
using LocalJSONDatabase.Services.Serialization;
using System.Collections;
using System.Reflection;

namespace LocalJSONDatabase.Core
{
    public class DBTable<TEntity> : IEnumerable<TEntity> where TEntity : class
    {
        public required List<TEntity> Entities { private get; init; }
        private readonly DBContext dBContext;
        private readonly FileWritingService writingService;
        private readonly FileReadingService readingService;

        public DBTable(string dbDirectoryLocation, DBContext dBContext)
        {
            this.dBContext = dBContext;

            if (string.IsNullOrWhiteSpace(dbDirectoryLocation))
                throw new ArgumentException($"'{nameof(dbDirectoryLocation)}' cannot be null or whitespace.", nameof(dbDirectoryLocation));

            if (!Directory.Exists(dbDirectoryLocation))
                throw new ArgumentException($"'{nameof(dbDirectoryLocation)}' has to point to a valid directory.", nameof(dbDirectoryLocation));

            var filepath = $"{dbDirectoryLocation}/{typeof(TEntity).FullName}.json";
            writingService = new(filepath);
            readingService = new(filepath);
            //Entities = (readingService.Read<TEntity>() ?? []).ToList();
            Entities = [];
        }

        public IEnumerator<TEntity> GetEnumerator() => Entities.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void Add(TEntity entity, bool asignPrimaryKey = true)
        {
            if (asignPrimaryKey)
            {
                PropertyInfo primaryKeyProperty = entity.GetType().GetProperties().FirstOrDefault(x => x.GetCustomAttribute(typeof(PrimaryKeyAttribute)) != null) ?? throw new MissingPrimaryKeyPropertyException();
                if (primaryKeyProperty.PropertyType == typeof(int))
                    primaryKeyProperty.SetValue(entity, Entities.Count != 0 ? Convert.ToInt32(primaryKeyProperty.GetValue(Entities[^1])) + 1 : 1);
                else if (primaryKeyProperty.PropertyType == typeof(Guid))
                    primaryKeyProperty.SetValue(entity, Guid.NewGuid());
                else
                    throw new NotSupportedException("Unsupported type has been used as primary key.");
            }

            Entities.Add(entity);
            dBContext.UpdateRelationships(entity);
        }

        public TEntity Find(object primaryKey)
        {
            var primaryKeyProp = typeof(TEntity).GetProperties().FirstOrDefault(x => x.GetCustomAttribute(typeof(PrimaryKeyAttribute)) != null) ?? throw new MissingPrimaryKeyPropertyException();
            return Entities.FirstOrDefault(x => Convert.ToString(primaryKeyProp.GetValue(x)) == Convert.ToString(primaryKey)) ?? throw new NullReferenceException();
        }

        public void Delete(TEntity entity) => Entities.Remove(entity);
        public void Delete(IEnumerable<TEntity> entities)
        {
            foreach (var entity in entities)
                Entities.Remove(entity);
        }

        public void SaveChanges()
        {
            var entityJSON = SerializationService.Serialize(this);
            writingService.Write(entityJSON, true);
        }

        public bool Contains(TEntity entity) => Entities.Contains(entity);
        public bool ContainsKey(object key)
        {
            PropertyInfo primaryKeyProperty = typeof(TEntity).GetProperties().FirstOrDefault(x => x.GetCustomAttribute(typeof(PrimaryKeyAttribute)) != null) ?? throw new MissingPrimaryKeyPropertyException();
            return Entities.Any(x => Convert.ToString(primaryKeyProperty.GetValue(x)) == Convert.ToString(key));
        }

        public string GetJSONForm() => readingService.Read();
    }
}
