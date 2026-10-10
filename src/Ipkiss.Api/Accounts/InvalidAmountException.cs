namespace Ipkiss.Api.Accounts;

public sealed class InvalidAmountException(decimal amount)
    : Exception($"Amount must be greater than zero, got {amount}.");
