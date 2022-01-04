using DbContextExample;

namespace AppExample
{
 public  static class Utility
    {
        public static void InitializeDbForTests(AppDBContext db)
        {
            var blogs = GetBlogs();
            foreach (var q in blogs)
            {
                var posts = GetPosts(q);
                db.Posts.AddRange(posts);
            }
            db.Blogs.AddRange(blogs);
            db.SaveChanges();
        }

        public static void ReinitializeDbForTests(AppDBContext db)
        {
            db.Blogs.RemoveRange(db.Blogs);
            db.Posts.RemoveRange(db.Posts);
            InitializeDbForTests(db);
        }

        static List<Blog> GetBlogs()
        {
            return new List<Blog>()
           {
               new Blog(){ Id = 1, Description = "Test blog1"},
               new Blog(){ Id = 2, Description = "Test blog2" },
               new Blog(){ Id = 3, Description = "Test blog3"}
           };
        }

        static List<Post> GetPosts(Blog b)
        {
            return new List<Post>()
            {
                new Post(){ BlogId =  b.Id, Id = 1,  Content = "This is a post 1"},
                new Post(){ BlogId =  b.Id, Id = 2,  Content = "This is a post 2"},
                new Post(){ BlogId =  b.Id, Id = 3,  Content = "This is a post 3"}
            };
        }


    }

}
