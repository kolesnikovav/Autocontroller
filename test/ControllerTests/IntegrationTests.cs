using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using AppExample;
using DbContextExample;
using System.Diagnostics;
using Xunit;

namespace AutocontrollerTests;

public class IntegrationTests : IClassFixture<WebApplicationFactory<Startup>>
{
    private readonly WebApplicationFactory<AppExample.Startup> _factory;
    public IntegrationTests(WebApplicationFactory<AppExample.Startup> factory)
    {
        _factory = factory;
        // database seeding
        var r = _factory.Services.GetService(typeof(DbContextExample.AppDBContext));
        System.Console.Write(r);
        //     using (var scope = _factory.Services.GetService(DbContextExample.AppDBContext))
        //     {
        //         var services = scope.ServiceProvider;
        //         try
        //         {
        //             var context = services.GetRequiredService<AppDBContext>();
        //             Utility.InitializeDbForTests(context);
        //         }
        //         catch (Exception ex)
        //         {
        //             var logger = services.GetRequiredService<ILogger<Program>>();
        //             logger.LogError(ex, "An error occurred while seeding the database.");
        //         }
        //     }        
        // _factory.Services.GetService = factory;
    }

    [Fact]
    public async Task TestIndexPage()
    {
            // Arrange
            var client = _factory.CreateClient();
            // Arrange
            var defaultPage = await client.GetAsync("/api/Blogs/Index");
            var defaultPage2 = await client.GetAsync("/api2/Blogs/Index");
            var defaultPage3 = await client.GetAsync("/api3/Blogs/Index");

            var defaultPagePost = await client.GetAsync("/api/Posts/Index");
            var defaultPagePost2 = await client.GetAsync("/api2/Posts/Index");
            var defaultPagePost3 = await client.GetAsync("/api3/Posts/Index");

            Assert.Equal(HttpStatusCode.OK, defaultPage.StatusCode);
            Assert.Equal(HttpStatusCode.OK, defaultPage2.StatusCode);
            Assert.Equal(HttpStatusCode.OK, defaultPage3.StatusCode);

            Assert.Equal(HttpStatusCode.OK, defaultPagePost.StatusCode);
            Assert.Equal(HttpStatusCode.OK, defaultPagePost2.StatusCode);
            Assert.Equal(HttpStatusCode.OK, defaultPagePost3.StatusCode);

            var q = await defaultPage.Content.ReadAsStringAsync();

            System.Console.Write(q.ToString());
    }

    [Fact]
    public async Task TestCountPage()
    {
            // Arrange
            var client = _factory.CreateClient();
            // Arrange
            var defaultPage = await client.GetAsync("/api/Blogs/Count");
            var defaultPage2 = await client.GetAsync("/api2/Blogs/Count");
            var defaultPage3 = await client.GetAsync("/api3/Blogs/Count");

            var defaultPagePost = await client.GetAsync("/api/Posts/Count");
            var defaultPagePost2 = await client.GetAsync("/api2/Posts/Count");
            var defaultPagePost3 = await client.GetAsync("/api3/Posts/Count");

            Assert.Equal(HttpStatusCode.OK, defaultPage.StatusCode);
            Assert.Equal(HttpStatusCode.OK, defaultPage2.StatusCode);
            Assert.Equal(HttpStatusCode.OK, defaultPage3.StatusCode);

            Assert.Equal(HttpStatusCode.OK, defaultPagePost.StatusCode);
            Assert.Equal(HttpStatusCode.OK, defaultPagePost2.StatusCode);
            Assert.Equal(HttpStatusCode.OK, defaultPagePost3.StatusCode);

            //System.Console.Write(defaultPage.ToString());
    }
}