namespace LocalJSONDatabase.Exceptions
{

    [Serializable]
	public class MissingPrimaryKeyPropertyException : Exception
	{
		public MissingPrimaryKeyPropertyException() { }
		public MissingPrimaryKeyPropertyException(string message) : base(message) { }
		public MissingPrimaryKeyPropertyException(string message, Exception inner) : base(message, inner) { }
	}
}
