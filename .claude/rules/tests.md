---
paths:
  - "tests/**/*.cs"
---

# Tests

- xunit.v3 + AwesomeAssertions. Never FluentAssertions.
- Naming: Method_Context_ExpectedResult (or the convention already used in the neighboring file).
- Arrange / Act / Assert, one behavior per test.
- Do not mock the domain. Test doubles only for ports.
- Async: pass TestContext.Current.CancellationToken.
- `UnitTests`: no network, no disk, no database. Otherwise it is an `IntegrationTests`.
- A test must be able to fail: check the red before the green.
- Before writing an assertion, name the production change that would make it fail. If
  the answer only cites operators present in the assertion, it is a tautology:
  `offer.TotalCost.Should().Be(offer.TicketsPrice + offer.AncillaryTotal)` is the body of
  `TotalCost` — break `AncillaryTotal` and both sides move together.
- Then ask the reverse question: which WRONG rule would produce the same expected value?
  `AnchorSide == Origin` is an impeccable literal, and five different rules produce it on
  a poorly chosen dataset. Fix it by choosing data where the candidate causes diverge,
  not by asserting something else.
- A count is invariant under permutation: `HaveCount(2)` sees neither the wrong side, nor
  the wrong direction, nor the wrong date window. Assert the exact set.
- Pick numbers that no plausible faulty implementation lands on: two tickets
  600 + 300 and two fees 80 + 25 give 900 / 105 / 1005, all distinct. A single ticket
  and a single fee cannot tell a sum from "take the first one".
- Test the CALL SITE, not only the function. A validation extracted into a shared helper
  stays covered by the other caller's tests; the removed call does not.
- Check that no branch is made unreachable by the fixture. With thresholds of
  2000/4000 km and a five-airport catalog, the middle branch of `ProbePolicy` was never
  executed: two green tests, a third of the function never called.
- For documentation, the test is "is it true". To verify a predicted failure, cause the
  predicted failure.
