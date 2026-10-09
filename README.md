# NetCha

NetCha is an open-source ALTCHA-compatible CAPTCHA service composed of an
ASP.NET Core API, a React demo application and a React administration panel.

## Architecture

| Component | Local | Deployment target |
| --- | --- | --- |
| API | ASP.NET Core | `https://netcha.netisler.com` on IIS 10 |
| Demo and admin | React + TypeScript | `https://tayfuns.github.io/netcha/` |
| Persistence | In-memory for zero-setup development; PostgreSQL/Supabase-ready | Supabase PostgreSQL |
| Replay/cache | In-memory fallback; Redis-ready | Redis |

No credential is stored in this repository. Copy the supplied example
configuration files and inject secrets through environment variables.

## Repository layout

```text
Backend/                 ASP.NET Core API, domain and tests
Frontend/                React workspace containing demo and admin apps
AGENTS.md                Collaboration and engineering rules
docker-compose.yml       Optional local Redis service
```

Detailed run commands live in the README inside each application directory.
Deployment preparation is documented in [`docs/DEPLOYMENT.md`](docs/DEPLOYMENT.md).

## Local prerequisites

- .NET SDK 10 or a compatible SDK selected by the backend project
- Node.js 22 or newer and npm
- Docker (optional, for Redis)

The project is intentionally able to start without Docker or cloud credentials.

## Security

- Do not commit `.env`, local appsettings files or database credentials.
- Production secrets must be supplied through IIS environment variables or a
  dedicated secret store.
- Restrict production CORS to the deployed frontend origin.
- Rotate any credential that has previously been placed in an unencrypted note.
