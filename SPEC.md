# Phishing-Resistant Posture Dashboard — Specification

## 1. Vision: The Entra Verified Account

Microsoft Entra is moving toward a future where every user identity is anchored in **cryptographic proof** rather than shared secrets. The concept we call the **Entra Verified Account** represents a fully phishing-resistant identity lifecycle:

```
┌──────────────────────────────────────────────────────────────────────────┐
│                    Entra Verified Account Lifecycle                      │
│                                                                          │
│   ┌─────────────┐     ┌───────────────┐     ┌────────────────────────┐  │
│   │  ONBOARDING │     │  DAILY ACCESS │     │  ACCOUNT RECOVERY      │  │
│   │             │     │               │     │                        │  │
│   │ Verified ID │────▶│   Passkeys    │────▶│  IDV-backed recovery   │  │
│   │ + IDV proof │     │  (FIDO2 /     │     │  via Verified ID +     │  │
│   │             │     │   platform)   │     │  identity verification │  │
│   └─────────────┘     └───────────────┘     └────────────────────────┘  │
│         ▲                                              │                │
│         └──────────────────────────────────────────────┘                │
│                    Phishing-resistant end-to-end                         │
└──────────────────────────────────────────────────────────────────────────┘
```

**Onboarding** — A user proves their real-world identity through Identity Verification (IDV) and is issued a Verified ID credential. This establishes a high-assurance, cryptographically bound identity from day one.

**Daily access** — The user authenticates with a **passkey** (FIDO2 security key, platform credential, or synced passkey). No passwords. No SMS codes. No phishable factor ever touches the wire.

**Account recovery** — When a user loses all credentials, they recover through **IDV-backed verification** powered by the Verified ID account recovery feature. They re-prove their identity and obtain a new phishing-resistant credential — never falling back to a helpdesk call, a shared secret, or an SMS code.

This is the vision we are bringing to customers: **an identity lifecycle that is phishing-resistant at every stage** — enrollment, daily use, and recovery.

---

## 2. The Problem

### 2.1 Phishing remains the #1 attack vector

Despite years of MFA deployment, **phishing attacks continue to succeed** because:

- **Passwords are still the default.** Most tenants have large populations of users who authenticate with nothing more than a password, or who have MFA registered but use phishable second factors (SMS, voice call, simple push).
- **Legacy MFA is phishable.** Adversary-in-the-middle (AiTM) toolkits like EvilGinx intercept SMS codes, voice OTPs, and even traditional push approvals in real time. MFA that was "good enough" five years ago is now routinely bypassed.
- **Privileged accounts are under-protected.** Global Administrators and other high-privilege roles often rely on the same phishable methods as the general population, creating catastrophic blast-radius risk.
- **Recovery flows reset all progress.** Even when a user has strong daily credentials, account recovery typically falls back to helpdesk-driven password resets or SMS-based self-service — reopening the phishing window at the moment the account is most vulnerable.

### 2.2 Customers lack visibility

Organisations today struggle to answer fundamental questions about their security posture:

| Question | Why it's hard today |
|----------|-------------------|
| **What percentage of my users are truly phishing-resistant?** | Entra admin center shows MFA registration rates, but does not break out phishing-resistant methods (FIDO2, passkeys) from legacy MFA (SMS, push). Admins cannot see the gap. |
| **Are my Global Admins protected?** | There is no single view that correlates role membership with per-user authentication method inventory. Admins must cross-reference multiple blades manually. |
| **Am I enforcing phishing-resistant auth via policy?** | Conditional Access policies may require "MFA" but not specifically *phishing-resistant* MFA. Determining whether the right auth-strength policies exist, which users/apps they cover, and what exclusions weaken them requires reading multiple policy objects. |
| **What does my auth method distribution actually look like?** | Registration counts per method exist in Graph reports, but they are not contextualised against the tenant's enabled user base, making it difficult to assess real coverage. |
| **What are users actually signing in with today?** | Sign-in logs contain per-event method data, but there is no aggregated view that shows which methods are being *used* (not just registered) and at what success/failure rate. |
| **Where should I focus my rollout effort?** | Without a maturity model or gap analysis, security teams cannot prioritise: should they push passwordless adoption, or focus on upgrading existing passwordless users to phishing-resistant? |

### 2.3 The cost of inaction

Without clear posture visibility, organisations:

- **Cannot measure progress** toward Zero Trust authentication goals.
- **Over-estimate their protection** by conflating "MFA enabled" with "phishing-resistant."
- **Under-invest in passkey rollout** because they cannot quantify the at-risk population.
- **Leave recovery flows as a backdoor** — a perfectly passkey-protected user can be socially engineered through a weak recovery path.

---

## 3. Solution: The Phishing-Resistant Posture Dashboard

This dashboard gives IT security teams and identity administrators a **single, real-time view** of their tenant's phishing-resistant posture — and a clear path to close the gaps.

### 3.1 Posture overview at a glance

#### Journey to Phishing-Resistant Authentication

A maturity banner that shows the organisation's overall position on the journey from passwords to passkeys:

| Metric | What it measures | Why it matters |
|--------|-----------------|----------------|
| **Phishing-Resistant %** | Users with FIDO2, Authenticator passkey, synced passkey, or device-bound passkey registered | The headline number — how much of the workforce is protected against AiTM phishing |
| **Passwordless %** | Users with any passwordless method (includes phishing-resistant + WHfB + Authenticator passwordless) | Shows how much of the passwordless journey is complete, and how large the "upgrade to PR" gap is |
| **MFA Capable %** | Users with any MFA method registered | The broadest protection baseline; the gap to passwordless shows how many users still rely on legacy MFA |
| **Maturity Rating** | Beginning → Early → Progressing → Advanced (based on PR %) | Executive-friendly label for board reporting and goal-setting |

All percentages are measured against **enabled users** (not total directory objects), giving an actionable denominator.

#### Tenant Security Status — Mutually Exclusive Tiers

Users are classified into exactly one tier based on their **highest** registered authentication strength:

```
 Phishing Resistant  ▸  Passwordless (non-PR)  ▸  MFA Only  ▸  Password Only
       (best)                                                      (worst)
```

Each user is counted **once**. The four tiers sum to the total enabled user count, eliminating the double-counting problem that plagues raw registration reports.

### 3.2 Privileged account protection

#### Global Administrator Security Status

A dedicated card that answers: *"Are my most powerful accounts protected?"*

- Lists every Global Administrator with their **account status** and **registered authentication methods**.
- Classifies each admin's strength: **Phishing Resistant**, **Passwordless**, **MFA**, or **Password Only**.
- Surfaces critical alerts when any GA lacks MFA or phishing-resistant credentials.
- Provides an overall rating (Excellent → Critical) so security teams can prioritise remediation of the highest-risk accounts first.

### 3.3 Policy enforcement analysis

#### Phishing-Resistant Enforcement via Conditional Access

Having users *capable* of phishing-resistant auth is necessary but not sufficient — the tenant must also *require* it. This section analyses all Conditional Access policies and answers:

| Insight | Detail |
|---------|--------|
| **Enforcing policies** | How many CA policies require phishing-resistant authentication strength and are in enforcing mode |
| **Report-only policies** | Policies configured but not yet enforcing — indicating planned but incomplete rollouts |
| **User coverage** | Whether policies target all users or specific groups, and how many groups are covered |
| **App coverage** | Whether policies apply to all cloud apps or only selected applications |
| **Exclusions** | Groups or apps excluded from enforcement — potential gaps that attackers can exploit |
| **Overall verdict** | Fully Enforced / Partial Coverage / Not Enforced — a single badge that tells the admin where they stand |

### 3.4 Authentication method intelligence

#### Security Tier Distribution Chart

A 7-tier doughnut chart that breaks down the phishing-resistant population into granular sub-categories:

1. **FIDO2 Hardware Key** — external security keys (YubiKey, Feitian, etc.)
2. **Authenticator Passkey** — device-bound passkey in Microsoft Authenticator
3. **Synced Passkey** — cross-device passkeys synced via platform (iCloud, Google Password Manager)
4. **Device-Bound Passkey** — other device-bound passkeys (Windows Hello, platform credentials)
5. **Passwordless (non-PR)** — Authenticator passwordless phone sign-in, WHfB (not FIDO2)
6. **MFA Only** — traditional MFA (Authenticator push, TOTP, SMS, voice)
7. **Password Only** — no MFA registered

This lets security teams understand not just *how many* users are phishing-resistant, but *which mechanisms* they are using — critical for hardware procurement, support planning, and policy decisions.

#### Registration Breakdown Table

A per-method table showing:

- Method name and classification badge (Phishing Resistant / Passwordless / Legacy)
- User registration count
- Percentage of enabled users
- Visual coverage bar

This answers: *"Which specific methods are adopted, and how does each compare to our total user base?"*

#### Last 24-Hour Sign-In Activity

A horizontal stacked bar chart showing **actual sign-in events** (not just registrations) grouped by authentication method, split into success and failure counts. This reveals:

- Which methods users are **actually using** today (registration ≠ usage).
- Failure hotspots that may indicate configuration issues, user friction, or attack attempts.
- Whether legacy methods still dominate daily sign-ins despite passkey registrations.

### 3.5 Verified ID and account recovery integration

The dashboard is built on top of the **Entra Verified ID Admin APIs**, providing a unified view that connects credential management with posture insights:

| Capability | How it supports the vision |
|-----------|---------------------------|
| **Authority management** | View and configure Verified ID authorities that anchor the trust chain for credential issuance and recovery |
| **Contract management** | Define verifiable credential types used for onboarding (identity proofing) and recovery (re-verification) |
| **Credential operations** | Search, inspect, and revoke issued credentials — critical for managing the lifecycle of identity-verified accounts |
| **FaceCheck / IDV status** | Monitor whether biometric identity verification is enabled for the tenant — the foundation of IDV-backed onboarding and recovery |
| **Activity and audit logs** | Track issuance and presentation transactions to understand Verified ID adoption alongside passkey deployment |

Together, these capabilities let administrators see whether the **full verified account lifecycle** is operational: IDV onboarding → daily passkey use → IDV-backed recovery.

### 3.6 Configuration management

The dashboard provides read/write access to the authentication method policies that underpin the phishing-resistant rollout:

- **FIDO2 policy** — enable/disable, self-service registration, attestation enforcement, AAGUID allow/block lists, target groups
- **Microsoft Authenticator policy** — enable/disable, feature settings (app context, location context, companion app), target groups
- **Policy overview** — all authentication method states at a glance

This means administrators can **identify a gap in the posture dashboard and immediately take action** in the configuration view — without switching tools.

---

## 4. Data Sources and Architecture

```
┌──────────────────────────────────────────────────────────────┐
│              Phishing-Resistant Posture Dashboard             │
│                  ASP.NET Core 8.0 MVC                        │
├──────────────────────────────────────────────────────────────┤
│                                                              │
│  Microsoft Graph API                                         │
│  ├─ reports/authenticationMethods/usersRegisteredByFeature   │
│  │  → MFA, passwordless, SSPR aggregate counts               │
│  ├─ reports/authenticationMethods/usersRegisteredByMethod    │
│  │  → Per-method registration counts (FIDO2, passkeys, etc) │
│  ├─ auditLogs/signIns                                        │
│  │  → Last 24h sign-in events by method + success/failure    │
│  ├─ roleManagement/directory/roleAssignments                 │
│  │  → Global Admin role membership                           │
│  ├─ users/{id}/authentication/methods                        │
│  │  → Per-admin auth method inventory                        │
│  ├─ identity/conditionalAccess/policies                      │
│  │  → CA policy analysis for PR enforcement                  │
│  └─ policies/authenticationStrengthPolicies                  │
│     → Auth strength policy definitions                       │
│                                                              │
│  Verified ID Admin API                                       │
│  ├─ Authorities, Contracts, Credentials                      │
│  ├─ Transactions (issuance & presentation)                   │
│  └─ FaceCheck / IDV configuration                            │
│                                                              │
│  Azure Resource Manager                                      │
│  └─ FaceCheck billing/configuration                          │
│                                                              │
├──────────────────────────────────────────────────────────────┤
│  Caching: IMemoryCache                                       │
│  ├─ Registration data: 30 min TTL                            │
│  ├─ Sign-in data: 15 min TTL                                 │
│  ├─ Tenant count: 60 min TTL                                 │
│  └─ Force-refresh available for on-demand cache invalidation │
│                                                              │
│  Performance: All posture data uses pre-aggregated Graph     │
│  endpoints. No per-user iteration. Safe for 1M+ user         │
│  tenants. Only Global Admin detail (small population)        │
│  requires per-user queries.                                  │
└──────────────────────────────────────────────────────────────┘
```

---

## 5. Key User Scenarios

### Scenario 1: "How exposed are we to phishing?"

**Persona:** CISO preparing for a board security review.

**Flow:** Open the Activity dashboard → read the **Journey Progress** banner → see that 15% of users are phishing-resistant (rated "Beginning") → note that 60% are MFA-only (phishable) and 25% are password-only → use these numbers to justify a passkey rollout budget.

**Insight delivered:** A single percentage and maturity label that communicates organisational risk in business terms.

---

### Scenario 2: "Are our admins safe?"

**Persona:** Identity team lead responding to an incident.

**Flow:** Open Activity dashboard → scroll to **Global Administrator Security Status** → see that 2 of 8 Global Admins are "MFA Only" and 1 is "Password Only" → the card surfaces a red critical alert → immediately identify the at-risk admins by name → navigate to Configuration to ensure FIDO2 is enabled and provision hardware keys.

**Insight delivered:** Per-admin auth method inventory with strength classification and actionable alerts.

---

### Scenario 3: "Are we actually enforcing phishing resistance?"

**Persona:** Security architect validating Zero Trust posture.

**Flow:** Open Activity dashboard → review the **Phishing-Resistant Enforcement** card → see 1 enforcing policy that targets 3 groups but not all users, and 2 report-only policies → badge reads "Partial Coverage" → notice 2 app exclusions → determine that CA policy changes are needed to close gaps before the next compliance audit.

**Insight delivered:** A policy-level gap analysis that distinguishes intent (report-only) from enforcement, and highlights exclusions.

---

### Scenario 4: "Where should we focus our rollout?"

**Persona:** Identity rollout program manager.

**Flow:** Open Activity dashboard → review the **Security Tier Distribution** doughnut → see that most phishing-resistant users are on FIDO2 hardware keys, with almost no synced passkeys → check the **Registration Breakdown** table to see that Authenticator passkey adoption is low → decide to run an Authenticator passkey enrollment campaign targeting mobile-first user populations rather than purchasing more hardware keys.

**Insight delivered:** Granular method-level adoption data that informs rollout strategy.

---

### Scenario 5: "Is our verified account lifecycle operational?"

**Persona:** Identity architect implementing the Entra Verified Account vision.

**Flow:** Open the Verified ID sections → confirm authorities are configured and DID documents are valid → verify that onboarding contracts (IDV-based) and recovery contracts are active → check FaceCheck is enabled → return to Activity dashboard to correlate passkey adoption with Verified ID issuance volume → confirm the end-to-end lifecycle is functioning.

**Insight delivered:** Unified view across Verified ID (onboarding + recovery) and authentication methods (daily use).

---

## 6. Success Metrics

The dashboard is successful when customers can:

| Outcome | Measurable via |
|---------|---------------|
| **Quantify phishing risk** | Phishing-Resistant % vs. total enabled users |
| **Identify unprotected privileged accounts** | Global Admin Security Status rating and per-admin detail |
| **Validate policy enforcement** | Enforcement verdict badge and coverage metrics |
| **Track rollout progress over time** | Maturity rating progression (Beginning → Advanced) |
| **Understand method distribution** | 7-tier doughnut and per-method registration table |
| **Correlate registration with usage** | Registration data vs. last-24h sign-in activity |
| **Operate the full verified account lifecycle** | Verified ID authority, contract, and FaceCheck status |

---

## 7. Scope and Limitations

- **Sample / exploratory project** — not an official Microsoft product; not intended for production use without further hardening.
- **Delegated permissions** — the dashboard operates under the signed-in user's permissions (no application-level daemon). The user must have appropriate Entra ID roles (Authentication Policy Administrator recommended).
- **Graph data freshness** — aggregated registration reports from Microsoft Graph may be up to 24 hours old. Sign-in data reflects the most recent 200 events within the last 24 hours.
- **No historical trending** — the dashboard shows current-state posture; it does not persist historical snapshots for trend analysis (future enhancement opportunity).

---

## 8. Future Considerations

| Area | Potential enhancement |
|------|----------------------|
| **Historical trending** | Persist daily snapshots to show posture improvement over weeks/months |
| **Account recovery metrics** | Surface Verified ID account recovery transaction volume alongside daily passkey usage to show full lifecycle health |
| **IDV onboarding funnel** | Track how many users have completed IDV-based onboarding vs. legacy provisioning |
| **Risk scoring** | Combine auth method strength, CA policy coverage, and sign-in anomaly data into a composite risk score per user or group |
| **Automated remediation** | One-click action to assign temporary access passes, trigger registration campaigns, or create CA policies directly from gap analysis |
| **Multi-tenant view** | Aggregate posture across multiple tenants for managed service providers or large enterprises |
