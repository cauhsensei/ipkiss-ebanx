namespace Ipkiss.Api.Accounts;

public sealed class AccountNotFoundException(string accountId)
    : Exception($"Account '{accountId}' does not exist.");
