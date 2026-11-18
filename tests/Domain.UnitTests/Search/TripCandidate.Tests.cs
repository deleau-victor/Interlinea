using Domain.Search;

namespace Domain.UnitTests.Search;

public sealed class TripCandidateTests
{
    private static readonly DateOnly FirstOfDecember = new(2026, 12, 1);

    private static TripCandidate Candidate(DateOnly? back) =>
        new(new AirportCode("CDG"), new AirportCode("LIM"), FirstOfDecember, back);

    // —————————————————————— STAY LENGTH TESTS ——————————————————————

    [Fact]
    public void Nights_OfARoundTrip_IsTheNumberOfDaysBetweenTheTwoDates()
    {
        // Arrange & Act
        TripCandidate candidate = Candidate(new DateOnly(2026, 12, 20));

        // Assert
        candidate.IsRoundTrip.Should().BeTrue();
        candidate.Nights.Should().Be(19);
    }

    [Fact]
    public void Nights_OfAOneWay_IsZero()
    {
        // Arrange & Act
        TripCandidate candidate = Candidate(back: null);

        // Assert
        candidate.Return.Should().BeNull();
        candidate.IsRoundTrip.Should().BeFalse();
        candidate.Nights.Should().Be(0);
    }

    [Fact]
    public void Nights_OfATripComingBackTheSameDay_IsZeroButStillARoundTrip()
    {
        // Arrange & Act — zero nights and a one-way both answer zero, so the two cases are
        // only told apart by IsRoundTrip. Asserting the count alone would confuse them.
        TripCandidate candidate = Candidate(FirstOfDecember);

        // Assert
        candidate.Nights.Should().Be(0);
        candidate.IsRoundTrip.Should().BeTrue();
    }

    // —————————————————————— VALIDATION TESTS ——————————————————————

    [Fact]
    public void Constructor_WithAReturnLeavingBeforeTheDeparture_ThrowsArgumentException()
    {
        // Arrange & Act
        Func<TripCandidate> candidate = () => Candidate(FirstOfDecember.AddDays(-1));

        // Assert
        candidate.Should()
                 .ThrowExactly<ArgumentException>().Which.ParamName
                 .Should()
                 .Be(nameof(TripCandidate.Return));
    }

    [Fact]
    public void Constructor_WithTheSameOriginAndDestination_ThrowsArgumentException()
    {
        // Arrange & Act
        Func<TripCandidate> candidate = () => new(
            new AirportCode("CDG"), new AirportCode("CDG"), FirstOfDecember, new DateOnly(2026, 12, 20));

        // Assert
        candidate.Should()
                 .ThrowExactly<ArgumentException>().Which.ParamName
                 .Should()
                 .Be(nameof(TripCandidate.Destination));
    }
}
