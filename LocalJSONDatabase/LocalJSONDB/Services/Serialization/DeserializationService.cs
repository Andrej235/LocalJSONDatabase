using LocalJSONDatabase.Attributes;
using LocalJSONDatabase.Exceptions;
using System.Dynamic;
using System.Reflection;
using System.Text.Json;

namespace LocalJSONDatabase.Services.Serialization
{
    public static class DeserializationService
    {
        /// <summary>
        /// Loads the given data into the given database (context)
        /// </summary>
        /// <typeparam name="TContext">Type of a database context in which the data will be injected</typeparam>
        /// <param name="context">Instance of TContext which will be used to inject data into the database</param>
        /// <param name="jsonEncodedDataBase">Data in a json format, will be translated into objects and injected into the database</param>
        /// <param name="modelNameToTableName">Calls this function every time it reaches a property marked as [ModelReference()] in order to find the name of that models table</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static async Task LoadDatabase<TContext>(TContext context, Func<string, string> modelNameToTableName) where TContext : DBContext
        {
            IEnumerable<PropertyInfo> tablesAsProperties = GetTablesAsProperties(context);
            //Dictionary<string, object> jsonBackupKeyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonEncodedDataBase) ?? throw new ArgumentNullException(nameof(jsonEncodedDataBase));

            Dictionary<string, object> jsonBackupKeyValuePairs = [];
            foreach (var tableProp in tablesAsProperties)
            {
                var table = tableProp.GetValue(context);
                var getJSONMethod = table?.GetType().GetMethod("GetJSONForm");
                var json = getJSONMethod?.Invoke(table, null) ?? throw new NullReferenceException();

                if (Convert.ToString(json) == "")
                    return;

                jsonBackupKeyValuePairs.Add(modelNameToTableName(tableProp.PropertyType.GetGenericArguments()[0].Name), JsonSerializer.Deserialize<object>(Convert.ToString(json) ?? "") ?? throw new NullReferenceException());
            }

            await LoadTablesFromProperties(context, tablesAsProperties, jsonBackupKeyValuePairs, modelNameToTableName, 0);
        }

        /// <summary>
        /// Loads the given data into the given database (context)
        /// </summary>
        /// <typeparam name="TContext">Type of a database context in which the data will be injected</typeparam>
        /// <param name="context">Instance of TContext which will be used to inject data into the database</param>
        /// <param name="jsonEncodedDataBase">Data in a json format, will be translated into objects and injected into the database</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static async Task LoadDatabase<TContext>(TContext context) where TContext : DBContext => await LoadDatabase(context, x => x + "s");

        private static async Task LoadTablesFromProperties<TContext>(TContext context, IEnumerable<PropertyInfo> tablesAsProperties, Dictionary<string, object> jsonDataToLoad, Func<string, string> modelNameToTableName, int currentAttempt = 0) where TContext : DBContext
        {
            List<PropertyInfo> tableBuffer = [];
            foreach (PropertyInfo tableProperty in tablesAsProperties)
            {
                try
                {
                    Type entityType = tableProperty.PropertyType.GetGenericArguments()[0];
                    if (jsonDataToLoad[tableProperty.Name] is not JsonElement tableAsJsonElement)
                        continue;

                    var a = ProcessJSON(tableAsJsonElement);
                    LoadTable(context, a, entityType);
                }
                catch (UnresolvedDependencyException ex)
                {
                    if (currentAttempt > 32)
                        throw new UnresolvedDependencyException($"Error while injecting json data into database: Missing a dependency, tried resolving {currentAttempt} times", ex);

                    tableBuffer.Add(tableProperty);
                }
            }
            if (tableBuffer.Count != 0)
                await LoadTablesFromProperties(context, tableBuffer, jsonDataToLoad, modelNameToTableName, currentAttempt + 1);
        }

        private static void LoadTable<TContext>(TContext context, dynamic table, Type entityType) where TContext : DBContext
        {
            foreach (var entity in table)
            {
                object newEntityInstance = Activator.CreateInstance(entityType) ?? throw new NullReferenceException();
                foreach (var property in (IDictionary<string, object>)entity)
                {
                    string propertyName = property.Key;
                    object propertyValue = property.Value;
                    PropertyInfo? entityPropertyInfo = entityType.GetProperty(propertyName);
                    if (entityPropertyInfo == null)
                        continue;

                    if (Convert.ToString(propertyValue) == "Not implemented")
                        continue;

                    var modelReferenceAttribute = Attribute.GetCustomAttribute(entityPropertyInfo, typeof(ForeignKeyAttribute));
                    if (modelReferenceAttribute != null)
                    {
                        if (entityPropertyInfo.PropertyType.IsGenericType)
                            continue; //Many to one relationship --- handled from the other side (one to many)

                        //var modelNameProperty = modelReferenceAttribute.GetType().GetProperty("PositionalString") ?? throw new NullReferenceException();

                        //string? referencedModelName = Convert.ToString(modelNameProperty.GetValue(modelReferenceAttribute)) ?? "";
                        var referencedModelName = entityPropertyInfo.PropertyType.Name;

                        var referencedTable = context.Table(entityPropertyInfo.PropertyType);
                        if (referencedTable is not IEnumerable<object> values)
                            continue;

                        var primaryKeyPropInReferencedTableEntity = entityPropertyInfo.PropertyType.GetProperties().FirstOrDefault(x => x.GetCustomAttribute(typeof(PrimaryKeyAttribute)) is not null) ?? throw new MissingPrimaryKeyPropertyException();
                        var referencedEntity = values.FirstOrDefault(x => Convert.ToString(primaryKeyPropInReferencedTableEntity.GetValue(x)) == Convert.ToString(Convert.ToString(propertyValue)));

                        entityPropertyInfo.SetValue(newEntityInstance, /*specificIdPair.newId*/ referencedEntity);
                        continue;
                    }

                    if (entityPropertyInfo.PropertyType.IsArray && entityPropertyInfo.PropertyType.GetElementType() == typeof(byte))
                    {
                        entityPropertyInfo.SetValue(newEntityInstance, Convert.FromBase64String(Convert.ToString(propertyValue) ?? throw new Exception($"Value of property: {property.Key} is entered as an empty string. (byte[])")));
                        continue;
                    }

                    if (entityPropertyInfo.PropertyType == typeof(DateTime))
                    {
                        if (DateTime.TryParse(Convert.ToString(propertyValue), out DateTime date))
                            entityPropertyInfo.SetValue(newEntityInstance, date);
                        continue;
                    }

                    if (entityPropertyInfo.PropertyType == typeof(DateTime))
                    {
                        if (DateOnly.TryParse(Convert.ToString(propertyValue), out DateOnly date))
                            entityPropertyInfo.SetValue(newEntityInstance, date);
                        continue;
                    }

                    if (IsNumericType(entityPropertyInfo.PropertyType))
                    {
                        entityPropertyInfo.SetValue(newEntityInstance, Convert.ChangeType(propertyValue, entityPropertyInfo.PropertyType));
                        continue;
                    }

                    entityPropertyInfo.SetValue(newEntityInstance, propertyValue);
                }

                context.Add(newEntityInstance, false);
            }
        }

        private static IEnumerable<PropertyInfo> GetTablesAsProperties<TContext>(TContext context) where TContext : DBContext => context
            .GetType()
            .GetProperties()
            .Where(x => x.PropertyType.IsGenericType && x.PropertyType.GetGenericTypeDefinition() == typeof(DBTable<>))
            .OrderBy(x => x.PropertyType.GetGenericArguments()[0].GetProperties().Select(x => x.Name).Where(x => x.Contains("Id")).Count());

        private static object ProcessJSON(JsonElement jsonElement)
        {
            try
            {
                switch (jsonElement.ValueKind)
                {
                    case JsonValueKind.Undefined:
                        throw new NullReferenceException();
                    case JsonValueKind.Object:
                        dynamic entity = new ExpandoObject();
                        foreach (JsonProperty property in jsonElement.EnumerateObject())
                            ((IDictionary<string, object>)entity).Add(property.Name, ProcessJSON(property.Value));

                        return entity;
                    case JsonValueKind.Array:
                        List<dynamic> expandoList = [];
                        expandoList.AddRange(jsonElement.EnumerateArray().Select(ProcessJSON));

                        return expandoList;
                    case JsonValueKind.String:
                        return jsonElement.GetString() ?? "";
                    case JsonValueKind.Number:
                        return jsonElement.TryGetDouble(out double doubleValue) ? doubleValue : 0d;
                    case JsonValueKind.True:
                        return true;
                    case JsonValueKind.False:
                        return false;
                    case JsonValueKind.Null:
                        throw new NullReferenceException();
                    default:
                        throw new NullReferenceException();
                }

            }
            catch (Exception ex)
            {
                LogDebugger.LogError(ex);
                throw;
            }
        }

        private static bool IsNumericType(Type type)
        {
            if (type.IsPrimitive)
            {
                // Check for specific numeric types
                return type == typeof(int) ||
                       type == typeof(float) ||
                       type == typeof(double) ||
                       type == typeof(decimal);
            }

            return false;
        }
    }
}