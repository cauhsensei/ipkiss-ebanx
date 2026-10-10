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

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Deposit_rejects_non_positive_amounts_without_changing_state(decimal amount)
    {
        _bank.Deposit("100", 10);

        Assert.Throws<InvalidAmountException>(() => _bank.Deposit("100", amount));
        Assert.Throws<InvalidAmountException>(() => _bank.Deposit("999", amount));

        Assert.Equal(10, _bank.GetBalance("100"));
        Assert.Null(_bank.GetBalance("999"));
    }
}
