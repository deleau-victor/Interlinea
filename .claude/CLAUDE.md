# Interlinea

Interlinea is a flight search engine that finds the cheapest trip between two cities.
It compares regular tickets and combines separate tickets through an intermediate airport (virtual interlining), for example Paris→Madrid followed by a Madrid⇄Lima round trip.
It relies on knowledge of airline revenue management to know where to search, and collects airline and online travel agency prices through Google Flights.

## Stack

.NET 10 (SDK 10.0.402), C# 14, xunit.v3 on Microsoft Testing Platform, AwesomeAssertions.
Package versions are centralized in Directory.Packages.props (read-only).
Scraper in Python, SPA in React / TypeScript, dev orchestration with .NET Aspire.

## Commands

- Build: `dotnet build Interlinea.slnx -warnaserror`
- Tests for one project: `dotnet test --project tests/Domain.UnitTests`
- Filtering: `--filter-class`, `--filter-method`, `--filter-trait`. The VSTest-style
  `--filter "FullyQualifiedName~X"` works too, and is the only form that expresses an
  exclusion or a compound predicate (`&`, `!~`).
- Full stack in dev: `dotnet run --project src/AppHost`
- Scraper and SPA: (to be completed)
- Never run the scraper against Google Flights unless explicitly asked: every request consumes anti-bot budget.
- `System.Console` is banned in the repository: use ILogger. Your throwaway probes OUTSIDE
  the repository — a `dotnet run file.cs` that prints what it proves — are exactly what I
  want to see. They are not part of the build.

## Architecture

- `src/Domain`: all business logic. No I/O, no dependencies.
- `src/Api`: endpoints, search phase orchestrator, EF Core, task queue. Organized by feature, not by layer.
- `src/ServiceDefaults`: telemetry, health checks, resilience. Used in dev and in production.
- `src/AppHost`: Aspire, dev only.
- `src/Scraper`: Python worker. Target: it reads Google and nothing else. Today
  it still carries business rules — the page-load budget, route validity, gateway ranking.
  That is debt, not design: each one moves to .NET when it can. Never add to it.
- `src/Spa`: front end.
- `contracts/`: JSON samples of the Api ⇄ Scraper messages. Any message change updates them, along with the .NET and Python tests.

Allowed dependencies between the domain's bounded contexts, and no others:

- Common -> nothing
- Reference, Fares, TravelRules -> Common
- Search -> all

`tests/Architecture.UnitTests` will enforce this list and will be the source of truth: if it
fails, your code is wrong. It does not exist yet — until it does, the list above is
unenforced, so check it by hand and say that you did.

## Domain

- Pure functions: the domain takes data in and returns data. No repository interface, no mocks.
- Invariants are checked in constructors: an invalid object cannot exist.
- XML documentation where it carries something a reader without airline background cannot
  guess. Proportionate: `Pitch` gets one line — the legroom in centimetres — and nothing
  more. The anchor side, which decides whether a partition is searched RT/RT, RT/OW or
  OW/OW, earns the why and an example. Never documentation that repeats the member's name.
- Identifiers in English, documentation in English, vocabulary from the README glossary.

## Method

1. Before coding: a short plan — the files you will touch, the tests you will write. If
   anything in the request is open, ask instead of guessing: a question costs one round
   trip, a wrong assumption costs the whole block. Wait for my approval.
2. Tests. When the rule is known before the code — a bug fix, a defect I reported — write
   the failing test first: watching it fail is what proves the defect was real. When we are
   exploring and the rule only appears along the way, write the code and the tests, then
   MEASURE them: mutate the production code and show that a test dies. A green test that
   survives every mutation proves nothing.
3. One block at a time. A block is one subject, verified: build clean, tests green, and the
   measurement shown. Then you STOP. I read, I test, I commit, and I tell you to move on.
   Never commit, never push.
4. I model the domain. In src/Domain you propose, you do not impose. If you believe my
   model has a defect: stop and tell me — what the defect is, why it is one, the
   measurement that shows it, and the actions you would take to fix it. A finding arrives
   with its evidence, or it does not arrive.
5. Stay within the requested scope. Anything you notice elsewhere, one line at the end,
   without touching it.

## Honesty

- If you cannot find a type, method or API in the repo or the restored packages, say so. Do not make it up.
- Every claim about existing code cites file:line.
- "It works" means: you ran the command and you show the output.
- Do not modify or delete a passing test without my approval. An inconvenient test is information.
- Never use `#pragma warning disable`, `SuppressMessage`, `NoWarn`, `Skip`, `null!`.
- Protected file or permission denied: stop and explain, do not work around it.

## End of task

A condensed report on ONE subject, that I can answer without looking anything up: what
changed and where, what proves it (the command and its output), the decisions you took
without me, and what is not covered. No scattering — if you found something outside the
subject, one line at the end, not a second report.
