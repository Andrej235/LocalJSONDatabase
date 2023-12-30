namespace LocalJSONDatabase.Attributes
{
    [System.AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
    sealed class ForeignKeyAttribute : Attribute
    {
        public bool Multiple { get; set; }
    }
}
