// ==============================================================================================
// KiranaHub Cloud-Native: Automated Manifest & Configuration Verification Suite
// [KYA KARTA HAI] Dockerfiles, docker-compose, Kubernetes manifests, aur CI/CD configs ko statically parse karke validate karta hai.
// [KYU ZAROORI HAI] Production deployment se pehle syntax errors, missing security probes, ya broken container contracts pakadna.
// [KAISE CHALTA HAI] File system se files read karke regex aur semantic assertions verify karta hai.
// [OUTPUT / SIDE EFFECT] Terminal me detailed compliance report print karta hai.
// [VARIATIONS TO TRY] Conftest or Open Policy Agent (OPA) policy checks.
// ==============================================================================================

// [KYA KARTA HAI] Node.js built-in file system promises module import karta hai.
// [KYU ZAROORI HAI] Asynchronously configuration files ko disk se read karne ke liye.
// [KAISE CHALTA HAI] Node V8 runtime fs bindings inject karta hai.
// [OUTPUT / SIDE EFFECT] fs API available.
// [VARIATIONS TO TRY] Synchronous fs methods.
import fs from 'node:fs/promises';

// [KYA KARTA HAI] Path resolution module import karta hai.
// [KYU ZAROORI HAI] Cross-platform Windows/Linux file paths seamlessly construct karne ke liye.
// [KAISE CHALTA HAI] OS specific path separators handle karta hai.
// [OUTPUT / SIDE EFFECT] path utilities loaded.
// [VARIATIONS TO TRY] URL file:// paths.
import path from 'node:path';

// [KYA KARTA HAI] Current file directory resolve karta hai.
// [KYU ZAROORI HAI] Relative path traversal ko anchor karne ke liye.
// [KAISE CHALTA HAI] import.meta.url se directory path derive karta hai.
// [OUTPUT / SIDE EFFECT] Base directory set.
// [VARIATIONS TO TRY] process.cwd().
const baseDir = path.resolve(import.meta.dirname, '..');

// [KYA KARTA HAI] Test tracker statistics object initialize karta hai.
// [KYU ZAROORI HAI] Passed aur failed checks ka final tally maintain karne ke liye.
// [KAISE CHALTA HAI] Counter increments on each assertion.
// [OUTPUT / SIDE EFFECT] Counters start at 0.
// [VARIATIONS TO TRY] Output in JUnit XML format.
let passedChecks = 0;

// [KYA KARTA HAI] Reusable assertion helper function define karta hai.
// [KYU ZAROORI HAI] Check condition evaluate karke readable status message print karna.
// [KAISE CHALTA HAI] Condition true hone par green tick print karta hai, false hone par exception throw karta hai.
// [OUTPUT / SIDE EFFECT] Terminal log output.
// [VARIATIONS TO TRY] Colorful ANSI logs.
function assert(condition, message) {
    // [KYA KARTA HAI] Condition evaluate karta hai.
    // [KYU ZAROORI HAI] Check passing state determine karna.
    // [KAISE CHALTA HAI] Truthy check.
    // [OUTPUT / SIDE EFFECT] Branch execution.
    // [VARIATIONS TO TRY] Strict equality check.
    if (!condition) {
        // [KYA KARTA HAI] Error throw karta hai assertion fail hone par.
        // [KYU ZAROORI HAI] Test execution abort karke stack trace dikhana.
        // [KAISE CHALTA HAI] Throws Error instance.
        // [OUTPUT / SIDE EFFECT] Process exits with failure.
        // [VARIATIONS TO TRY] Soft assertions collecting all errors.
        throw new Error(`[FAIL] ${message}`);
    }
    // [KYA KARTA HAI] Passed test count increment karta hai.
    // [KYU ZAROORI HAI] Summary metrics update karna.
    // [KAISE CHALTA HAI] passedChecks++.
    // [OUTPUT / SIDE EFFECT] Counter updated.
    // [VARIATIONS TO TRY] Record check names in an array.
    passedChecks++;
    // [KYA KARTA HAI] Success status line print karta hai.
    // [KYU ZAROORI HAI] Developer ko visual feedback dena.
    // [KAISE CHALTA HAI] Outputs to stdout.
    // [OUTPUT / SIDE EFFECT] Console log.
    // [VARIATIONS TO TRY] Log to file.
    console.log(`  -> [PASS] ${message}`);
}

// [KYA KARTA HAI] Main async verification runner function define karta hai.
// [KYU ZAROORI HAI] Saari file checks ko step-by-step asynchronously execute karna.
// [KAISE CHALTA HAI] Top-level async IIFE pattern.
// [OUTPUT / SIDE EFFECT] Complete verification run.
// [VARIATIONS TO TRY] Parallel Promise.all execution.
async function runVerification() {
    // [KYA KARTA HAI] Header banner print karta hai.
    // [KYU ZAROORI HAI] Clean report separation.
    // [KAISE CHALTA HAI] Console log.
    // [OUTPUT / SIDE EFFECT] Banner displayed.
    // [VARIATIONS TO TRY] ASCII art logo.
    console.log('====================================================================');
    console.log('  KiranaHub Project 24: Cloud-Native Docker, K8s & CI/CD Verifier  ');
    console.log('====================================================================\n');

    // [KYA KARTA HAI] Check 1: Dockerfile.api multi-stage and security verification.
    // [KYU ZAROORI HAI] ASP.NET 10 microservice security standards confirm karna.
    // [KAISE CHALTA HAI] Reads file text and asserts security tokens.
    // [OUTPUT / SIDE EFFECT] Verifies Dockerfile.api.
    // [VARIATIONS TO TRY] Dockerfile linter Hadolint.
    console.log('[STAGE 1] Verifying ASP.NET 10 Web API Dockerfile (Dockerfile.api)...');
    const apiDockerfile = await fs.readFile(path.join(baseDir, 'Dockerfile.api'), 'utf-8');
    assert(apiDockerfile.includes('FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build-env'), 'Multi-stage build stage defined with .NET 10 SDK');
    assert(apiDockerfile.includes('FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime'), 'Runtime stage defined with minimal ASP.NET 10 runtime');
    assert(apiDockerfile.includes('USER $APP_UID'), 'Security Hardening: Runs under non-root $APP_UID');
    assert(apiDockerfile.includes('EXPOSE 8080'), 'Exposes standard non-root port 8080');
    assert(apiDockerfile.includes('ENTRYPOINT ["dotnet", "KiranaHub.WebApi.dll"]'), 'Production exec-form entrypoint configured');

    // [KYA KARTA HAI] Check 2: Dockerfile.web & Nginx SPA configuration.
    // [KYU ZAROORI HAI] Frontend containerization aur client-side routing integrity check karna.
    // [KAISE CHALTA HAI] Reads Dockerfile.web and nginx.conf.
    // [OUTPUT / SIDE EFFECT] Verifies Web container.
    // [VARIATIONS TO TRY] Nginx syntax dry-run.
    console.log('\n[STAGE 2] Verifying Frontend Web Dockerfile & Nginx SPA Configuration...');
    const webDockerfile = await fs.readFile(path.join(baseDir, 'Dockerfile.web'), 'utf-8');
    const nginxConf = await fs.readFile(path.join(baseDir, 'nginx.conf'), 'utf-8');
    assert(webDockerfile.includes('FROM node:22-alpine AS deps'), 'Frontend build leverages Node.js 22 alpine');
    assert(webDockerfile.includes('FROM nginxinc/nginx-unprivileged:alpine AS runner'), 'Production runner uses non-root unprivileged Nginx');
    assert(nginxConf.includes('try_files $uri $uri/ /index.html;'), 'Nginx handles SPA client-side routing on page refresh');
    assert(nginxConf.includes('location /healthz'), 'Nginx exposes /healthz probe endpoint');

    // [KYA KARTA HAI] Check 3: docker-compose.yml multi-container orchestration.
    // [KYU ZAROORI HAI] Stack completeness (API, Web, Redis, RabbitMQ, Postgres) verify karna.
    // [KAISE CHALTA HAI] Reads docker-compose.yml.
    // [OUTPUT / SIDE EFFECT] Verifies local composition.
    // [VARIATIONS TO TRY] Docker Compose config validation command.
    console.log('\n[STAGE 3] Verifying Docker Compose Full-Stack Cluster...');
    const dockerCompose = await fs.readFile(path.join(baseDir, 'docker-compose.yml'), 'utf-8');
    assert(dockerCompose.includes('kiranahub-api:'), 'Web API service present');
    assert(dockerCompose.includes('kiranahub-web:'), 'Frontend Storefront service present');
    assert(dockerCompose.includes('kiranahub-redis:'), 'Redis Geospatial cache service present');
    assert(dockerCompose.includes('kiranahub-rabbitmq:'), 'RabbitMQ Saga message broker present');
    assert(dockerCompose.includes('kiranahub-postgres:'), 'PostgreSQL database service present');
    assert(dockerCompose.includes('frontend-net:') && dockerCompose.includes('backend-net:'), 'Network isolation with frontend and backend subnets');
    assert(dockerCompose.includes('condition: service_healthy'), 'Startup synchronization with health probes');

    // [KYA KARTA HAI] Check 4: Kubernetes manifests (Deployment, Service, Ingress, HPA, ConfigMap).
    // [KYU ZAROORI HAI] Production cluster deployment readiness guarantee karna.
    // [KAISE CHALTA HAI] Reads k8s/*.yaml files.
    // [OUTPUT / SIDE EFFECT] Verifies K8s manifests.
    // [VARIATIONS TO TRY] Kubectl dry-run validate.
    console.log('\n[STAGE 4] Verifying Kubernetes Manifests (k8s/)...');
    const deployment = await fs.readFile(path.join(baseDir, 'k8s/deployment.yaml'), 'utf-8');
    const service = await fs.readFile(path.join(baseDir, 'k8s/service.yaml'), 'utf-8');
    const ingress = await fs.readFile(path.join(baseDir, 'k8s/ingress.yaml'), 'utf-8');
    const hpa = await fs.readFile(path.join(baseDir, 'k8s/hpa.yaml'), 'utf-8');
    const configs = await fs.readFile(path.join(baseDir, 'k8s/configmap-secret.yaml'), 'utf-8');

    assert(deployment.includes('type: RollingUpdate') && deployment.includes('maxUnavailable: 0'), 'Zero-downtime RollingUpdate strategy configured');
    assert(deployment.includes('livenessProbe:') && deployment.includes('/health/live'), 'Liveness probe bound to /health/live');
    assert(deployment.includes('readinessProbe:') && deployment.includes('/health/ready'), 'Readiness probe bound to /health/ready');
    assert(deployment.includes('limits:') && deployment.includes('memory: "512Mi"'), 'Container resource limits enforced');
    assert(service.includes('type: ClusterIP') && service.includes('targetPort: 8080'), 'ClusterIP internal service mapped to 8080');
    assert(ingress.includes('path: /api') && ingress.includes('path: /'), 'Ingress path routing for API and Frontend');
    assert(ingress.includes('kiranahub-tls-secret'), 'TLS encryption configured for Ingress');
    assert(hpa.includes('minReplicas: 2') && hpa.includes('maxReplicas: 10'), 'HPA auto-scales from 2 to 10 pods');
    assert(configs.includes('kind: ConfigMap') && configs.includes('kind: Secret'), 'ConfigMap and Secret decoupled from image');

    // [KYA KARTA HAI] Check 5: GitHub Actions CI/CD Pipeline.
    // [KYU ZAROORI HAI] CI automation checks confirm karna.
    // [KAISE CHALTA HAI] Reads .github/workflows/ci.yml.
    // [OUTPUT / SIDE EFFECT] Verifies CI pipeline.
    // [VARIATIONS TO TRY] Actionlint parser.
    console.log('\n[STAGE 5] Verifying GitHub Actions CI/CD Pipeline (.github/workflows/ci.yml)...');
    const ciWorkflow = await fs.readFile(path.join(baseDir, '.github/workflows/ci.yml'), 'utf-8');
    assert(ciWorkflow.includes('branches: [ main ]'), 'Triggers on push and pull_request to main');
    assert(ciWorkflow.includes('build-and-test-dotnet:'), 'Build and test .NET job configured');
    assert(ciWorkflow.includes('build-and-test-frontend:'), 'Build and test frontend job configured');
    assert(ciWorkflow.includes('Scan Image for Vulnerabilities (Trivy)'), 'Trivy security vulnerability scanner integrated');

    // [KYA KARTA HAI] Summary footer print karta hai.
    // [KYU ZAROORI HAI] Final pass count display karna.
    // [KAISE CHALTA HAI] Console logs total passed checks.
    // [OUTPUT / SIDE EFFECT] Green exit.
    // [VARIATIONS TO TRY] Return exit code.
    console.log('\n====================================================================');
    console.log(` [SUCCESS] All ${passedChecks} Cloud-Native, K8s & CI/CD checks PASSED! `);
    console.log('====================================================================\n');
}

// [KYA KARTA HAI] Verification suite execute karta hai aur unhandled errors handle karta hai.
// [KYU ZAROORI HAI] Non-zero exit code emit karna failure par.
// [KAISE CHALTA HAI] Catch block catches error and exits with 1.
// [OUTPUT / SIDE EFFECT] Node process exit code.
// [VARIATIONS TO TRY] Alert notification on failure.
runVerification().catch(err => {
    console.error(err.message);
    process.exit(1);
});
