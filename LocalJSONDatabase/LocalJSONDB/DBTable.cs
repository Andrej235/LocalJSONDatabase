using LocalJSONDatabase.Attributes;
using LocalJSONDatabase.Exceptions;
using LocalJSONDatabase.Services.Files;
using LocalJSONDatabase.Services.Serialization;
using System.Collections;
using System.Reflection;

namespace LocalJSONDatabase
{
    public class DBTable<TEntity> : IEnumerable<TEntity> where TEntity : class
    {
        private readonly DBContext dBContext;

        public required List<TEntity> Entities { private get; init; }
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

        public void Add(TEntity entity, bool serialize = true)
        {
            if (serialize)
            {
                PropertyInfo primaryKeyProperty = entity.GetType().GetProperties().FirstOrDefault(x => x.GetCustomAttribute(typeof(PrimaryKeyAttribute)) != null) ?? throw new MissingPrimaryKeyPropertyException();
                if (primaryKeyProperty.PropertyType == typeof(int))
                    primaryKeyProperty.SetValue(entity, Entities.Count != 0 ? Convert.ToInt32(primaryKeyProperty.GetValue(Entities[^1])) + 1 : 1);
                else if (primaryKeyProperty.PropertyType == typeof(Guid))
                    primaryKeyProperty.SetValue(entity, Guid.NewGuid());
                else
                    throw new NotSupportedException("Unsupported type has been used as primary key.");

                var entityJSON = SerializationService.Serialize(entity);
                writingService.Write(entityJSON);
            }

            Entities.Add(entity);
            dBContext.UpdateRelationships(entity);

            /*            var foreignKeyProperties = typeof(TEntity).GetProperties().Where(x => x.GetCustomAttribute(typeof(ForeignKeyAttribute)) is not null);
                        foreach (var foreignKey in foreignKeyProperties)
                        {
                            var attribute = foreignKey.GetCustomAttribute(typeof(ForeignKeyAttribute)) ?? throw new NullReferenceException();

                            if (Convert.ToBoolean(attribute.GetType()?.GetProperty("Multiple")?.GetValue(attribute)))
                            {
                                //The reference foreignKey is an IEnumerable<T>
                            }
                            else
                            {
                                //The reference foreignKey is a single T
                            }

                            //Maybe do this the same way entity does? Define all relationships in an override method in dbContext?
                        }

                        //Create the opposite reference, if post points to user, user should also point to post
                        Entities.Add(entity);*/
        }

        public bool Contains(TEntity entity) => Entities.Contains(entity);
        public bool ContainsKey(object key)
        {
            PropertyInfo primaryKeyProperty = typeof(TEntity).GetProperties().FirstOrDefault(x => x.GetCustomAttribute(typeof(PrimaryKeyAttribute)) != null) ?? throw new MissingPrimaryKeyPropertyException();
            return Entities.Any(x => Convert.ToString(primaryKeyProperty.GetValue(x)) == Convert.ToString(key));
        }

        public string GetJSONForm()
        {
            return readingService.Read();
        }
    }
}
