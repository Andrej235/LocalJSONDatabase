using LocalJSONDatabase;
using LocalJSONDatabase.Attributes;
using LocalJSONDatabase.Exceptions;
using System.Reflection;

namespace LocalJSONDatabase.Services.Serialization
{
    public static class SerializationService
    {
        public static string Serialize<TEntity>(DBTable<TEntity> table) where TEntity : class
        {
            string entitiesJSON = string.Join(", \n", table.Select(Serialize));
            string resultingJSON = $"{{\n\"{typeof(TEntity).Name}\": [\n{entitiesJSON}\n]\n}}";
            return resultingJSON;
        }

        public static string Serialize(object entity)
        {
            var properties = entity.GetType().GetProperties().Where(x => !x.PropertyType.IsGenericType);
            var resultingJSON = "{\n"
                + string.Join(", \n", properties.Select(property =>
                {
                    string value = "";
                    var propertyValue = property.GetValue(entity);

                    if (property.PropertyType == typeof(string))
                        value = $"\"{propertyValue}\"";
                    else if (property.PropertyType == typeof(int) || property.PropertyType == typeof(float) || property.PropertyType == typeof(double) || property.PropertyType == typeof(decimal) || property.PropertyType == typeof(bool))
                        value = $"{propertyValue}".ToLower();
                    else if (property.PropertyType == typeof(Guid) || property.PropertyType == typeof(DateOnly) || property.PropertyType == typeof(DateTime))
                        value = $"\"{propertyValue}\"";
                    else if (property.PropertyType.IsArray && property.PropertyType.GetElementType() == typeof(byte))
                    {
                        if (propertyValue is IEnumerable<byte> bytes)
                            value = $"\"{Convert.ToBase64String([.. bytes])}\"";
                        //TODO: Add serialization support for other thing, also make sure to just include primary keys if its a list of models
                        //else if (propertyValue is IEnumerable<object> objects)
                            //value = $"[{string.Join(',', objects.Select(Serialize))}]";
                    }
                    else
                    {
                        if (property.GetCustomAttribute(typeof(ForeignKeyAttribute)) is not null)
                        {
                            PropertyInfo foreignEntityPrimaryKeyProp = propertyValue?.GetType().GetProperties().FirstOrDefault(x => x.GetCustomAttribute(typeof(PrimaryKeyAttribute)) is not null) ?? throw new MissingPrimaryKeyPropertyException();
                            value = $"{foreignEntityPrimaryKeyProp.GetValue(propertyValue)}";
                        }
                        else
                            value = "\"Not implemented\"";
                    }

                    return $"\"{property.Name}\": {value}";
                }))
                + "\n}";
            return resultingJSON;
        }
    }
}
