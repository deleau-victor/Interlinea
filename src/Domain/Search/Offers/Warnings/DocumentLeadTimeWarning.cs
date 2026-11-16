namespace Domain.Search.Offers.Warnings;

/// <summary>
/// The departure date is too close to be sure I'll be able to get the required document.
/// </summary>
/// <param name="Country">The country who exiges the document</param>
/// <param name="Document">The required document to be able to complete the layover</param>
/// <param name="LeadTime">The obtention delay time</param>
public sealed record DocumentLeadTimeWarning(CountryCode Country, string Document, TimeSpan LeadTime) : OfferWarning;
