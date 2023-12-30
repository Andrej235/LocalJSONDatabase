using LocalJSONDatabase.Attributes;
using LocalJSONDatabase.Core;
using LocalJSONDatabase.Services.ModelBuilder;

namespace LocalJSONDatabase
{
    internal class Program
    {
        static void Main()
        {
            var context = new UserDBContext(new());
            Initialize(context);

            /*     context.Users.Add(new()
                 {
                     Name = "Andrej",
                     Password = "password123"
                 });

                 context.Users.Add(new()
                 {
                     Id = 123,
                     Name = "Different user",
                     Password = "password"
                 });

                 //Posts
                 Post post1 = new()
                 {
                     Caption = "First post!",
                     Creator = context.Users.FirstOrDefault(x => x.Id == 1) ?? throw new NullReferenceException()
                 };
                 context.Posts.Add(post1);

                 Post post2 = new()
                 {
                     Caption = "Second post",
                     Creator = context.Users.FirstOrDefault(x => x.Id == 1) ?? throw new NullReferenceException()
                 };
                 context.Add(post2, true);*/

            /*            foreach (var post in context.Posts)
                        {
                            Console.WriteLine($"Caption: {post.Caption}");
                        }*/

            //context.UpdateRelationships(context.Users.First(x => x.Id == 1));
        }

        private static async void Initialize(UserDBContext context)
        {
            await context.Initialize();
        }
    }

    public class User
    {
        [PrimaryKey]
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Password { get; set; }

        [ForeignKey(Multiple = true)]
        public IEnumerable<Post> Posts { get; set; } = [];
    }

    public class Post
    {
        [PrimaryKey]
        public int Id { get; set; }
        public required string Caption { get; set; }

        [ForeignKey]
        public required User Creator { get; set; }
    }

    public class Set
    {
        [PrimaryKey]
        public int Id { get; set; }
        public required string Exercise { get; set; }

        [ForeignKey]
        public Superset? Superset { get; set; }
    }

    public class Superset
    {
        [PrimaryKey]
        public int Id { get; set; }
        public required string Exercise { get; set; }

        [ForeignKey]
        public required Set Set { get; set; }
    }

    public class UserDBContext(ModelBuilder modelBuilder) : DBContext(modelBuilder)
    {
        public DBTable<User> Users { get; set; } = null!;
        public DBTable<Post> Posts { get; set; } = null!;

        //public DBTable<Set> Sets { get; set; } = null!;
        //public DBTable<Superset> Supersets { get; set; } = null!;

        protected override string DBDirectoryPath => $"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}";

        protected override void OnConfiguring(ModelBuilder modelBuilder)
        {
            modelBuilder.Model<Post>()
                .HasOne(x => x.Creator)
                .WithMany(x => x.Posts);
        }
    }
}
