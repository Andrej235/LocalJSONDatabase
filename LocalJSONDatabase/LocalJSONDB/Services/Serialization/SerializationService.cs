using LocalJSONDatabase;

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
                    if (property.PropertyType == typeof(string))
                        value = $"\"{property.GetValue(entity)}\"";
                    else if (property.PropertyType == typeof(int) || property.PropertyType == typeof(float) || property.PropertyType == typeof(bool))
                        value = $"{property.GetValue(entity)}".ToLower();
                    else if (property.PropertyType == typeof(Guid) || property.PropertyType == typeof(DateOnly) || property.PropertyType == typeof(DateTime))
                        value = $"\"{property.GetValue(entity)}\"";
                    else if (property.PropertyType.IsArray && property.PropertyType.GetElementType() == typeof(byte))
                    {
                        if (property.GetValue(entity) is IEnumerable<byte> bytes)
                            value = $"\"{Convert.ToBase64String([.. bytes])}\"";
                    }
                    else
                        value = "\"Not implemented\"";

                    return $"\"{property.Name}\": {value}";
                }))
                + "\n}";
            return resultingJSON;
        }
    }
}
