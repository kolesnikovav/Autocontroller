using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net;
using AppExample;
using DbContextExample;
using System.Diagnostics;
using System.Text.Json;
using System.Xml.Serialization;
using System.IO;
using Xunit;

namespace AutocontrollerTests;

public class IntegrationTests : IClassFixture<WebApplicationFactory<Startup>>
{
    private readonly WebApplicationFactory<AppExample.Startup> _factory;
    public IntegrationTests(WebApplicationFactory<AppExample.Startup> factory)
    {
        _factory = factory;
        // database seeding
        using (var scope = _factory.Services.CreateScope())
        {
            var scopedServices = scope.ServiceProvider;
            try
            {
                var context = scopedServices.GetRequiredService<AppDBContext>();
                Utility.ReinitializeDbForTests(context);
            }
            catch (Exception ex)
            {
                var logger = scopedServices.GetRequiredService<ILogger<IntegrationTests>>();
                logger.LogError(ex, "An error occurred while seeding the database.");
            }
        };
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

        // Check out that defaultPage is JSON with 3 blogs
        using (var reader = new StreamReader(defaultPage.Content.ReadAsStream()))
        {
            var body = await reader.ReadToEndAsync();
            var recivedData = JsonSerializer.Deserialize<List<DbContextExample.Blog>>(body);
            Assert.Equal(3, recivedData.Count);
            //Assert.Contains<DbContextExample.Blog>( new Blog { Id = 1, Description = "Test blog1"}, recivedData);
        }

        // Check out that defaultPage is XML with 3 blogs
        using (var reader = new StreamReader(defaultPage2.Content.ReadAsStream()))
        {
            // Stream stream = GenerateStreamFromString(body);
            XmlRootAttribute a = new XmlRootAttribute("result");
            XmlSerializer serializer = new XmlSerializer(typeof(List<DbContextExample.Blog>), a);
            List<DbContextExample.Blog> recivedDataL = (List<DbContextExample.Blog>)serializer.Deserialize(reader);
            Assert.Equal(3, recivedDataL.Count);
        }
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

        var defaultPageWithFilter = await client.GetAsync("/api/Blogs/Count?filter=Id=2");
        var defaultPageWithFilter2 = await client.GetAsync("/api/Blogs/Count?filter=Id>1");

        Assert.Equal(HttpStatusCode.OK, defaultPage.StatusCode);
        Assert.Equal(HttpStatusCode.OK, defaultPage2.StatusCode);
        Assert.Equal(HttpStatusCode.OK, defaultPage3.StatusCode);

        Assert.Equal(HttpStatusCode.OK, defaultPagePost.StatusCode);
        Assert.Equal(HttpStatusCode.OK, defaultPagePost2.StatusCode);
        Assert.Equal(HttpStatusCode.OK, defaultPagePost3.StatusCode);

        var countApi = await defaultPage.Content.ReadAsStringAsync();
        Assert.Equal(3, Int32.Parse(countApi));
        var countApi2 = await defaultPage2.Content.ReadAsStringAsync();
        Assert.Equal(3, Int32.Parse(countApi2));
        var countApi3 = await defaultPage3.Content.ReadAsStringAsync();
        Assert.Equal(3, Int32.Parse(countApi3));

        var countApiPost = await defaultPagePost.Content.ReadAsStringAsync();
        Assert.Equal(3, Int32.Parse(countApiPost));
        var countApiPost2 = await defaultPagePost2.Content.ReadAsStringAsync();
        Assert.Equal(3, Int32.Parse(countApiPost2));
        var countApiPost3 = await defaultPagePost3.Content.ReadAsStringAsync();
        Assert.Equal(3, Int32.Parse(countApiPost3));

        var countApiWithFilter = await defaultPageWithFilter.Content.ReadAsStringAsync();
        Assert.Equal(1, Int32.Parse(countApiWithFilter));
        var countApiWithFilter2 = await defaultPageWithFilter2.Content.ReadAsStringAsync();
        Assert.Equal(2, Int32.Parse(countApiWithFilter2));
    }
}