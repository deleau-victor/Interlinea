---
paths:
  - "src/Domain/**/*.cs"
---

# Domain

- No dependencies: no NuGet package.
- Pure functions: `Planner`, `Composer` and `Ranker` take data and return data. Configuration (catalogs, policies) goes through the constructor. No repository interface, no injected service.
- Names come from the README glossary. Missing term: ask, do not create a synonym. The same word never means two things (`Ticket.FirstJourney` is not `Offer.Outbound`).
- Everything is immutable: records and readonly record structs, `init` at most, changes through `with`. No public setter, no mutable collection exposed.
- Invariants are checked in the constructor: an invalid object cannot exist. An invariant
  violation throws `ArgumentException` — even when a user of the web interface can produce
  it. We would rather refuse the object than carry an incoherent one. Provisional: the day
  we introduce a result type, this is the line that changes.
- A SELECTION outcome is never an exception: no reachable hub, no retained offer, an
  impossible connection, a side that was never measured — `null`, an empty list, or
  filtered out.
- Time: instants are `DateTimeOffset` (local time + offset); durations are computed on instants only. `DateOnly` / `TimeOnly` only for user-facing dates and Google Flights time filters. Never `DateTime`.
- Money: never add or compare amounts in different currencies; convert through `ExchangeRates`.
- A physical flight is identified by `Segment.PhysicalKey`, never by its flight number (codeshares).
- Business rules are data (`ConnectionPolicy`, `ProbePolicy`), not hard-coded constants.
- Respect the bounded context dependencies listed in CLAUDE.md.
- XML documentation proportionate to what the member hides: one line when the name nearly
  says it, the why and a concrete example (Paris→Madrid→Lima) when a reader cannot deduce
  the rule — the anchor side, the pivot currency, a floor that is a total for both
  directions.
- No persistence or serialization attribute here.
- No I/O, no ambient clock. The current instant arrives as a parameter
  (`SearchRequest.RequestedAt`, or an argument). `TimeProvider` belongs to the Api, never
  here: the domain injects nothing.
