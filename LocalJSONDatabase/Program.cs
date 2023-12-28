using LocalJSONDatabase.Attributes;

namespace LocalJSONDatabase
{
    internal class Program
    {
        static void Main()
        {
            var a = new UserDBContext();
            a.Initialize();

            a.Users.Add(new()
            {
                Name = "Test",
                Password = "password"
            });

            Console.WriteLine(a.Users.ContainsKey(2));

            a.Users.Add(new()
            {
                Id = 123,
                Name = "Test 2 - given explicit id => 123",
                Password = "password"
            });

            Console.WriteLine(a.Users.ContainsKey(2));
        }
    }

    public class User
    {
        [PrimaryKey]
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Password { get; set; }
    }

    public class UserDBContext : DBContext
    {
        public DBTable<User> Users { get; set; } = null!;

        protected override string DBDirectoryPath => $"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}";
    }
}
