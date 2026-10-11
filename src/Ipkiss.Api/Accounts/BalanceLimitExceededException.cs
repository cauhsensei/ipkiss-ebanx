namespace Ipkiss.Api.Accounts;

public sealed class BalanceLimitExceededException(string accountId)
    : Exception($"Account '{accountId}' cannot hold a larger balance.");
