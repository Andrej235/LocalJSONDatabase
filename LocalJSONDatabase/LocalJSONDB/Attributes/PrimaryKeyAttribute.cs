namespace LocalJSONDatabase.Attributes
{
    [System.AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
    sealed class PrimaryKeyAttribute : Attribute { }
}
