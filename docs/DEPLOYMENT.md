# Deployment guide

## Backend: IIS 10

Target host: `https://netcha.netisler.com`.

1. Install the ASP.NET Core Hosting Bundle matching the project's .NET major
   version and restart IIS.
2. Publish the API:

   ```bash
   dotnet publish Backend/src/NetCha.Api/NetCha.Api.csproj \
     --configuration Release \
     --output Backend/artifacts/publish
   ```

3. Create an IIS application pool with **No Managed Code**, create a site bound
   to the hostname, and point it at the publish directory.
4. Configure HTTPS before allowing public traffic.
5. Add environment variables at the server or application-pool level. At
   minimum, production requires:

   ```text
   ASPNETCORE_ENVIRONMENT=Production
   Captcha__HmacKey=<random value of at least 32 bytes>
   Admin__ApiKey=<independent random admin key>
   Cors__AllowedOrigins__0=https://tayfuns.github.io
   ```

6. Add `Persistence__ConnectionString` for Supabase PostgreSQL and
   `Redis__ConnectionString` for Redis. Use a least-privilege database account.
7. Verify `/health`, `/openapi/v1.json`, challenge generation, verification,
   replay rejection and admin authentication before switching DNS traffic.

Secrets must not be placed in `appsettings.json`, the frontend bundle, workflow
files or repository settings visible to untrusted users.

## Frontend: GitHub Pages

The Pages workflow builds with `VITE_API_URL=https://netcha.netisler.com` and
publishes:

- Demo: `/netcha/`
- Administration: `/netcha/admin/`

In repository settings, select **GitHub Actions** as the Pages source. The API
must allow `https://tayfuns.github.io` in CORS and serve a trusted HTTPS
certificate.

## Release checklist

- Backend and frontend CI passes.
- Production secrets are generated independently and stored outside Git.
- Database migration permissions are removed or `AutoMigrate` is disabled after
  controlled schema setup.
- Redis is reachable from the API host when more than one API instance is used.
- Only the deployed GitHub Pages origin is present in production CORS.
- IIS request limits, logging retention, TLS and reverse-proxy forwarded headers
  are reviewed.
- A valid challenge succeeds once; the same payload fails on replay.
