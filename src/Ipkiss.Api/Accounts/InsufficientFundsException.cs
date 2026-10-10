namespace Ipkiss.Api.Accounts;

public sealed class InsufficientFundsException(string accountId)
    : Exception($"Account '{accountId}' does not have enough balance.");
