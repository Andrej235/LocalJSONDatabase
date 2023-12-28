namespace ProjectGym.Services.DatabaseSerialization
{
    [System.AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
    sealed class ModelReferenceAttribute(string positionalString) : Attribute
    {
        readonly string positionalString = positionalString;
        public string PositionalString => positionalString;
        public bool IsNullable { get; set; }
    }
}
