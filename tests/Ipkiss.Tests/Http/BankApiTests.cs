using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Ipkiss.Tests.Http;

public sealed class BankApiTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory = new();
    private readonly HttpClient _client;

    public BankApiTests() => _client = _factory.CreateClient();

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task Ipkiss_script_passes_end_to_end()
    {
        await AssertText(await _client.PostAsync("/reset", null), HttpStatusCode.OK, "OK");

        await AssertText(await GetBalance("1234"), HttpStatusCode.NotFound, "0");

        await AssertJson(await PostEvent("""{"type":"deposit", "destination":"100", "amount":10}"""),
            HttpStatusCode.Created, """{"destination": {"id":"100", "balance":10}}""");

        await AssertJson(await PostEvent("""{"type":"deposit", "destination":"100", "amount":10}"""),
            HttpStatusCode.Created, """{"destination": {"id":"100", "balance":20}}""");

        await AssertText(await GetBalance("100"), HttpStatusCode.OK, "20");

        await AssertText(await PostEvent("""{"type":"withdraw", "origin":"200", "amount":10}"""),
            HttpStatusCode.NotFound, "0");

        await AssertJson(await PostEvent("""{"type":"withdraw", "origin":"100", "amount":5}"""),
            HttpStatusCode.Created, """{"origin": {"id":"100", "balance":15}}""");

        await AssertJson(await PostEvent("""{"type":"transfer", "origin":"100", "amount":15, "destination":"300"}"""),
            HttpStatusCode.Created, """{"origin": {"id":"100", "balance":0}, "destination": {"id":"300", "balance":15}}""");

        await AssertText(await PostEvent("""{"type":"transfer", "origin":"200", "amount":15, "destination":"300"}"""),
            HttpStatusCode.NotFound, "0");

        await AssertText(await GetBalance("100"), HttpStatusCode.OK, "0");
        await AssertText(await GetBalance("300"), HttpStatusCode.OK, "15");
    }

    [Fact]
    public async Task Reading_a_balance_does_not_change_state()
    {
        await PostEvent("""{"type":"deposit", "destination":"100", "amount":10}""");

        await AssertText(await GetBalance("100"), HttpStatusCode.OK, "10");
        await AssertText(await GetBalance("100"), HttpStatusCode.OK, "10");
        await AssertText(await GetBalance("999"), HttpStatusCode.NotFound, "0");
        await AssertText(await GetBalance("999"), HttpStatusCode.NotFound, "0");
    }

    [Fact]
    public async Task Withdraw_beyond_balance_is_rejected_and_keeps_balance()
    {
        await PostEvent("""{"type":"deposit", "destination":"100", "amount":10}""");

        await AssertText(await PostEvent("""{"type":"withdraw", "origin":"100", "amount":11}"""),
            HttpStatusCode.BadRequest, "0");

        await AssertText(await GetBalance("100"), HttpStatusCode.OK, "10");
    }

    [Fact]
    public async Task Transfer_beyond_balance_is_rejected_and_keeps_both_accounts()
    {
        await PostEvent("""{"type":"deposit", "destination":"100", "amount":10}""");

        await AssertText(await PostEvent("""{"type":"transfer", "origin":"100", "amount":11, "destination":"300"}"""),
            HttpStatusCode.BadRequest, "0");

        await AssertText(await GetBalance("100"), HttpStatusCode.OK, "10");
        await AssertText(await GetBalance("300"), HttpStatusCode.NotFound, "0");
    }

    [Theory]
    [InlineData("""{"type":"deposit", "destination":"100", "amount":0}""")]
    [InlineData("""{"type":"deposit", "destination":"100", "amount":-10}""")]
    [InlineData("""{"type":"deposit", "amount":10}""")]
    [InlineData("""{"type":"refund", "destination":"100", "amount":10}""")]
    public async Task Invalid_events_are_rejected_without_creating_accounts(string body)
    {
        await AssertText(await PostEvent(body), HttpStatusCode.BadRequest, "0");

        await AssertText(await GetBalance("100"), HttpStatusCode.NotFound, "0");
    }

    [Theory]
    [InlineData("""{"type":"deposit", "destination":"", "amount":10}""")]
    [InlineData("""{"type":"deposit", "destination":"   ", "amount":10}""")]
    [InlineData("""{"type":"withdraw", "origin":"", "amount":5}""")]
    [InlineData("""{"type":"transfer", "origin":"100", "amount":5, "destination":""}""")]
    public async Task Events_with_blank_account_ids_are_rejected(string body)
    {
        await PostEvent("""{"type":"deposit", "destination":"100", "amount":10}""");

        await AssertText(await PostEvent(body), HttpStatusCode.BadRequest, "0");

        await AssertText(await GetBalance("100"), HttpStatusCode.OK, "10");
    }

    [Fact]
    public async Task Deposit_beyond_the_balance_limit_is_rejected()
    {
        await PostEvent($$"""{"type":"deposit", "destination":"100", "amount":{{decimal.MaxValue}}}""");

        await AssertText(await PostEvent("""{"type":"deposit", "destination":"100", "amount":1}"""),
            HttpStatusCode.BadRequest, "0");
    }

    [Theory]
    [InlineData("{bad")]
    [InlineData("")]
    [InlineData("""{"type":"deposit", "destination":"100", "amount":"ten"}""")]
    public async Task Malformed_event_bodies_get_the_uniform_error_response(string body)
    {
        await AssertText(await PostEvent(body), HttpStatusCode.BadRequest, "0");
    }

    [Fact]
    public async Task Balance_without_account_id_gets_the_uniform_error_response()
    {
        await AssertText(await _client.GetAsync("/balance"), HttpStatusCode.BadRequest, "0");
    }

    [Fact]
    public async Task Reset_clears_all_accounts()
    {
        await PostEvent("""{"type":"deposit", "destination":"100", "amount":10}""");

        await _client.PostAsync("/reset", null);

        await AssertText(await GetBalance("100"), HttpStatusCode.NotFound, "0");
    }

    private Task<HttpResponseMessage> GetBalance(string accountId) =>
        _client.GetAsync($"/balance?account_id={accountId}");

    private Task<HttpResponseMessage> PostEvent(string json) =>
        _client.PostAsync("/event", new StringContent(json, Encoding.UTF8, "application/json"));

    private static async Task AssertText(HttpResponseMessage response, HttpStatusCode status, string body)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal(body, await response.Content.ReadAsStringAsync());
    }

    private static async Task AssertJson(HttpResponseMessage response, HttpStatusCode status, string body)
    {
        Assert.Equal(status, response.StatusCode);
        var actual = JsonNode.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(body), actual), $"Unexpected body: {actual}");
    }
}
