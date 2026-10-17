# Catalog backend foundation

The API now defaults to `Prestige:Mode=Catalog`. It uses PostgreSQL without registering a Hangfire server, Redis cache, or Cosmos client. Supplying old Redis/Cosmos connection strings does not enable those dependencies in catalog mode. Local Demo remains available with SQLite.

This is the storage/runtime foundation for the [catalog pivot](catalog-pivot.md), not a finished public release. The core product is a social music rating/ranking platform positioned against Rate Your Music; Spotify-backed prestige is an optional layer, with more granular tracking planned after proof of concept. Catalog signup is independent of Spotify; imports still use the old Functions pipeline, and the catalog/collection model will evolve in later PostgreSQL migrations. Do not deploy the existing Functions app as part of a new catalog environment.

## Catalog accounts

An authenticated request resolves the full validated token issuer and subject through `UserIdentity` to a random, application-owned account ID. First use creates the account, identity and existing consent defaults in one transaction. Concurrent first requests resolve to the same account. Accounts start with an editable listener nickname and empty Spotify credentials; provisioning never calls Auth0 Management or Spotify.

Clients must bootstrap with `GET /users/me` and use its `id` for subsequent account-specific requests. Do not derive account IDs by splitting the Auth0 subject. The API retains the original subject for legacy authentication operations, but authorization uses only the server-resolved account identity. Client-supplied account claims cannot select another account. `/users/{id}/tokens` is unavailable in catalog mode.

`GET /users/search?query=...` searches public names and nicknames case-insensitively, with at most ten deterministically ordered results. It uses provider-compatible EF queries rather than SQL Server full-text functions. Search results contain only public profile fields; emails, setup state and credentials are excluded. This bounded substring search is intended for the initial small catalog audience; add indexed search when measured usage warrants it.

The old mobile client still assumes subject-derived IDs and needs this bootstrap change before targeting the catalog deployment. Legacy mode and Local Demo retain their existing IDs. Existing demo databases can continue using their seeded accounts without identity records. Apply the new `CatalogAccounts` PostgreSQL migration before serving catalog traffic; the baseline alone is insufficient for login.

Apply all current migrations, including `PersonalCollections`, before using the catalog rating or collection endpoints. [The collection API](collections-api.md) separates saved music, favorites and pins from listening aggregates. Catalog rating writes now save collection membership atomically, without requiring Spotify credentials or listening totals. Web favorites and pins use the new collection contract in Catalog mode; native migration and catalog population remain outstanding. [Stored catalog search](catalog-search.md) lets the web find known music without Spotify; it does not fetch missing releases.

### Web authentication

Copy `Prestige.Web/.env.example` to `.env.local` and supply the SPA's public Auth0 client ID, tenant domain, API URL and audience (matching the API configuration). Configure the SPA callback as `<web origin>/authorization` and allow that web origin and logout URL in Auth0. Enable an email/password or passwordless connection for the SPA in the Auth0 dashboard; frontend changes alone cannot enable a tenant connection. Set `VITE_PRESTIGE_MODE=Catalog` to match the API (use `Legacy` only for the old API). Browser environment variables are public: never add a client secret.

The web app requests only `openid profile email`. It loads `/users/me` before rendering authenticated routes, uses the returned account ID throughout, and isolates query caches when the login changes. A failed bootstrap shows a retry state without making account-specific requests. Completing profile edits refreshes the shared account cache. Local Demo continues to use the same bootstrap endpoint with its seeded account.

## Local PostgreSQL

Use a PostgreSQL 15+ server, or start the optional local database:

```sh
docker compose -f compose.catalog.yml up -d --wait
export ConnectionStrings__Postgres='Host=localhost;Port=54329;Database=prestige;Username=prestige;Password=local-development-only'
```

The Compose credentials are for local development only. The volume persists across restarts. No other containers are required for this foundation.

Apply migrations with the EF Core 9 CLI (the implementation was validated using `dotnet-ef` 9.0.5):

```sh
dotnet restore Prestige.Api/Prestige.Api.csproj
dotnet ef database update --project Prestige.Api --context PostgresPrestigeContext
```

The design-time factory requires `ConnectionStrings__Postgres` explicitly and does not start the application or its background services. Do not use the SQL Server context or `EnsureCreated` to initialize a PostgreSQL deployment. PostgreSQL migrations are in `Prestige.Api/Migrations/Postgres`; existing SQL Server migrations remain associated with `PrestigeContext` and are not replayed against PostgreSQL.

Configure normal Auth0 authentication and start the API:

```sh
export ASPNETCORE_ENVIRONMENT=Development
export Auth0__Domain='your-tenant.auth0.com'
export Auth0__Audience='your-api-audience'
dotnet run --project Prestige.Api --no-launch-profile --urls http://localhost:5249
```

Normal startup does not apply migrations or create tables. Apply reviewed migrations once as a deployment step before admitting traffic.

For the existing seeded demo without PostgreSQL or Auth0:

```sh
dotnet run --project Prestige.Api --launch-profile 'Local Demo'
```

## Production configuration

| Setting | Value |
| --- | --- |
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `Prestige__Mode` | `Catalog` (default) |
| `Database__Provider` | `Postgres` (default in catalog mode) |
| `ConnectionStrings__Postgres` | Secret connection string for the target database; configure TLS according to the provider. |
| `Auth0__Domain` | Auth0 tenant hostname |
| `Auth0__Audience` | API audience |
| `Cors__AllowedOrigins__0` | Exact HTTPS web origin, such as `https://music.example.com`; no trailing slash or path |

Add more CORS origins with consecutive array indices. The API uses bearer tokens and does not enable credentialed cross-origin cookies. Production startup rejects missing auth settings, missing/invalid web origins, demo authentication, and SQLite. Staging also rejects demo authentication and SQLite. `LocalDemo:Enabled` and the older `PRESTIGE_LOCAL_DEMO` flag are permitted only in `Development` or `LocalDemo` environments.

Catalog mode deliberately does not fall back to an Azure SQL connection if PostgreSQL is missing. The old SQL Server deployment can be selected explicitly with `Prestige__Mode=Legacy` and its existing SQL connection settings. This compatibility mode still has the old background jobs; it is not the new launch configuration. Disabling registration in this API does not stop already deployed Azure Functions, delete stored Hangfire jobs, or remove billing resources in an external account.

Readiness and liveness are separate:

- `GET /api/health`: process liveness; no database query.
- `GET /api/ready`: returns 200 when the rating-category table is accessible and seeded, otherwise 503. It does not validate all business flows or upstream providers. Database work has a five-second health-check timeout.
- Other mapped controllers remain protected by the existing authentication requirements, except existing explicitly anonymous endpoints that are still being retired/reviewed.

Use liveness for frequent process monitoring; readiness queries can wake an idle serverless database. Avoid constant external readiness polling if preserving database suspension is a cost requirement.

## Tests

```sh
dotnet test Prestige.Api.Tests/Prestige.Api.Tests.csproj
```

This runs the runtime/auth-configuration/CORS/readiness tests; PostgreSQL tests report an explicit skip without a server connection. To run the full suite:

```sh
export PRESTIGE_TEST_POSTGRES='Host=localhost;Port=54329;Database=postgres;Username=prestige;Password=local-development-only'
dotnet test Prestige.Api.Tests/Prestige.Api.Tests.csproj
```

The PostgreSQL test role must be able to create databases. Every integration test creates its own randomly named `prestige_test_*` database and drops only that database during cleanup. Use a dedicated local/CI server, not the production database role.

CI supplies a PostgreSQL 15 service and runs the full suite. Tests cover fresh migrations, migration-history isolation, reapplying migrations without losing data, rating/consent persistence, UTC timestamps, production PostgreSQL service resolution, authenticated routes, readiness before/after schema initialization, provider/issuer identity isolation, account ownership, and concurrent account provisioning. These are foundation tests, not yet full rating-flow or import correctness tests.

When the model changes, generate a new migration for `PostgresPrestigeContext` and rerun the real PostgreSQL tests. Do not suppress pending-model warnings or edit an already deployed baseline to avoid writing a migration.
