# Project 31: Zero-Trust Security, Mutual TLS (mTLS) Handshakes & Attribute-Based Access Control (ABAC)

> **Quick-Commerce Domain:** KiranaHub Cryptographic Service Identity & Customer PII Privacy Engine  
> **Tech Stack:** C# 13, .NET 10, Zero-Trust Architecture, OAuth2 & OpenID Connect (OIDC) Asymmetric Tokens (RS256 RSA-2048), Mutual TLS (mTLS) Peer X.509 Handshakes, Dynamic Attribute-Based Access Control (ABAC), Contextual Data Masking.

---

## 1. Iss Project Ko Scratch Se Kaise Padhein? (Roadmap)

Legacy microservice systems me internal network ko "trusted" maana jata tha ("yeh API internal VPC ya Kubernetes cluster ke andar hai, toh isse kisi auth ya TLS encryption ki zaroorat nahi hai"). Lekin modern cloud security me **Perimeter Security is Dead**. Agar attacker kisi ek low-security container (jaise image resizer) me ghus jaye, toh wo poore internal network ke plaintext HTTP traffic ko sniff kar sakta hai aur databases ko query kar sakta hai.

**Zero-Trust Principle** ka matlab hai: *"Never Trust, Always Verify"*. Chahe request cluster ke andar se hi kyu na aa rahi ho, har call me do cheezein verify hongi:
1. **Machine Identity**: Mutual TLS (mTLS) cryptographic handshake.
2. **User Identity & Scope**: Asymmetric RS256 JWT access token.
3. **Contextual Permission**: ABAC policy evaluation.

Aapko yeh code iss sequence me padhna hai:
1. **`Identity/OAuth2TokenServer.cs`**:
   - Dekhiye ki asymmetric RS256 token signing kaise kaam karti hai:
     - Private key se Authorization Server token sign karta hai.
     - Resource microservices sirf Public Key se token verify karti hain bina centralized auth server ko bar-bar hit kiye.
2. **`MutualTls/TlsCertificateValidator.cs`**:
   - Samajhiye Microservice-to-Microservice mTLS cryptographic validation:
     - Both client and server present X.509 digital certificates.
     - Root CA trust chain, expiry dates, aur service name whitelist (`CN=checkout-api`) inspect hoti hai.
3. **`Authorization/AttributeBasedAccessControl.cs`**:
   - Dekhiye why Role-Based Access Control (RBAC) fails and **ABAC** wins:
     - Ek delivery rider ko poore shahar ke customer addresses dekhne ki permission nahi honi chahiye.
     - ABAC policy dynamic context check karti hai: *Rider tabhi customer phone aur address dekh sakta hai jab order actively `Dispatched` state me ho, usi dark-store ka ho, aur 30-minute delivery window ke andar ho*.
     - Order deliver hone ke baad phone number automatically mask ho jata hai (`+91-XXXXX-43210`).
4. **`Program.cs`**:
   - Automated security test suite jo RS256 token validation, mTLS rogue certificate rejection, aur ABAC dynamic data masking ko verify karta hai.

---

## 2. Kya Pehle Bna, Kaise Bna? (Foundations)

### Foundation: NIST SP 800-207 Zero Trust Architecture
- Traditional castle-and-moat security maanti thi ki boundary ke andar sab safe hai.
- Zero-Trust Architecture me 3 core pillars hote hain:
  1. **All communication is secured**: Chahe pods ek hi node par hon, unka traffic encrypted aur mutually authenticated (mTLS) hona chahiye.
  2. **Access is granted on a per-session basis**: Long-lived permanent privileges nahi hote; contextual short-lived tokens use hote hain.
  3. **Dynamic Policy Evaluation (ABAC)**: Access sirf role par nahi, balki Subject, Resource, Action, aur Environment (Time, Location, State) ke combination par decide hota hai.

```
Caller (Rider App / Microservice)
                │
                ▼ [Layer 1: Mutual TLS (mTLS) Handshake]
┌────────────────────────────────────────────────────────┐
│ Client presents X.509 Certificate (CN=checkout-api)    │
│ Server validates: Issuer == Root CA & Whitelist Match  │
└────────────────────────────────────────────────────────┘
                │
                ▼ [Layer 2: OAuth2 / OIDC Token Verification]
┌────────────────────────────────────────────────────────┐
│ Bearer Token verified via RS256 Public Key             │
│ Claims: sub="RIDER-SUNIL-99", role="Rider", scope      │
└────────────────────────────────────────────────────────┘
                │
                ▼ [Layer 3: Dynamic ABAC Policy Engine]
┌────────────────────────────────────────────────────────┐
│ Policy Checks:                                         │
│ 1. AssignedStore ("DS-NOIDA-62") == ResourceStore?     │
│ 2. OrderStatus == "Dispatched"?                        │
│ 3. Time elapsed <= 30 minutes?                         │
│ -> IF TRUE : Full Address & Phone Unmasked             │
│ -> IF FALSE: Phone Masked (+91-XXXXX), Address Hidden  │
└────────────────────────────────────────────────────────┘
```

---

## 3. Phir Kya Aaya, Kyu Aaya, Kaise Aaya, Kya Karta Hai?

### Step 1: `OAuth2TokenServer.cs` (Asymmetric RS256 Cryptography)
- **Kyu Aaya?** Symmetric HMAC-SHA256 (HS256) me saare microservices ko same secret key share karni padti hai. Agar 1 microservice compromise ho jaye, toh attacker poore system ke liye fake tokens generate kar sakta hai.
- **Kaise Kaam Karta Hai?**
  - RSA 2048-bit key pair generate hota hai.
  - Private key secure identity server ke paas rehti hai, aur public key baaki services me distribute hoti hai.

### Step 2: `TlsCertificateValidator.cs` (Mutual TLS Handshake)
- **Kyu Aaya?** Plaintext HTTP me Kubernetes cluster ke andar attacker pod network traffic sniff kar sakta hai (Packet Sniffing).
- **Kaise Kaam Karta Hai?**
  - TCP handshake ke waqt client aur server dono X.509 certificates exchange karte hain.
  - Root CA thumbprint verify hoti hai; untrusted CA se sign hua certificate turant drop ho jata hai.

### Step 3: `AttributeBasedAccessControl.cs` (Dynamic PII Privacy Gating)
- **Kyu Aaya?** Digital Personal Data Protection (DPDP) Act compliance. Delivery complete hone ke baad delivery boy ko customer ka phone number aur home address dekhne ki koi legitimate zaroorat nahi hoti.
- **Kaise Kaam Karta Hai?**
  - Subject, Resource, aur Environment context collect hota hai.
  - Delivery window expire hone par phone number dynamically mask ho jata hai (`+91-XXXXX-43210`).

---

## 4. ⚠️ Uske Na Hone Se Kya Hoga? (Production Catastrophes)

| Component Missing | Real-World Production Catastrophe |
|---|---|
| **Without Mutual TLS (mTLS)** | Attacker cluster me ek rogue container launch karke internal network par ARP spoofing karega aur database connection strings aur customer credit cards intercept kar lega. |
| **Without Asymmetric RS256 Tokens (using shared symmetric key)** | Internal inventory service ka developer symmetric JWT secret dekh kar fake Admin token generate kar lega aur poore platform ka database wipe kar dega. |
| **Without Attribute-Based Access Control (Static RBAC only)** | Delivery boy raat ko ghar ja kar un sabhi female customers ke phone numbers aur addresses browse karega jinko usne din me deliver kiya tha (Severe Safety & Privacy Breach). |
| **Without Cross-Store Access Protection** | Mumbai ka rogue employee Delhi ke dark-store ke VIP customers ke addresses aur orders download karke leak kar dega. |
| **Without Dynamic Data Masking** | Delivery complete hone ke baad bhi customer PII open rahegi, resulting in multi-crore regulatory fines under DPDP Act. |

---

## 5. How to Run & Verify

Run from PowerShell:
```powershell
dotnet run --project d:\ToLearn_Antigravity\31-ZeroTrust-mTLS-And-OAuth2-IdentityServer\KiranaHub.ZeroTrustSecurity.csproj
```

### Expected Output:
```text
====================================================================
  KiranaHub Project 31: Zero-Trust Security, mTLS & OAuth2 Suite   
====================================================================

[TEST 1] Testing OAuth2 Asymmetric (RS256) Token Authority...
  -> Issued RS256 JWT Token: eyJhbGciOiJSUzI1NiIsInR5c...ErsXNTWOOD-yhAg
  -> Validated Claims: Sub='RIDER-SUNIL-99', Role='Rider', Issuer='https://auth.kiranahub.internal': PASSED

[TEST 2] Testing Mutual TLS (mTLS) Peer Handshake & Service Whitelisting...
  -> mTLS Handshake for 'checkout-api': Authorized=True: PASSED
  -> Unauthorized Service Blocked: Service 'rogue-monitoring-agent' is not in authorized mTLS service whitelist!: PASSED
  -> Untrusted Foreign CA Rejected: Certificate was NOT issued by trusted KiranaHub Internal CA!: PASSED

[TEST 3] Testing Attribute-Based Access Control (ABAC) Dynamic PII Policy Gating...
  -> Active Delivery Context: Permitted=True, Address='Tower B, Flat 402, Sector 62, Noida': PASSED
  -> Post-Delivery Context: Permitted=False, Masked Phone='+91-XXXXX-43210': PASSED
  -> Cross-Store Access Blocked: Access Denied: Rider assigned to store 'DS-MUMBAI-01' cannot access orders of store 'DS-NOIDA-62'!: PASSED

====================================================================
 [SUCCESS] All Zero-Trust Security, mTLS & ABAC checks PASSED!       
====================================================================
```
