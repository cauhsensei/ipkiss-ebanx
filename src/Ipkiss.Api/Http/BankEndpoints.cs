using System.Globalization;
using Ipkiss.Api.Accounts;
using Microsoft.AspNetCore.Mvc;

namespace Ipkiss.Api.Http;

public static class BankEndpoints
{
    public static void MapBankEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/reset", Reset);
        app.MapGet("/balance", GetBalance);
        app.MapPost("/event", HandleEvent);
    }

    private static IResult Reset(Bank bank)
    {
        bank.Reset();
        return Results.Text("OK");
    }

    private static IResult GetBalance([FromQuery(Name = "account_id")] string accountId, Bank bank)
    {
        var balance = bank.GetBalance(accountId);
        return balance is null
            ? NotFound()
            : Results.Text(balance.Value.ToString(CultureInfo.InvariantCulture));
    }

    private static IResult HandleEvent(EventRequest request, Bank bank)
    {
        try
        {
            return request switch
            {
                { Type: "deposit", Destination: { } destination } =>
                    Created(new { destination = bank.Deposit(destination, request.Amount) }),
                { Type: "withdraw", Origin: { } origin } =>
                    Created(new { origin = bank.Withdraw(origin, request.Amount) }),
                { Type: "transfer", Origin: { } origin, Destination: { } destination } =>
                    Transfer(bank, origin, destination, request.Amount),
                _ => BadRequest()
            };
        }
        catch (AccountNotFoundException)
        {
            return NotFound();
        }
        catch (Exception e) when (e is InsufficientFundsException or InvalidAmountException)
        {
            return BadRequest();
        }
    }

    private static IResult Transfer(Bank bank, string originId, string destinationId, decimal amount)
    {
        var (origin, destination) = bank.Transfer(originId, destinationId, amount);
        return Created(new { origin, destination });
    }

    private static IResult Created(object body) => Results.Json(body, statusCode: StatusCodes.Status201Created);

    private static IResult NotFound() => Results.Text("0", statusCode: StatusCodes.Status404NotFound);

    private static IResult BadRequest() => Results.Text("0", statusCode: StatusCodes.Status400BadRequest);
}
