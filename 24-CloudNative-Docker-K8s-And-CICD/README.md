# Project 24: Cloud-Native Docker, Kubernetes Orchestration & GitHub Actions CI/CD

> **Quick-Commerce Domain:** KiranaHub Production Infrastructure & Deployment Engine  
> **Tech Stack:** Docker (Multi-stage builds), Docker Compose, Kubernetes (Deployments, Services, Ingress, HPA, ConfigMaps, Secrets), GitHub Actions, Trivy Security Scanner, Nginx Unprivileged.

---

## 1. Iss Project Ko Scratch Se Kaise Padhein? (Roadmap)

Local laptop par code run karna aur usse 99.99% SLA ke sath production cloud me chalana do alag baatein hain. Quick-commerce me agar shaam ko 7 PM flash sale ke time system 5 minute ke liye bhi down ho jaye, toh lakhon rupaye ka nuksan aur customer trust loss hota hai.

Aapko yeh project iss step-by-step order me padhna hai:
1. **`Dockerfile.api` & `Dockerfile.web`**:
   - Dekhiye ki kaise multi-stage builds 2GB ke heavy SDKs aur dev dependencies ko discard karke 110MB ki ultra-lightweight, rootless (`USER $APP_UID`) production images create karte hain.
2. **`docker-compose.yml`**:
   - Samajhiye ki Web API, Storefront, Redis Geospatial, RabbitMQ Saga, aur PostgreSQL ko network segregation (`frontend-net`, `backend-net`) aur health probe synchronization ke sath local cluster me kaise chalaya jata hai.
3. **`k8s/deployment.yaml` & `k8s/service.yaml`**:
   - Dekhiye ki zero-downtime rolling updates (`maxSurge: 1`, `maxUnavailable: 0`), non-root security context, aur `livenessProbe`/`readinessProbe` kaise crash-loop hone par system ko auto-heal karte hain.
4. **`k8s/ingress.yaml` & `k8s/hpa.yaml`**:
   - Dekhiye ki Ingress controller single domain se `/api` aur `/` traffic ko TLS termination ke sath kaise route karta hai, aur Horizontal Pod Autoscaler (HPA) CPU/RAM spike hone par automatically pods ko 2 se 10 kaise scale karta hai.
5. **`.github/workflows/ci.yml`**:
   - Dekhiye ki har git push aur pull request par automated tests, multi-stage container builds, aur Trivy CVE vulnerability scan kaise fail-safe gate ki tarah act karte hain.

---

## 2. Kya Pehle Bna, Kaise Bna? (Foundations)

### Foundation: 12-Factor App & Container Immutability
- Pehle monolithic systems me code direct virtual machine (VM) par deploy hota tha. Har VM ka OS version, installed packages, aur env alag hote the ("mere laptop me chal raha hai, server par kyu nahi chal raha?").
- Is problem ko permanently khatam karne ke liye containerization banaya gaya:
  - **Single Artifact Principle**: Wahi image jo staging me test hui hai, wahi bina kisi code change ke production me deploy hoti hai.
  - **Decoupled Configs**: Saare environment variables aur passwords image ke bahar `ConfigMap` aur `Secret` me rehte hain.

```
Developer Push (Git)
        │
        ▼ [.github/workflows/ci.yml]
┌─────────────────────────────────────────────────────────────┐
│ GitHub Actions Runner: .NET Tests + Node Tests + Trivy Scan │
└─────────────────────────────────────────────────────────────┘
        │
        ▼ (Build & Tag Immutable Image)
┌─────────────────────────────────────────────────────────────┐
│ Multi-Stage Docker Build -> Distroless Non-Root Image       │
└─────────────────────────────────────────────────────────────┘
        │
        ▼ [Kubernetes Cluster]
┌─────────────────────────────────────────────────────────────┐
│ Ingress -> Service -> Pods (HPA: 2..10) -> Redis / Postgres │
└─────────────────────────────────────────────────────────────┘
```

---

## 3. Phir Kya Aaya, Kyu Aaya, Kaise Aaya, Kya Karta Hai?

### Step 1: Multi-Stage Dockerfiles (`Dockerfile.api` & `Dockerfile.web`)
- **Kyu Aaya?** Single-stage Dockerfile me Roslyn C# compiler, Node.js devDependencies, aur git binaries production image me chali jaati hain. Isse image size 2GB ho jaati hai aur CVE security vulnerabilities exponentially badh jaati hain.
- **Kaise Kaam Karta Hai?**
  - **Stage 1 (build-env):** Full SDK me restore aur publish run hota hai.
  - **Stage 2 (runtime):** Sirf binary DLLs aur assets copy hote hain minimal chiseled runtime me.
  - Non-root user (`USER $APP_UID` = 1654) enforce hota hai jisse container escape attacks block ho jaate hain.

### Step 2: `docker-compose.yml` (Segregated Local Cloud)
- **Kyu Aaya?** Distributed microservices me developer ko local debugging ke liye 5 alag terminals me DBs aur brokers start karne padte the.
- **Kaise Kaam Karta Hai?**
  - Ek command `docker compose up` se API, Frontend, Redis, RabbitMQ, aur PostgreSQL launch hote hain.
  - `condition: service_healthy` ensure karta hai ki API tabhi start ho jab Redis ping aur Postgres `pg_isready` pass ho chuke hon.
  - `frontend-net` aur `backend-net` alag networks hain, jisse database direct browser se physically unreachable ho jata hai.

### Step 3: Kubernetes Orchestration (`k8s/`)
- **Kyu Aaya?** Agar traffic badhne par pod crash ho jaye ya new deployment par downtime ho, toh manual server restarts impossible hote hain.
- **Kaise Kaam Karta Hai?**
  - **`deployment.yaml`**: `RollingUpdate` with `maxUnavailable: 0` ensure karta hai ki naya pod jab tak `/health/ready` pass na kare, purana pod band nahi hoga (Zero Downtime).
  - **`hpa.yaml`**: CPU > 75% ya RAM > 80% hote hi pods 2 se 10 tak dynamically scale out ho jaate hain. Flash sale khatam hone par 5 minute cooldown ke baad auto-scale down ho jaate hain.
  - **`ingress.yaml`**: Single entrypoint par SSL terminate karke `/api` ko backend service aur `/` ko frontend service me forward karta hai.

### Step 4: GitHub Actions CI/CD (`.github/workflows/ci.yml`)
- **Kyu Aaya?** Agar koi developer galti se broken test ya vulnerable package commit kar de, toh wo production me nahi jaana chahiye.
- **Kaise Kaam Karta Hai?**
  - Parallel jobs `.NET 10` aur `Node.js 22` unit tests verify karte hain.
  - Tests pass hone ke baad Docker build trigger hota hai aur Aqua Security Trivy image ko scan karke CRITICAL vulnerabilities flag karta hai.

---

## 4. ⚠️ Uske Na Hone Se Kya Hoga? (Production Catastrophes)

| Component Missing | Real-World Production Catastrophe |
|---|---|
| **Without Multi-Stage & Rootless Container** | Attacker application vulnerability use karke container ke andar root shell access le lega aur host machine ke Docker socket ko hijack karke poora cloud infrastructure delete kar sakta hai. |
| **Without Health Probes (`liveness` & `readiness`)** | Naye deployment ke waqt application abhi boot hi ho rahi hogi (DB connection initialize nahi hua hoga) aur K8s live users ki requests usse bhej dega, causing massive 502 Bad Gateway errors. |
| **Without RollingUpdate (`maxUnavailable: 0`)** | Har deployment ke time saare existing pods ek sath band honge, jisse deployment ke 30 second ke window me order kar rahe hazaron customers ka checkout drop ho jayega. |
| **Without HPA (Horizontal Pod Autoscaler)** | Flash sale me achanak 50,000 users aane par 2 pods 100% CPU par freeze ho jayenge aur OOMKilled ho kar crash ho jayenge. Manual intervention me 30 minute lag jayenge tab tak company ka business collapse ho jayega. |
| **Without Automated CI/CD & Trivy Scan** | Developer ke laptop par pass hone wala broken build production me chala jayega, ya vulnerable log4j/crypto package bina scan hue live ho jayega, resulting in data leaks and regulatory fines. |

---

## 5. How to Run & Verify

Run the automated verification suite using `npm.cmd test`:
```powershell
npm.cmd test --prefix d:\ToLearn_Antigravity\24-CloudNative-Docker-K8s-And-CICD
```

### Expected Output:
```text
====================================================================
  KiranaHub Project 24: Cloud-Native Docker, K8s & CI/CD Verifier  
====================================================================

[STAGE 1] Verifying ASP.NET 10 Web API Dockerfile (Dockerfile.api)...
  -> [PASS] Multi-stage build stage defined with .NET 10 SDK
  -> [PASS] Runtime stage defined with minimal ASP.NET 10 runtime
  -> [PASS] Security Hardening: Runs under non-root $APP_UID
  -> [PASS] Exposes standard non-root port 8080
  -> [PASS] Production exec-form entrypoint configured

[STAGE 2] Verifying Frontend Web Dockerfile & Nginx SPA Configuration...
  -> [PASS] Frontend build leverages Node.js 22 alpine
  -> [PASS] Production runner uses non-root unprivileged Nginx
  -> [PASS] Nginx handles SPA client-side routing on page refresh
  -> [PASS] Nginx exposes /healthz probe endpoint

[STAGE 3] Verifying Docker Compose Full-Stack Cluster...
  -> [PASS] Web API service present
  -> [PASS] Frontend Storefront service present
  -> [PASS] Redis Geospatial cache service present
  -> [PASS] RabbitMQ Saga message broker present
  -> [PASS] PostgreSQL database service present
  -> [PASS] Network isolation with frontend and backend subnets
  -> [PASS] Startup synchronization with health probes

[STAGE 4] Verifying Kubernetes Manifests (k8s/)...
  -> [PASS] Zero-downtime RollingUpdate strategy configured
  -> [PASS] Liveness probe bound to /health/live
  -> [PASS] Readiness probe bound to /health/ready
  -> [PASS] Container resource limits enforced
  -> [PASS] ClusterIP internal service mapped to 8080
  -> [PASS] Ingress path routing for API and Frontend
  -> [PASS] TLS encryption configured for Ingress
  -> [PASS] HPA auto-scales from 2 to 10 pods
  -> [PASS] ConfigMap and Secret decoupled from image

[STAGE 5] Verifying GitHub Actions CI/CD Pipeline (.github/workflows/ci.yml)...
  -> [PASS] Triggers on push and pull_request to main
  -> [PASS] Build and test .NET job configured
  -> [PASS] Build and test frontend job configured
  -> [PASS] Trivy security vulnerability scanner integrated

====================================================================
 [SUCCESS] All 29 Cloud-Native, K8s & CI/CD checks PASSED! 
====================================================================
```
