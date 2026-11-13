# JwtAuthExample

A small ASP.NET Core Web API built to learn, from the inside, **how JWT authentication and refresh tokens work**.

The project is intentionally simple: no database, no external identity provider, and no production-grade secret management. Everything that would normally be infrastructure is simulated in memory so the authentication flow stays easy to read and reason about.

> **Purpose:** a personal reference for the concepts and a portfolio piece showing how I build and reason about authentication in .NET.

## What this project demonstrates

- Issuing a signed JWT after validating user credentials.
- Protecting endpoints with `[Authorize]` and role-based authorization.
- Reading the authenticated user's claims (`sub`, `name`, `role`).
- A refresh-token flow with rotation.
- Storing refresh tokens in `HttpOnly`, `Secure`, `SameSite=Strict` cookies.
- Keeping access tokens short-lived and refresh tokens longer-lived.
- Clear separation between simulated infrastructure and real application code.

## Concepts learned

### Authentication vs. authorization

Authentication answers *who are you?* (login -> token). Authorization answers *what are you allowed to do?* (`[Authorize]`, roles -> 403).

### Anatomy of a JWT

`header.payload.signature` — base64url-encoded. The payload is **not encrypted**, only signed, so never put secrets in it.

| Claim | Meaning |
|---|---|
| `sub` | subject / user ID |
| `name` / `nameidentifier` | username / user ID |
| `role` | role used by `[Authorize(Roles = ...)]` |
| `exp` | expiration (UTC) |
| `iss` / `aud` | issuer and intended audience |

### Signing and validation

- Tokens are signed with HMAC-SHA256 (`HmacSha256`) using a symmetric key.
- On every request the JWT bearer middleware validates the signature, issuer, audience, and lifetime (`ClockSkew = TimeSpan.Zero`).
- Tampering with the payload invalidates the signature.

### ASP.NET Core middleware pipeline

`UseAuthentication()` must run before `UseAuthorization()`: first the token is validated and `HttpContext.User` is populated, then authorization policies are evaluated.

### 401 vs. 403

- `401 Unauthorized`: no token or an invalid token.
- `403 Forbidden`: a valid token but insufficient permissions (e.g. `user` calling an admin-only endpoint).

### Refresh tokens

- Access tokens are short-lived (60 minutes).
- Refresh tokens are long-lived (7 days), random (32 bytes), and stored server-side (simulated with an in-memory dictionary).
- On refresh: validate that the token exists and is not expired -> rotate it (invalidate the old one) -> issue a new access token and refresh token.
- Rotation limits replay attacks: a stolen, already-used refresh token stops working.

### HttpOnly cookies

The refresh token is sent as a cookie with:

- `HttpOnly` -> JavaScript cannot read it (mitigates XSS token theft).
- `Secure` -> only sent over HTTPS.
- `SameSite=Strict` -> not sent on cross-site requests (mitigates CSRF).
- `Path=/api/auth` -> scoped to the auth endpoints.

The access token is returned in the response body and kept by the client in memory.

### Simulated infrastructure

Parts of the code marked `SIMULATED` are stand-ins for real systems:

- Users table -> hardcoded list.
- Refresh-token table -> `ConcurrentDictionary`.
- Secret store -> hardcoded signing key.
- Auth server / identity provider -> local `TokenService`.

There are no external API or LLM calls.

## Authentication flow

```text
Client                              API
  |  POST /api/auth/login           |
  |  { username, password } ------->|  validate credentials (simulated DB)
  |                                 |  create access JWT (60 min)
  |                                 |  create refresh token (7 days, stored)
  | <-- 200 { token }               |
  |     Set-Cookie: refreshToken (HttpOnly; Secure; SameSite=Strict)
  |                                 |
  |  GET /api/values                |
  |  Authorization: Bearer <jwt> -->|  validate JWT -> 200
  |                                 |
  |  POST /api/auth/refresh         |
  |  Cookie: refreshToken=<token> ->|  validate + rotate -> new access token
  | <-- 200 { token }               |
  |     Set-Cookie: refreshToken (rotated)
```

## Endpoints

| Method | Route | Auth | Description |
|---|---|---|---|
| POST | `/api/auth/login` | anonymous | Validate credentials, return an access token, set the refresh cookie |
| POST | `/api/auth/refresh` | refresh cookie | Validate + rotate the refresh token, return a new access token |
| GET | `/api/values` | any authenticated user | Returns the current user and roles |
| GET | `/api/values/admin` | `Admin` role | Admin-only example |

Demo users (exercise only):

| Username | Password | Role |
|---|---|---|
| `admin` | `Admin123!` | `Admin` |
| `user` | `User123!` | `User` |

## Getting started

Prerequisite: .NET 10 SDK.

```bash
git clone <repo-url>
cd JwtAuthExample
dotnet run --launch-profile https
```

- HTTPS: `https://localhost:7191`
- HTTP: `http://localhost:5291`

Use the **HTTPS** profile to test the `Secure` refresh cookie.

Open `JwtAuthExample.http` in Visual Studio or VS Code (REST Client) and run the requests in order. The login request captures the JWT and the protected requests reuse it through `{{login.response.body.$.token}}`.

## Project structure

```text
Controllers/
  AuthController.cs     login + refresh; simulated users/refresh-token DB
  ValuesController.cs   protected endpoints ([Authorize], roles)
Models/
  JwtSettings.cs        signing key, issuer, audience, lifetimes
  LoginRequest.cs       login payload
Services/
  TokenService.cs       builds and signs JWTs
Program.cs              JWT bearer validation + middleware pipeline
JwtAuthExample.http     end-to-end request walkthrough
```

## Security notes (why this is not production-ready)

This is a learning project. Before production you would:

- Load the signing key from a secret manager (never source control) and rotate it.
- Persist users and refresh tokens in a database or Redis, hashing stored tokens where appropriate.
- Keep the access token in client memory, never in `localStorage`.
- Enforce HTTPS everywhere and add rate limiting, account lockout, auditing, and token revocation.
- Consider an external identity provider (Entra ID, IdentityServer, Auth0) instead of rolling your own.
