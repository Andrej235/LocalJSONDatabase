using LocalJSONDatabase.Attributes;
using LocalJSONDatabase.Exceptions;
using LocalJSONDatabase.Services.ModelBuilder;
using LocalJSONDatabase.Services.Utility;
using LocalJSONDatabase.Services.Serialization;
using System.Collections;
using System.Reflection;

namespace LocalJSONDatabase.Core
{
    public abstract class DBContext
    {
        protected abstract string DBDirectoryPath { get; }
        protected abstract void OnConfiguring(ModelBuilder modelBuilder);

        private IEnumerable<PropertyInfo>? tablesProperties;
        readonly ModelBuilder modelBuilder;

        protected DBContext(ModelBuilder modelBuilder) => this.modelBuilder = modelBuilder;

        public async Task Initialize()
        {
            OnConfiguring(modelBuilder);

            tablesProperties = GetType().GetProperties().Where(x => x.PropertyType.IsGenericType && x.PropertyType.GetGenericTypeDefinition() == typeof(DBTable<>));
            foreach (PropertyInfo property in tablesProperties)
            {
                var dbTableType = property.PropertyType;
                var ctor = dbTableType.GetConstructor([typeof(string), typeof(DBContext)]) ?? throw new NullReferenceException("No viable constructor found");

                property.SetValue(this, ctor.Invoke([DBDirectoryPath, this]));
            }

            await DeserializationService.LoadDatabase(this);
        }

        public void Add(object entity, bool asignPrimaryKey = true)
        {
            var table = Table(entity.GetType());
            var addMethod = table.GetType().GetMethod("Add");
            addMethod?.Invoke(table, [entity, asignPrimaryKey]);
        }
        public void Add<TEntity>(TEntity entity, bool asignPrimaryKey = true) where TEntity : class => Table<TEntity>().Add(entity, asignPrimaryKey);

        //TODO?: Add another delete method which takes an object like 'Add(object entity, bool asignPrimaryKey = true)'
        public void Delete<TEntity>(TEntity entity) where TEntity : class => Table<TEntity>().Delete(entity);

        //TODO?: Add another Find method which takes a type parameter like 'Table(Type entityType)'
        public TEntity Find<TEntity>(object primaryKey) where TEntity : class => Table<TEntity>().Find(primaryKey);


        public DBTable<T> Table<T>() where T : class
        {
            if (tablesProperties is null)
                throw new UninitializedContextException(GetType().FullName ?? "");

            foreach (PropertyInfo property in tablesProperties)
            {
                var tableEntityType = property.PropertyType.GetGenericArguments()[0];
                if (tableEntityType == typeof(T))
                {
                    return (DBTable<T>?)property.GetValue(this) ?? throw new NullReferenceException();
                }
            }
            throw new NullReferenceException();
        }
        public object Table(Type entityType) => GetType().GetMethod("Table", [])?.MakeGenericMethod(entityType).Invoke(this, []) ?? throw new NullReferenceException();

        public void UpdateRelationships(object entity)
        {
            IEnumerable<Relationship>? relationships = modelBuilder.GetRelationships(entity.GetType()) ?? throw new NullReferenceException();
            foreach (var relationship in relationships)
            {
                try
                {
                    if (relationship.Property1 is null)
                        throw new NullReferenceException(nameof(relationship.Property1));

                    if (relationship.Property2 is null)
                        throw new NullReferenceException(nameof(relationship.Property2));

                    //Throws NullReferenceException if relationship.Property1.GetValue(entity) == null, also check whether the relationship is One to X or Many to X
                    if ((relationship.Property1.GetValue(entity) ?? throw new NullReferenceException()) is not IEnumerable<object> referencedEntities)
                    {
                        //One to many or one to one
                        object? referencedEntityValue = relationship.Property2.GetValue(relationship.Property1.GetValue(entity));
                        Type referencedEntityType = relationship.Property2.PropertyType;

                        //If referencedEntityValue is null, check if it implements IEnumerable.
                        //If so create a new instance so the rest of the code doesn't break
                        //If not leave it as null
                        if (referencedEntityValue is null && referencedEntityType.GetInterface(nameof(IEnumerable)) != null)
                            referencedEntityValue = Activator.CreateInstance(referencedEntityType);

                        if (referencedEntityValue is IEnumerable<object> values)
                        {
                            //'referencedEntityValue' is a Collection named values (contains references to objects of same type as 'entity')
                            //One to many
                            var valueType = values.FirstOrDefault()?.GetType();
                            List<object> newValues;
                            if (valueType is null)
                            {
                                //No elements / values collection is empty
                                //Asign a new collection to it, with the only element inside it being 'entity'
                                newValues = [entity];
                            }
                            else
                            {
                                var valuePrimaryKeyProp = valueType.GetProperties().FirstOrDefault(x => x.GetCustomAttribute(typeof(PrimaryKeyAttribute)) != null) ?? throw new MissingPrimaryKeyPropertyException();
                                var entityPrimaryKey = valuePrimaryKeyProp.GetValue(entity);
                                bool entityAlreadyInValues = false;
                                foreach (var value in values)
                                {
                                    //Go through each element already referenced and compare each primary key to the one of 'entity'
                                    //If an entity with pk of 'entity' doesn't exist in values collection add it and set the value
                                    var valuePrimaryKey = valuePrimaryKeyProp.GetValue(value);
                                    if (Convert.ToString(valuePrimaryKey) == Convert.ToString(entityPrimaryKey))
                                    {
                                        entityAlreadyInValues = true;
                                        break;
                                    }
                                }

                                if (entityAlreadyInValues)
                                    continue;

                                newValues = [.. values, entity];

                            }

                            var castMethod = typeof(Enumerable)
                                          .GetMethod("Cast")?
                                          .MakeGenericMethod(entity.GetType());

                            relationship.Property2.SetValue(relationship.Property1?.GetValue(entity) ?? throw new NullReferenceException(), castMethod?.Invoke(null, new object[] { newValues }));
                        }
                        else
                        {
                            //One to one
                            //Set the previous relationship to null??? Maybe just hope the user doesn't screw up...
                            relationship.Property2.SetValue(relationship.Property1?.GetValue(entity) ?? throw new NullReferenceException(), entity);
                        }
                    }
                    /*                    else
                                        {
                                            //Many to one or many to many

                                            foreach (var referencedEntity in referencedEntities)
                                            {
                                                object referencedEntityRelationshipValue = relationship.Property2?.GetValue(referencedEntity) ?? throw new NullReferenceException();
                                                var referencedEntityRelationshipValueType = referencedEntityRelationshipValue.GetType();
                                                if (referencedEntityRelationshipValue is IEnumerable<object> values)
                                                {
                                                    //Many to many
                                                }
                                                else
                                                {
                                                    //Many to one
                                                }
                                            }
                                        }*/
                }
                catch (Exception ex)
                {
                    LogDebugger.LogError(ex);
                }
            }
        }

        public void SaveChanges()
        {
            if (tablesProperties is null)
                throw new UninitializedContextException(GetType().FullName ?? "");

            foreach (var property in tablesProperties)
            {
                var saveChangesMethod = property.PropertyType.GetMethod("SaveChanges", []);
                var table = property.GetValue(this);

                saveChangesMethod?.Invoke(table, []);
            }
        }
    }
}
