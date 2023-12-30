namespace LocalJSONDatabase.Exceptions
{

    [Serializable]
	public class UninitializedContextException : Exception
	{
		public UninitializedContextException() { }
		public UninitializedContextException(string message) : base(message) { }
		public UninitializedContextException(string message, Exception inner) : base(message, inner) { }
	}
}
