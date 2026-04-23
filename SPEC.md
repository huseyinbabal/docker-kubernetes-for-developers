# SPEC — Docker & Kubernetes for Developers Training Monorepo

Current-state specification. Documents what is in the repository today so new contributors and agents can orient quickly. This is a reference/training artifact, not a production system.

---

## 1. Objective

A hands-on teaching repo for the **"Docker & Kubernetes for Developers"** training by Hüseyin Babal. It contains a deliberately polyglot e-commerce system that is deployed progressively across 8 training sessions — from raw pods to a full GitOps pipeline with service mesh and observability.

**Audience:** Students and instructors of the training.
**Non-goal:** Running a real e-commerce business. The services exist to demonstrate Kubernetes concepts, not to be production-correct.

### Why three languages
Each service uses a different runtime, build tool, and database so students see K8s concepts applied across stacks rather than learning one toolchain's quirks:

| Service          | Language | Framework         | DB         | HTTP Port |
| ---------------- | -------- | ----------------- | ---------- | --------- |
| `user-service`    | Java 21  | Spring Boot 3.5.6 | MySQL 8.0  | 8081      |
| `product-service` | C# / .NET 8 | ASP.NET Core   | PostgreSQL 15 | 8082   |
| `order-service`   | Go 1.21  | Gin               | MongoDB 7.0 | 8083     |

All three publish async events to **RabbitMQ** (topic exchanges: `user.exchange`, `product.exchange`, `order.exchange`). All containers listen on `:8080` internally; the 8081/8082/8083 ports are the host-side mappings.

---

## 2. Commands

### Per-service build (from repo root)

```bash
# user-service (Maven → jar → Docker)
cd user-service && ./mvnw clean package && docker build -t user-service:latest .

# product-service (dotnet publish → Docker)
cd product-service && dotnet build && dotnet publish -c Release && docker build -t product-service:latest .

# order-service (go build → Docker)
cd order-service && go mod tidy && go build && docker build -t order-service:latest .
```

### Cluster provisioning — pick one flavor from `iac/`

```bash
# Local dev cluster
cd iac/kind && <follow README>

# Cloud cluster on Hetzner via Terraform (kubeadm v1.35 + Calico)
cd iac/hetzner-terraform && terraform init && terraform apply

# Bare-metal / existing hosts via Ansible
cd iac/kubespray && ansible-playbook -i inventory/mycluster/hosts.yaml cluster.yml

# Fully manual walkthrough
open iac/manual/node_preparations.txt
```

### GitOps — Flux v2 bootstrap

The Flux instance in `gitops/infrastructure/controllers/flux_instance.yaml` syncs this repo's `gitops/clusters/production` path from GitHub master at 1-minute intervals. Bootstrap is a one-time step per cluster:

```bash
flux install
kubectl apply -k gitops/clusters/production
```

From there, reconciliation is pull-based — do **not** `kubectl apply` workload manifests by hand.

### CI — GitHub Actions

- Workflow: `.github/workflows/docker-images.yaml`
- Triggers: PRs, pushes to tags, `workflow_dispatch`
- Matrix-builds all three service Dockerfiles and pushes to `ghcr.io/<owner>/{user,product,order}-service`
- Tags: `latest` + short SHA on main, `pr-N` on PRs, tag name on tag pushes
- Flux's `ImageRepository` + `ImageUpdateAutomation` (for user-service and product-service) polls GHCR and commits tag bumps back to master

---

## 3. Project Structure

```
microservices/
├── README.md                  # training overview + 8-session roadmap
├── microservices.sln          # .NET solution (product-service only)
├── SPEC.md                    # this file
│
├── user-service/              # Spring Boot / Java 21 / Maven / MySQL
│   ├── src/main/…             # Spring app sources
│   ├── pom.xml, mvnw, mvnw.cmd
│   └── Dockerfile             # multi-stage: maven:3-amazoncorretto-21 → amazoncorretto:21
│
├── product-service/           # ASP.NET Core / .NET 8 / PostgreSQL
│   ├── Controllers/           # /api/v1/products/*
│   ├── DTOs/ Models/ Data/ Services/
│   ├── Services/Messaging/    # RabbitMQPublisher → product.exchange
│   ├── ProductService.csproj, Program.cs
│   └── Dockerfile             # multi-stage: dotnet/sdk:8.0 → dotnet/aspnet:8.0
│
├── order-service/             # Gin / Go 1.21 / MongoDB
│   ├── main.go, go.mod, go.sum
│   ├── handlers/ routes/ services/ models/ database/ utils/
│   └── Dockerfile             # multi-stage: golang:1.21-alpine → alpine:latest
│
├── k8s/                       # raw manifests used in early sessions (sessions 3–6)
│                              # Deployments, Services, ConfigMaps, Secrets,
│                              # database StatefulSet-ish deployments.
│                              # NOT reconciled by Flux — deliberately hand-applied
│                              # for teaching. GitOps takes over in session 7.
│
├── iac/                       # four alternative cluster provisioning paths
│   ├── kind/                  # local dev
│   ├── manual/                # step-by-step kubeadm notes
│   ├── kubespray/             # Ansible (existing hosts)
│   └── hetzner-terraform/     # cloud cluster (kubeadm + Calico)
│
├── gitops/                    # Flux v2 — the session-7+ target state
│   ├── clusters/
│   │   ├── production/        # active cluster entry point
│   │   └── staging/           # placeholder
│   ├── infrastructure/
│   │   ├── configs/
│   │   └── controllers/       # istio, metallb, cert-manager, external-secrets,
│   │                          # vault, kube-prometheus-stack, tempo, kiali,
│   │                          # cnpg, mariadb-operator, capacitor, flux itself
│   └── apps/
│       ├── base/              # kustomize bases: Deployment, Service, VirtualService, DB
│       └── production/        # overlay with prod values + ImageUpdateAutomation
│
└── .github/workflows/
    └── docker-images.yaml     # matrix build → ghcr.io
```

### Layer wiring
```
  developer push
        │
        ▼
 .github/workflows          ──► ghcr.io/<owner>/<service>:<sha>
                                         │
                                         ▼
                              Flux ImageRepository (polls)
                                         │
                                         ▼
                       Flux ImageUpdateAutomation (commits tag)
                                         │
                                         ▼
                   gitops/clusters/production (Kustomization)
                                         │
                                         ▼
                               running cluster
```

---

## 4. Code Style

Each service follows its ecosystem's default conventions — no cross-cutting style is imposed, and that is intentional (students see idiomatic code in each stack).

- **user-service (Java):** Spring Boot layout — `controller/`, `service/`, `repository/`, `model/`, `dto/`, `config/`. Package base `com.huseyinbabal.userservice`. Standard Maven wrapper.
- **product-service (C#):** ASP.NET Core layout — `Controllers/`, `Services/`, `Models/`, `DTOs/`, `Data/`. PascalCase throughout per .NET norms. Nested `Services/Messaging/` isolates RabbitMQ.
- **order-service (Go):** Flat package per responsibility — `handlers/`, `routes/`, `services/`, `models/`, `database/`, `utils/`. `main.go` at root wires Gin + Mongo + RabbitMQ.

### Cross-cutting conventions (what IS shared)
- **HTTP shape:** `/api/v1/<resource>` for all services; health endpoints exposed.
- **Config:** all services read from environment variables (no committed secrets; DB hosts/ports/creds injected at runtime).
- **Container port:** always `8080` inside the container.
- **Image names:** `ghcr.io/<owner>/<service-directory-name>` — must match the directory name verbatim for the CI matrix to pick them up.
- **Event topology:** one RabbitMQ topic exchange per service (`<domain>.exchange`) with topic routing keys like `user.created`, `order.status.updated`, `product.stock.updated`.

### YAML / Kustomize conventions
- `gitops/apps/base/<service>/` contains `kustomization.yaml`, `deployment.yaml`, `service.yaml`, `virtualservice.yaml`, and a `database.yaml` (CNPG Cluster or MariaDB resource).
- Environment overlays live under `gitops/apps/<env>/` and patch the base — image tags are **not** hand-edited; `ImageUpdateAutomation` owns them.

---

## 5. Testing Strategy

**Current state: no automated tests are committed** in any service. The `user-service` pom declares `spring-boot-starter-test` but no test sources exist; `product-service` and `order-service` have none.

Verification of the training artifacts is done manually, per the README:

```bash
curl http://localhost:8081/api/v1/users/health
curl http://localhost:8082/api/v1/products/health
curl http://localhost:8083/api/v1/health
```

Plus the message-flow sanity check via the RabbitMQ management UI at `http://localhost:15672` (admin / password).

### CI quality gates
- The GitHub Actions workflow validates that **each service Dockerfile still builds** on every PR. That is the only automated gate.

### When adding tests
If tests are introduced later, keep them stack-idiomatic — JUnit/Spring Test for Java, `dotnet test` for .NET, `go test ./...` for Go — and wire them into the existing matrix workflow rather than creating a new one.

---

## 6. Boundaries

Minimal — the repo is a teaching tool, so most rules are soft.

**Always:**
- Preserve the three-language split. It is pedagogical, not accidental.
- Keep the 8-session progression intact: raw `k8s/` manifests before session 7, `gitops/` from session 7 onward. Do not delete either.
- Keep container port `8080` and the `ghcr.io/<owner>/<dir-name>` image-naming contract — the CI matrix and Flux image policies both depend on it.

**Ask first:**
- Changes to `.github/workflows/docker-images.yaml` (breaks the image pipeline for every downstream consumer of the training).
- Renaming service directories (breaks GHCR tags and Flux `ImageRepository` paths).
- Adding a fourth service or swapping a database — the choices are load-bearing teaching material.

**Never:**
- Commit real secrets. All DB creds and RabbitMQ creds come from env vars at runtime.
- `kubectl apply` workloads directly into a cluster managed by Flux — let reconciliation do it.
- Hand-edit image tags inside `gitops/apps/` — `ImageUpdateAutomation` owns those lines.
