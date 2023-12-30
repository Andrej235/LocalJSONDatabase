﻿using LocalJSONDatabase.Attributes;
using System;
using System.Runtime.InteropServices;

namespace LocalJSONDatabase
{
    internal class Program
    {
        static void Main()
        {
            var context = new UserDBContext(new());
            Initialize(context);

            context.Users.Add(new()
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
            //context.UpdateRelationships(post1);
            /*            foreach (var post in context.Posts)
                        {
                            Console.WriteLine($"Caption: {post.Caption}");
                        }*/
        }

        private static async void Initialize(UserDBContext context)
        {
            await context.Initialize();

            //var a = context.Users;
            //var b = context.Posts;
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

    public class UserDBContext(ModelBuilder modelBuilder) : DBContext(modelBuilder)
    {
        public DBTable<User> Users { get; set; } = null!;
        public DBTable<Post> Posts { get; set; } = null!;

        protected override string DBDirectoryPath => $"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}";

        protected override void OnConfiguring(ModelBuilder modelBuilder)
        {
            modelBuilder.Model<Post>()
                .HasOne<Post, User>(x => x.Creator)
                .WithMany<User, Post>(x => x.Posts);
        }
    }
}
