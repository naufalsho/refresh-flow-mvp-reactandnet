# 🔐 Refresh Token Flow — .NET 8 Web API + React (TypeScript)

End-to-end secure implementation of **short-lived JWT access tokens** + **rotating refresh tokens** with **HttpOnly cookies**, **reuse detection**, and **proper logout**.

---

## 🧠 Big Picture — Token Flow

### 1️⃣ Login
Frontend → Backend
POST /auth/login { username, password }

**Backend:**
- ✅ Validate user (ASP.NET Identity)
- 🔐 Issue **Access Token (JWT)** — short-lived (e.g. 15 min)
- 🔐 Issue **Refresh Token (random 256-bit)** — long-lived (e.g. 7–30 days)
- 💾 Store **hashed refresh token** in DB (with metadata: expiry, IP, UA)
- 🍪 Send refresh token as `HttpOnly`, `Secure`, `SameSite=Lax`, `Path=/auth/refresh` cookie
- ⬅️ Return access token via JSON

**Frontend:**
- 🧠 Store access token **in memory** (not localStorage)
- 🍪 Refresh token cookie is stored automatically by the browser

---

### 2️⃣ Access Protected Resources
Frontend → Backend
GET /me
Authorization: Bearer <access_token>


**Backend:**
- ✅ Validate JWT (issuer, audience, lifetime, signature)
- ⬅️ 200 OK if valid  
- ❌ 401 Unauthorized if expired

---

### 3️⃣ Token Expired → Refresh Flow
When the frontend receives `401`, Axios interceptor triggers:
POST /auth/refresh
Cookie: rt=<refresh_token>

**Backend:**
- 🔍 Verify refresh token:
  - Exists in DB?
  - Not expired?
  - Not revoked?
- ✅ Valid → rotate:
  - Mark old token `revoked = true`, `replacedById = newToken.Id`
  - Issue **new refresh token** (same family)
  - Issue **new access token (JWT)**
  - 🍪 Set new refresh cookie
  - ⬅️ Return new access token JSON

- ❌ Invalid/revoked/reused → **reuse detection**:
  - Revoke entire token family (`FamilyId`)
  - Clear cookie
  - 401 → user must re-login

**Frontend:**
- 🧠 Replace in-memory access token
- 🍪 Browser automatically updates refresh cookie
- 🔁 Retry failed request once using new token

---

### 4️⃣ Logout
Frontend → Backend
POST /auth/logout

**Backend:**
- 🔍 Find refresh token from cookie
- 🔥 Mark as revoked
- 🍪 Clear cookie (expired)
- ⬅️ 200 OK

**Frontend:**
- 🧹 Clear in-memory access token
- ✅ Session truly terminated

---

### 5️⃣ Reuse Detection (Compromise Scenario)
If an **old** refresh token (already rotated) is reused:
- Server detects `revoked == true`
- Revoke all tokens with same `FamilyId`
- Remove cookie
- Force logout all sessions for that device/user

---

## 🔄 Summary Diagram
Login
└── Issue JWT + Refresh Token
├─ JWT → short life (15m)
└─ Refresh → long life (7–30d, stored in DB)

Normal API Call
├─ Valid JWT → OK
└─ Expired JWT → 401 → Refresh flow

Refresh Flow
├─ Cookie HttpOnly (/auth/refresh)
├─ Validate & Rotate
├─ Return new JWT + set new cookie
└─ Old token revoked

Logout
├─ Revoke active refresh token
└─ Clear cookie

Reuse Detection
├─ If revoked token used → revoke all family tokens
└─ Force logout on all devices (compromise response)

---

## ⚙️ Security Principles

| Component | Policy |
|------------|---------|
| **Access Token (JWT)** | Short-lived (15m), stateless, cannot be revoked manually |
| **Refresh Token** | Long-lived (7–30d), random 256-bit, hashed in DB, rotatable & revocable |
| **Cookie Settings** | `HttpOnly`, `Secure`, `SameSite=Lax`, `Path=/auth/refresh` |
| **Reuse Detection** | If revoked token reused → revoke all in the same `FamilyId` |
| **Rotation** | Each refresh issues new token; previous becomes invalid immediately |
| **Logout** | Revoke active refresh token and clear cookie |
| **Device Sessions** | Track `FamilyId`, IP, and UserAgent for audit or per-device revoke |
| **CORS** | Whitelist only front-end origin, allow credentials |
| **Rate Limit** | Throttle `/auth/login` and `/auth/refresh` endpoints |
| **Storage** | Never store JWT in `localStorage`; only in memory (volatile, XSS-safe) |

---

## 🧩 Project Structure

### Backend (.NET 8 Web API)
- ASP.NET Core Identity + EF Core (SQL Server)
- JWT Authentication + Cookie-based refresh
- Endpoints:
  - `POST /auth/login`
  - `POST /auth/refresh`
  - `POST /auth/logout`
  - `GET /me`
- `RefreshTokens` table with family, expiry, IP, and reuse detection

### Frontend (React + TypeScript + Vite)
- Axios instance with `withCredentials: true`
- Interceptor for 401 → `/auth/refresh`
- `AuthContext` to store JWT in memory
- `ProtectedRoute` guard
- Simple pages: `Login`, `Profile`, `Logout`

---

## 🧪 Typical Flow Summary
User logs in → receives access + refresh token (cookie)
→ Uses JWT in API calls
→ When JWT expires → automatic refresh (cookie)
→ Refresh token rotates (old invalidated)
→ If reused → revoke family (compromise)
→ Logout → revoke + clear cookie

---

## 🔍 Reference Security Guidelines
- [OWASP Cheat Sheet — Authentication](https://cheatsheetseries.owasp.org/cheatsheets/Authentication_Cheat_Sheet.html)
- [OWASP — Token-based Authentication](https://owasp.org/www-community/Token-based_Authentication)
- [RFC 6749 — OAuth 2.0](https://datatracker.ietf.org/doc/html/rfc6749)
- [RFC 6819 — OAuth 2.0 Threat Model](https://datatracker.ietf.org/doc/html/rfc6819)

---

## ✅ Summary

- Safe: HttpOnly cookie for refresh + JWT in-memory
- Scalable: stateless access token, database-backed refresh tokens
- Secure: rotation, reuse detection, revocation
- UX-friendly: seamless token renewal, one retry only
- Ready for production with .NET 8 + React 18 (TypeScript)

---

### 🧰 Credits
Built with ❤️ by [naufalsho] — .NET Developer  
Design inspired by OWASP recommendations and modern security best practices.
