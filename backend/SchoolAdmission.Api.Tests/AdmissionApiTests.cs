using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace SchoolAdmission.Api.Tests;

public class AdmissionApiTests
{
    private static readonly HttpClient Client = new()
    {
        BaseAddress = new Uri(
            Environment.GetEnvironmentVariable("TEST_API_BASE_URL")
            ?? "http://localhost:8080")
    };

    [Fact]
    public async Task CanCreateClass()
    {
        using var response = await Client.PostAsJsonAsync(
            "/classes",
            new { name = $"Test Class {Guid.NewGuid():N}", totalSeats = 2 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(body.GetProperty("id").GetGuid() == Guid.Empty);
        Assert.Equal(2, body.GetProperty("totalSeats").GetInt32());
    }

    [Fact]
    public async Task InvalidStudentIdIsRejected()
    {
        var classId = await CreateClassAsync(1);
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/classes/{classId}/holds");
        request.Headers.Add("X-Student-Id", "not-a-student-id");

        using var response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ConcurrentApplicationsNeverExceedCapacity()
    {
        const int seats = 5;
        var classId = await CreateClassAsync(seats);

        var requests = Enumerable.Range(0, 30).Select(async index =>
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"/classes/{classId}/holds");
            request.Headers.Add(
                "X-Student-Id",
                $"201526912{index:00000000}");
            return await Client.SendAsync(request);
        });

        var responses = await Task.WhenAll(requests);
        var successful = responses.Count(response =>
            response.StatusCode == HttpStatusCode.Created);

        Assert.Equal(seats, successful);
        Assert.True(
            successful <= seats,
            $"Created {successful} holds for {seats} seats.");
    }

    private static async Task<Guid> CreateClassAsync(int seats)
    {
        using var response = await Client.PostAsJsonAsync(
            "/classes",
            new { name = $"Test Class {Guid.NewGuid():N}", totalSeats = seats });
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("id").GetGuid();
    }
}
