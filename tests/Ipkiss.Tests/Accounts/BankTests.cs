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

    [Fact]
    public void Transfer_moves_amount_from_origin_to_new_destination()
    {
        _bank.Deposit("100", 15);

        var (origin, destination) = _bank.Transfer("100", "300", 15);

        Assert.Equal(new Account("100", 0), origin);
        Assert.Equal(new Account("300", 15), destination);
        Assert.Equal(0, _bank.GetBalance("100"));
        Assert.Equal(15, _bank.GetBalance("300"));
    }

    [Fact]
    public void Transfer_into_existing_destination_adds_to_its_balance()
    {
        _bank.Deposit("100", 10);
        _bank.Deposit("300", 5);

        _bank.Transfer("100", "300", 4);

        Assert.Equal(6, _bank.GetBalance("100"));
        Assert.Equal(9, _bank.GetBalance("300"));
    }

    [Fact]
    public void Transfer_from_unknown_origin_fails_without_touching_destination()
    {
        Assert.Throws<AccountNotFoundException>(() => _bank.Transfer("200", "300", 15));

        Assert.Null(_bank.GetBalance("200"));
        Assert.Null(_bank.GetBalance("300"));
    }

    [Fact]
    public void Transfer_beyond_balance_fails_and_keeps_both_balances()
    {
        _bank.Deposit("100", 10);
        _bank.Deposit("300", 1);

        Assert.Throws<InsufficientFundsException>(() => _bank.Transfer("100", "300", 11));

        Assert.Equal(10, _bank.GetBalance("100"));
        Assert.Equal(1, _bank.GetBalance("300"));
    }

    [Fact]
    public void Transfer_to_the_same_account_keeps_its_balance()
    {
        _bank.Deposit("100", 10);

        var (origin, destination) = _bank.Transfer("100", "100", 4);

        Assert.Equal(new Account("100", 10), origin);
        Assert.Equal(new Account("100", 10), destination);
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
        Assert.Throws<InvalidAmountException>(() => _bank.Transfer("100", "300", amount));

        Assert.Equal(10, _bank.GetBalance("100"));
        Assert.Null(_bank.GetBalance("999"));
        Assert.Null(_bank.GetBalance("300"));
    }

    [Fact]
    public async Task Concurrent_transfers_preserve_the_total_balance()
    {
        _bank.Deposit("A", 1000);
        _bank.Deposit("B", 1000);

        var transfers = Enumerable.Range(0, 2000).Select(i => Task.Run(() =>
        {
            try
            {
                if (i % 2 == 0)
                    _bank.Transfer("A", "B", 3);
                else
                    _bank.Transfer("B", "A", 3);
            }
            catch (InsufficientFundsException)
            {
            }
        }));
        await Task.WhenAll(transfers);

        Assert.Equal(2000, _bank.GetBalance("A") + _bank.GetBalance("B"));
        Assert.True(_bank.GetBalance("A") >= 0);
        Assert.True(_bank.GetBalance("B") >= 0);
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
