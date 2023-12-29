namespace LocalJSONDatabase.Exceptions
{
    [Serializable]
    public class UnresolvedDependencyException : Exception
    {
        public UnresolvedDependencyException() { }
        public UnresolvedDependencyException(string message) : base(message) { }
        public UnresolvedDependencyException(string message, Exception inner) : base(message, inner) { }
    }
}