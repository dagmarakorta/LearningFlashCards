# LearningFlashCards

A cross‑platform flashcard app built with .NET MAUI, focusing on a simple, fast study flow and synchronized data across services.

> Status: **Work in progress** — core flows are being built and iterated.

## Highlights

- **Decks & cards**: Create decks and add cards with front/back text (emoji‑friendly).
- **Study mode**: Flip cards, step through a deck, and track your place.
- **Multi‑project architecture**: Clean separation between UI, core domain, API, and infrastructure.
- **Sync‑ready backend**: Repository + sync primitives in place for future offline/online flows.

## Tech Stack

- **Frontend**: .NET MAUI (Android / iOS / macOS / Windows)
- **Backend**: ASP.NET Core API
- **Data**: Entity Framework Core + SQLite
- **Tests**: xUnit‑based test projects

## Solution Layout

```
src/
  LearningFlashCards.Maui/          # MAUI UI app
  LearningFlashCards.Api/           # ASP.NET Core API
  LearningFlashCards.Core/          # Domain entities + contracts
  LearningFlashCards.Infrastructure/# EF Core + repositories
tests/
  LearningFlashCards.*.Tests/       # Unit & integration tests
```

## Current App Flow

1. Create a deck
2. Add cards (front/back)
3. Open a deck and tap **Study** to flip cards and advance

## Development Notes

- The MAUI app and API are separate projects in the solution.
- Database migrations live under `src/LearningFlashCards.Infrastructure/Persistence/Migrations`.
- The API uses a local SQLite database for now.

## Roadmap (short‑term)

- Rich card content (images, formatting)
- Smarter study session logic
- Tagging and filtering
- Polished UI/UX pass

---

If you’re exploring the repo, expect frequent changes while core features settle.
