using Ipkiss.Api.Accounts;

namespace Ipkiss.Tests.Accounts;

public class BankTests
{
    private readonly Bank _bank = new();

    [Fact]
    public void GetBalance_of_unknown_account_returns_null_and_does_not_create_it()
    {
        Assert.Null(_bank.GetBalance("1234"));
        Assert.Null(_bank.GetBalance("1234"));
    }

    [Fact]
    public void Deposit_creates_account_with_the_deposited_amount()
    {
        var account = _bank.Deposit("100", 10);

        Assert.Equal(new Account("100", 10), account);
        Assert.Equal(10, _bank.GetBalance("100"));
    }

    [Fact]
    public void Deposit_into_existing_account_increases_balance()
    {
        _bank.Deposit("100", 10);

        var account = _bank.Deposit("100", 10);

        Assert.Equal(20, account.Balance);
        Assert.Equal(20, _bank.GetBalance("100"));
    }

    [Fact]
    public void Withdraw_reduces_balance()
    {
        _bank.Deposit("100", 20);

        var account = _bank.Withdraw("100", 5);

        Assert.Equal(new Account("100", 15), account);
        Assert.Equal(15, _bank.GetBalance("100"));
    }

    [Fact]
    public void Withdraw_of_the_whole_balance_leaves_zero()
    {
        _bank.Deposit("100", 20);

        _bank.Withdraw("100", 20);

        Assert.Equal(0, _bank.GetBalance("100"));
    }

    [Fact]
    public void Withdraw_from_unknown_account_fails_without_creating_it()
    {
        Assert.Throws<AccountNotFoundException>(() => _bank.Withdraw("200", 10));

        Assert.Null(_bank.GetBalance("200"));
    }

    [Fact]
    public void Withdraw_beyond_balance_fails_and_keeps_balance()
    {
        _bank.Deposit("100", 10);

        Assert.Throws<InsufficientFundsException>(() => _bank.Withdraw("100", 11));

        Assert.Equal(10, _bank.GetBalance("100"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Operations_reject_non_positive_amounts_without_changing_state(decimal amount)
    {
        _bank.Deposit("100", 10);

        Assert.Throws<InvalidAmountException>(() => _bank.Deposit("100", amount));
        Assert.Throws<InvalidAmountException>(() => _bank.Deposit("999", amount));
        Assert.Throws<InvalidAmountException>(() => _bank.Withdraw("100", amount));

        Assert.Equal(10, _bank.GetBalance("100"));
        Assert.Null(_bank.GetBalance("999"));
    }

    [Fact]
    public async Task Concurrent_withdrawals_never_overdraw()
    {
        _bank.Deposit("100", 100);

        var withdrawals = Enumerable.Range(0, 500).Select(_ => Task.Run(() =>
        {
            try
            {
                _bank.Withdraw("100", 1);
                return true;
            }
            catch (InsufficientFundsException)
            {
                return false;
            }
        }));
        var results = await Task.WhenAll(withdrawals);

        Assert.Equal(100, results.Count(succeeded => succeeded));
        Assert.Equal(0, _bank.GetBalance("100"));
    }
}
