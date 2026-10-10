namespace Ipkiss.Api.Accounts;

public sealed class Bank
{
    private readonly Dictionary<string, decimal> _balances = new();
    private readonly Lock _lock = new();

    public decimal? GetBalance(string accountId)
    {
        lock (_lock)
            return _balances.TryGetValue(accountId, out var balance) ? balance : null;
    }

    public Account Deposit(string destinationId, decimal amount)
    {
        EnsurePositive(amount);

        lock (_lock)
        {
            Credit(destinationId, amount);
            return Snapshot(destinationId);
        }
    }

    private void Credit(string accountId, decimal amount) =>
        _balances[accountId] = _balances.GetValueOrDefault(accountId) + amount;

    private Account Snapshot(string accountId) => new(accountId, _balances[accountId]);

    private static void EnsurePositive(decimal amount)
    {
        if (amount <= 0)
            throw new InvalidAmountException(amount);
    }
}
