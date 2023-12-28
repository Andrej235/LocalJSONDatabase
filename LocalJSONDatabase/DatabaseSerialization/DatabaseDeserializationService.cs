/*using Microsoft.EntityFrameworkCore;
using ProjectGym.Services.DatabaseSerialization.Exceptions;
using ProjectGym.Utilities;
using System.Dynamic;
using System.Reflection;
using System.Text.Json;

namespace ProjectGym.Services.DatabaseSerialization
{
    public static class DatabaseDeserializationService
    {
        struct OldNewIdPairs(object oldId, object newId)
        {
            public object oldId = oldId;
            public object newId = newId;
        }

        /// <summary>
        /// Loads the given data into the given database (context)
        /// </summary>
        /// <typeparam name="TContext">Type of a database context in which the data will be injected</typeparam>
        /// <param name="context">Instance of TContext which will be used to inject data into the database</param>
        /// <param name="jsonEncodedDataBase">Data in a json format, will be translated into objects and injected into the database</param>
        /// <param name="modelNameToTableName">Calls this function every time it reaches a property marked as [ModelReference()] in order to find the name of that models table</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static async Task LoadDatabase<TContext>(TContext context, string jsonEncodedDataBase, Func<string, string> modelNameToTableName) where TContext : DbContext
        {
            IEnumerable<PropertyInfo> tablesAsProperties = GetTablesAsProperties(context);
            Dictionary<string, object> jsonBackupKeyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonEncodedDataBase) ?? throw new ArgumentNullException(nameof(jsonEncodedDataBase));
            Dictionary<string, IEnumerable<OldNewIdPairs>> idPairs = [];
            await LoadTablesFromProperties(context, tablesAsProperties, jsonBackupKeyValuePairs, idPairs, modelNameToTableName, 0);
        }

        /// <summary>
        /// Loads the given data into the given database (context)
        /// </summary>
        /// <typeparam name="TContext">Type of a database context in which the data will be injected</typeparam>
        /// <param name="context">Instance of TContext which will be used to inject data into the database</param>
        /// <param name="jsonEncodedDataBase">Data in a json format, will be translated into objects and injected into the database</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static async Task LoadDatabase<TContext>(TContext context, string jsonEncodedDataBase) where TContext : DbContext => await LoadDatabase(context, jsonEncodedDataBase, x => x + "s");

        private static async Task LoadTablesFromProperties<TContext>(TContext context, IEnumerable<PropertyInfo> tablesAsProperties, Dictionary<string, object> jsonDataToLoad, Dictionary<string, IEnumerable<OldNewIdPairs>> idPairs, Func<string, string> modelNameToTableName, int currentAttempt = 0) where TContext : DbContext
        {
            List<PropertyInfo> tableBuffer = [];
            foreach (PropertyInfo tableProperty in tablesAsProperties)
            {
                try
                {
                    Type entityType = tableProperty.PropertyType.GetGenericArguments()[0];
                    if (jsonDataToLoad[tableProperty.Name] is not JsonElement tableAsJsonElement)
                        continue;

                    var currentTableIdPairs = await LoadTable(context, ProcessJSON(tableAsJsonElement), entityType, idPairs, modelNameToTableName);
                    idPairs.Add(tableProperty.Name, currentTableIdPairs);
                }
                catch (UnresolvedDependencyException ex)
                {
                    if (currentAttempt > 32)
                        throw new UnresolvedDependencyException($"Error while injecting json data into database: Missing a dependency, tried resolving {currentAttempt} times", ex);

                    tableBuffer.Add(tableProperty);
                }
            }
            if (tableBuffer.Count != 0)
                await LoadTablesFromProperties(context, tableBuffer, jsonDataToLoad, idPairs, modelNameToTableName, currentAttempt + 1);
        }

        private static async Task<IEnumerable<OldNewIdPairs>> LoadTable<TContext>(TContext context, dynamic table, Type entityType, IDictionary<string, IEnumerable<OldNewIdPairs>> idPairs, Func<string, string> modelNameToTableName) where TContext : DbContext
        {
            List<OldNewIdPairs> tableIdPairs = [];
            foreach (var entity in table)
            {
                object newEntityInstance = Activator.CreateInstance(entityType) ?? throw new NullReferenceException();
                object? oldId = null;
                foreach (var property in (IDictionary<string, object>)entity)
                {
                    string propertyName = property.Key;
                    object propertyValue = property.Value;
                    PropertyInfo? entityPropertyInfo = entityType.GetProperty(propertyName);
                    if (entityPropertyInfo == null)
                        continue;

                    if (Convert.ToString(propertyValue) == "Not implemented")
                        continue;

                    if (propertyName == "Id")
                    {
                        oldId = propertyValue;
                        continue;
                    }

                    var modelReferenceAttribute = Attribute.GetCustomAttribute(entityPropertyInfo, typeof(ModelReferenceAttribute));
                    if (modelReferenceAttribute != null)
                    {
                        var modelNameProperty = modelReferenceAttribute.GetType().GetProperty("PositionalString") ?? throw new NullReferenceException();

                        string? referencedModelName = Convert.ToString(modelNameProperty.GetValue(modelReferenceAttribute)) ?? "";
                        if (referencedModelName == null || (!idPairs.TryGetValue(modelNameToTableName(referencedModelName), out IEnumerable<OldNewIdPairs>? idPairsOfSpecificTable) && !idPairs.TryGetValue(referencedModelName, out idPairsOfSpecificTable)))
                            throw new UnresolvedDependencyException(referencedModelName ?? "");

                        OldNewIdPairs specificIdPair = idPairsOfSpecificTable.First(x => Convert.ToString(x.oldId) == Convert.ToString(propertyValue));
                        entityPropertyInfo.SetValue(newEntityInstance, specificIdPair.newId);
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

                if (oldId == null)
                    throw new MissingIdColumnException();

                await context.AddAsync(newEntityInstance);
                await context.SaveChangesAsync();

                object newId = newEntityInstance.GetType().GetProperty("Id")?.GetValue(newEntityInstance) ?? throw new MissingIdColumnException();
                tableIdPairs.Add(new OldNewIdPairs(oldId, newId));
            }
            return tableIdPairs;
        }

        private static IEnumerable<PropertyInfo> GetTablesAsProperties<TContext>(TContext context) where TContext : DbContext => context
            .GetType()
            .GetProperties()
            .Where(x => x.PropertyType.IsGenericType && x.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
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
}*/