# NetCha agent guide

## Product goal

Deliver a self-hostable ALTCHA-based CAPTCHA service with an ASP.NET Core API,
React demo and React administration panel. Local development must work before
deployment is attempted.

## Responsibilities

- Product Owner: owns scope, integration, acceptance tests and release readiness.
- Backend Developer: owns everything under `Backend/`.
- Frontend Developer: owns everything under `Frontend/`.

## Engineering rules

- Never commit credentials, connection strings, private keys or service-role keys.
- Keep local configuration in ignored files or environment variables and provide
  redacted `.example` files.
- Keep the public CAPTCHA verification path independent from Supabase availability
  for local development, while preserving production persistence adapters.
- A change is complete only after relevant builds and tests pass.
- Frontend deployment targets GitHub Pages at `/netcha/`; the API deployment target
  is `https://netcha.netisler.com` on IIS 10.
