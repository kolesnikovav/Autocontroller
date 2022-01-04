using DbContextExample;

namespace AppExample
{
    static class Utility
    {
        static void InitializeDbForTests(AppDBContext db)
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

        static void ReinitializeDbForTests(AppDBContext db)
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
        new Post(){ Blog =  b, Id = 1,  Content = "This is a post 1"},
        new Post(){ Blog =  b, Id = 2,  Content = "This is a post 2"},
        new Post(){ Blog =  b, Id = 3,  Content = "This is a post 3"}
    };
        }


    }

}
