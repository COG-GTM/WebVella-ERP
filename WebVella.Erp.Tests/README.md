# WebVella.Erp.Tests

xUnit integration tests that boot the full ERP engine — the existing
`Next`, `SDK`, `Project`, `Crm`, `Mail`, and `TravelERP` plugins together with the
new additive modules (`Finance`, `Workflow`, and, once merged, `Inventory`,
`Procurement`, `Sales`, `HR`) — against a real PostgreSQL database and assert that:

- every module's expected entities / fields / relations were created;
- module business rules hold (Finance journals must balance, Workflow rejects
  undefined state transitions and only approves a flow once every step is approved);
- booting all plugins in one database produces **no metadata GUID or name
  collisions** and existing plugin patches still apply cleanly (regression).

## How it works

`ErpTestFixture` (shared via the `"erp"` xUnit collection, so the bootstrap runs
once) performs the same startup sequence a `WebVella.Erp.Site.*` host does:

1. sets `Npgsql.EnableLegacyTimestampBehavior`;
2. drops & recreates a disposable database;
3. initializes `ErpSettings` from an in-memory configuration;
4. opens a `DbContext`, runs `ErpService.InitializeSystemEntities()`,
   `InitializeBackgroundJobs()`, sets up AutoMapper, initializes `ErpAppContext`,
   then `InitializePlugins()` which runs each plugin's version-gated patches.

## PostgreSQL setup

The tests need a reachable PostgreSQL server. The target database (default
`erp_test`) is **dropped and recreated** on every run, so point the tests at a
throwaway server/instance.

Connection is resolved in this order:

1. `TEST_ERP_CONNECTION_STRING` — a full Npgsql connection string, or
2. built from `PGHOST` / `PGPORT` / `PGUSER` / `PGPASSWORD`
   (defaults `localhost` / `5432` / `postgres` / `postgres`), targeting the
   database named by `TEST_ERP_DATABASE` (default `erp_test`).

### Quick local run (Docker)

```bash
docker run --rm -d --name erp-test-pg -e POSTGRES_PASSWORD=postgres -p 5432:5432 postgres:14

cd <repo root>
PGHOST=localhost PGPORT=5432 PGUSER=postgres PGPASSWORD=postgres TEST_ERP_DATABASE=erp_test \
  dotnet test WebVella.Erp.Tests/WebVella.Erp.Tests.csproj -c Debug
```

Or with an explicit connection string:

```bash
TEST_ERP_CONNECTION_STRING="Server=localhost;Port=5432;User Id=postgres;Password=postgres;Database=erp_test;Pooling=true;" \
  dotnet test WebVella.Erp.Tests/WebVella.Erp.Tests.csproj
```

> Linux note: the solution references `WebVella.ERP\WebVella.Erp.csproj` (mixed
> case). On case-sensitive filesystems create a `WebVella.ERP -> WebVella.Erp`
> symlink at the repo root before building.
