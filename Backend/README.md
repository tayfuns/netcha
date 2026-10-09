# NetCha Backend

ASP.NET Core 10 Web API implementing an ALTCHA-compatible proof-of-work challenge, expiry and one-time-use protection. Local development defaults to in-process memory. Redis and PostgreSQL/Supabase are enabled only when connection strings are supplied.

## Run locally

```bash
dotnet restore NetCha.slnx
dotnet run --project src/NetCha.Api
```

No checked-in secret is used. In `Development`, an ephemeral HMAC key is generated at startup; all outstanding challenges become invalid after a restart. Outside `Development` or `Testing`, `Captcha__HmacKey` is mandatory and must be at least 32 UTF-8 bytes. Copy `.env.example` into your secret manager or shell environment; the application does not load `.env` files itself.

OpenAPI is available at `/openapi/v1.json` and liveness at `/health`.

## Public API

- `GET /api/v1/captcha/challenge` returns `{ algorithm, challenge, salt, signature }` for the ALTCHA widget.
- `POST /api/v1/captcha/verify` accepts `{ "payload": "<base64 ALTCHA payload>" }`. A valid payload succeeds once; reuse returns `challenge_replayed`.

Challenge and verify calls are limited per client IP. `ForwardedHeaders` support is enabled for IIS/reverse proxy deployments; configure trusted proxies/networks in the hosting environment.

## Admin API

Set `Admin__ApiKey`, then send it in `X-Admin-Key`. If the key is absent, admin routes return 503.

- `GET /api/v1/admin/stats`
- `GET /api/v1/admin/audit?limit=100` (maximum 500)
- `GET /api/v1/admin/settings` (only non-secret effective CAPTCHA settings)

## Optional infrastructure

Set `Redis__ConnectionString` for distributed, atomic replay protection. If Redis fails and `Redis__FallbackToMemory=true`, a per-instance fallback is used; for multi-instance production deployments, availability of Redis is strongly recommended.

Set `Persistence__ConnectionString` to a Supabase/PostgreSQL connection string. With `Persistence__AutoMigrate=true`, the service creates `captcha_audit` and its timestamp index. Use a least-privilege database role and disable auto-migration after applying DDL in controlled production deployments.

Typical Supabase connection strings require TLS; retain the provider's `SSL Mode=Require` setting. Never commit database credentials.

## Test

```bash
dotnet test NetCha.slnx
```
