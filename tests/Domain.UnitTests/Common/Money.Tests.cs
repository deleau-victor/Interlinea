namespace Domain.UnitTests.Common;

public sealed class MoneyTests
{
    // —————————————————————— CONSTRUCTOR TESTS ——————————————————————

    [Fact]
    public void Constructor_WithNegativeAmount_ThrowsArgumentOutOfRangeException()
    {
        // Arrange & Act
        Func<Money> act = () => new Money(-1m, "EUR");

        // Assert
        act.Should()
           .ThrowExactly<ArgumentOutOfRangeException>().Which.ParamName
           .Should()
           .Be(nameof(Money.Amount));
    }

    [Theory]
    [Cases("", " ", "aws", "AAAAAA", "eur", "E1R")]
    public void Constructor_WithInvalidCurrencyCodes_ThrowsArgumentException(string currency)
    {
        // Arrange & Act
        Func<Money> act = () => new Money(1m, currency);

        // Assert
        act.Should()
           .ThrowExactly<ArgumentException>().Which.ParamName
           .Should()
           .Be(nameof(Money.Currency));
    }

    [Theory]
    [Cases(0d, 12.5d)]
    public void Constructor_WithValidValues_ExposesAmountAndCurrency(double amount)
    {
        // Arrange & Act
        Money money = new((decimal)amount, "EUR");

        // Assert
        money.Amount.Should().Be((decimal)amount);
        money.Currency.Should().Be("EUR");
    }

    // —————————————————————— SUM TESTS ——————————————————————

    [Fact]
    public void Sum_OfEmpty_ThrowsArgumentException()
    {
        // Arrange & Act
        Action sum = () => Money.Sum([]);

        // Assert
        sum.Should()
           .ThrowExactly<ArgumentException>().Which.ParamName
           .Should()
           .Be("items");
    }

    [Fact]
    public void Sum_SameCurrency_AddsTheAmounts()
    {
        // Arrange & Act
        var sum = Money.Sum([new Money(80m, "EUR"), new Money(12.50m, "EUR")]);

        // Assert
        sum.Amount.Should().Be(92.5m);
    }

    [Fact]
    public void Sum_WithCurrency_OfEmpty_IsZeroInThatCurrency()
    {
        // Arrange & Act
        var total = Money.Sum([], "EUR");

        // Assert
        total.Should().Be(new Money(0m, "EUR"));
    }

    [Fact]
    public void Sum_WithCurrency_WithAnotherCurrency_ThrowsInvalidOperationException()
    {
        // Arrange & Act
        Action sum = () => Money.Sum([new Money(80m, "USD")], "EUR");

        // Assert
        sum.Should().ThrowExactly<InvalidOperationException>();
    }

    [Fact]
    public void Sum_WithCurrency_AddsTheAmounts()
    {
        // Arrange & Act
        var total = Money.Sum([new Money(80m, "EUR"), new Money(12.50m, "EUR")], "EUR");

        // Assert
        total.Should().Be(new Money(92.50m, "EUR"));
    }

    // —————————————————————— COMPARE TO TESTS ——————————————————————

    [Fact]
    public void CompareTo_WithDifferentCurrencies_ThrowsInvalidOperationException()
    {
        // Arrange & Act
        Action compareTo = () => new Money(80m, "EUR").CompareTo(new Money(12.50m, "USD"));

        // Assert
        compareTo.Should().ThrowExactly<InvalidOperationException>();
    }

    [Theory]
    [InlineData(80.0, 12.50, 1)]
    [InlineData(10.30, 56.50, -1)]
    [InlineData(50.0, 50.0, 0)]
    public void CompareTo_WithSameCurrency_ReturnsSignOfAmountDifference(
        decimal value1,
        decimal value2,
        int comparisonResult)
    {
        // Arrange & Act
        int comparison = new Money(value1, "EUR").CompareTo(new Money(value2, "EUR"));

        // Assert
        comparison.Should().Be(comparisonResult);
    }

    // —————————————————————— OPERATORS TESTS ——————————————————————

    [Fact]
    public void Subtract_WithDifferentCurrencies_ThrowsInvalidOperationException()
    {
        // Arrange & Act
        Func<Money> subtract = () => new Money(10m, "EUR") - new Money(10m, "USD");

        // Assert
        subtract.Should().ThrowExactly<InvalidOperationException>();
    }

    [Fact]
    public void Subtract_WithANegativeResult_ThrowsInvalidOperationException()
    {
        // Arrange & Act
        Func<Money> subtract = () => new Money(10m, "EUR") - new Money(20m, "EUR");

        // Assert
        subtract.Should().ThrowExactly<InvalidOperationException>();
    }

    [Theory]
    [InlineData(10, 10, 0)]
    [InlineData(20, 10, 10)]
    [InlineData(11.5, 1.752, 9.748)]
    public void Subtract_WithAPositiveValue_ReturnMoneyWithValidAmount(decimal value1, decimal value2, decimal expectedResult)
    {
        // Arrange
        Money money1 = new(value1, "EUR");
        Money money2 = new(value2, "EUR");

        // Act
        Money resultMoney = money1 - money2;

        // Assert
        resultMoney.Amount.Should().Be(expectedResult);
    }

    [Fact]
    public void Addition_WithDifferentCurrencies_ThrowsInvalidOperationException()
    {
        // Arrange & Act
        Func<Money> addition = () => new Money(10m, "EUR") + new Money(10m, "USD");

        // Assert
        addition.Should().ThrowExactly<InvalidOperationException>();
    }

    [Fact]
    public void Addition_WithSameCurrency_ReturnMoneyWithValidAmount()
    {
        // Arrange
        Money money1 = new(10.5m, "EUR");
        Money money2 = new(20m, "EUR");

        // Act
        Money moneyResult = money1 + money2;

        // Assert
        moneyResult.Amount.Should().Be(30.5m);
    }

    [Fact]
    public void Comparators_WithDifferentCurrencies_ThrowsInvalidOperationException()
    {
        // Arrange & Act
        Func<bool> superior = () => new Money(10m, "EUR") > new Money(20m, "USD");
        Func<bool> superiorOrEquals = () => new Money(10m, "EUR") >= new Money(20m, "USD");
        Func<bool> inferior = () => new Money(10m, "EUR") < new Money(20m, "USD");
        Func<bool> inferiorOrEquals = () => new Money(10m, "EUR") <= new Money(20m, "USD");

        // Assert
        superior.Should().ThrowExactly<InvalidOperationException>();
        superiorOrEquals.Should().ThrowExactly<InvalidOperationException>();
        inferior.Should().ThrowExactly<InvalidOperationException>();
        inferiorOrEquals.Should().ThrowExactly<InvalidOperationException>();
    }

    [Theory]
    [InlineData(10, 20, true, true, false, false)]
    [InlineData(20, 20, false, true, false, true)]
    [InlineData(30, 20, false, false, true, true)]
    public void Comparators_WithSameCurrency_ReturnExpectedResult(
    decimal left, decimal right, bool lt, bool le, bool gt, bool ge)
    {
        // Arrange
        Money a = new(left, "EUR");
        Money b = new(right, "EUR");

        // Act & Assert
        (a < b).Should().Be(lt);
        (a <= b).Should().Be(le);
        (a > b).Should().Be(gt);
        (a >= b).Should().Be(ge);
    }

    // —————————————————————— TO STRING TESTS ——————————————————————

    [Theory]
    [InlineData(12.5, "12.5 EUR")]
    [InlineData(12, "12 EUR")]
    public void ToString_UsesInvariantCultureAndTwoDecimalsMax(decimal amount, string expected)
    {
        // Arrange
        CultureInfo previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
        try
        {
            // Act
            string text = new Money(amount, "EUR").ToString();

            // Assert
            text.Should().Be(expected);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }
}
