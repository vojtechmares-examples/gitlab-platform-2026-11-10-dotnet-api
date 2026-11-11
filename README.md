# dotnet-api

ASP.NET Core (.NET 10) REST API with **Swagger**, EF Core and PostgreSQL, deployed to a Linux server as a **container** started with `docker run`.

| Topic | How |
|-------|-----|
| API | `GET/POST /api/todos`, `GET/PUT/DELETE /api/todos/{id}`, `GET /api/info` (build, environment), `GET /health` (answers only when PostgreSQL does); **Swagger UI at `/swagger`** |
| Images | Microsoft's MCR images: `dotnet/sdk:10.0` for the jobs, `dotnet/aspnet:10.0` for the runtime image (non-root user `app`) |
| Tests | xUnit: controller unit tests and integration tests through the real HTTP pipeline (in-memory database) |
| **Code coverage** | `coverlet` + ReportGenerator; the percentage appears on the job and in merge requests (`coverage:` regex), line coverage is drawn in the MR diff (`coverage_report`), and the HTML report is a job artifact (`coverage/`) |
| **Security scanning** | Trivy (pinned image through the Dependency Proxy): `trivy:repository` scans the repository (NuGet `packages.lock.json`, committed secrets), `trivy:image` scans the pushed image; JSON reports are artifacts; CRITICAL findings fail the job, which is allowed to fail |
| **Database migrations** | a pipeline step: `dotnet ef migrations bundle` builds one self-contained `efbundle`; `migrate:staging` (and, in production, the first step of the approval-gated `deploy:production`) runs it through an **SSH tunnel** to the server's PostgreSQL (no database port is opened to the network) with the password from a CI/CD variable scoped to the environment |
| Registry | the image goes to the GitLab Container Registry: `registry.maresdemo.com/examples/dotnet-api:<pipeline id>` |
| Deploy | over SSH: `docker login` with the project's **deploy token** (`gitlab-deploy-token`, `read_registry`), `docker pull`, write `IMAGE=...` to `/var/lib/examples/dotnet-api/<env>/image.env`, restart `dotnet-api-<env>.service` (its `ExecStart` is `docker run ...`) |
| HTTPS | Caddy: https://dotnet-api-staging.examples.maresdemo.com, https://dotnet-api-production.examples.maresdemo.com |
| Environments | `staging` (automatic on `main`), `production` (manual; protected environment: the migration and the deployment wait for the approval, then run in one job) |
| Rollback | manual `rollback:<env>` job runs the previous image again; migrations are not undone, so write them backward compatible |

Run it on a laptop: `docker run -d -p 5432:5432 -e POSTGRES_PASSWORD=postgres postgres:16`, then
`dotnet ef database update --project src/DotnetApi` and `dotnet run --project src/DotnetApi`.

## CI configuration

The pipeline reads the CI/CD variables below (*Settings > CI/CD > Variables*). Variables that start with `CI_` are predefined by GitLab.

| Variable | Set on | Type | Used for |
|----------|--------|------|----------|
| `DEPLOY_HOST` | group `examples` | Variable | Address of the deploy server |
| `DEPLOY_SSH_PORT` | group `examples` | Variable | SSH port of the deploy server (`6022`) |
| `DEPLOY_USER` | group `examples` | Variable | Account the pipelines log in as (`deploy`) |
| `SSH_KNOWN_HOSTS` | group `examples` | Variable | Host keys of the deploy server; the connection uses `StrictHostKeyChecking yes` |
| `SSH_PRIVATE_KEY` | group `examples` | File, **protected** | Private key of the `deploy` account |
| `EXAMPLES_DOMAIN` | group `examples` | Variable | Parent domain of the applications (`examples.maresdemo.com`); the deploy job waits for `https://dotnet-api-<env>.$EXAMPLES_DOMAIN` |
| `DB_PASSWORD` | this project, **once per environment scope** (`staging`, `production`) | Variable, **masked**, **protected** | Password of the PostgreSQL role `dotnet_api_<env>`. `deploy/migrate.sh` hands it to the migration bundle through the SSH tunnel and stops when it is missing. Use the value of `DB_PASSWORD_DOTNET_API` of the same environment in the `ansible` project, which creates the role |
| `CI_DEPLOY_USER`, `CI_DEPLOY_PASSWORD` | deploy token of this project | Predefined, from the token named **`gitlab-deploy-token`** (scope `read_registry`) | `deploy/release.sh` logs in to the Container Registry on the server with it. Create the token in *Settings > Repository > Deploy tokens*; the name must be exactly `gitlab-deploy-token` |

The group variables are inherited by every project in the `examples` group, so nothing has to be set for them here. The `.ssh-setup` snippet of `examples/ci-templates` turns the first five into `ssh deploy`.
A **protected** variable is passed only to pipelines on protected branches and tags (`main`), which is where the migration and deploy jobs run.
The two copies of `DB_PASSWORD` have the environment scopes `staging` and `production`, so a job of the environment `staging` gets the staging password.
