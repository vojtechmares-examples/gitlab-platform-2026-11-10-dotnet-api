# dotnet-api

ASP.NET Core (.NET 10) REST API with **Swagger**, EF Core and PostgreSQL, deployed to a Linux server as a **container** started with `docker run`.

| Topic | How |
|-------|-----|
| API | `GET/POST /api/todos`, `GET/PUT/DELETE /api/todos/{id}`, `GET /api/info` (build, environment), `GET /health` (answers only when PostgreSQL does); **Swagger UI at `/swagger`** |
| Images | Microsoft's MCR images: `dotnet/sdk:10.0` for the jobs, `dotnet/aspnet:10.0` for the runtime image (non-root user `app`) |
| Tests | xUnit: controller unit tests and integration tests through the real HTTP pipeline (in-memory database) |
| **Code coverage** | `coverlet` + ReportGenerator; the percentage appears on the job and in merge requests (`coverage:` regex), line coverage is drawn in the MR diff (`coverage_report`), and the HTML report is a job artifact (`coverage/`) |
| **Security scanning** | Trivy (pinned image through the Dependency Proxy): `trivy:repository` scans the repository (NuGet `packages.lock.json`, committed secrets), `trivy:image` scans the pushed image; JSON reports are artifacts; fixable CRITICAL findings fail the job, which is allowed to fail |
| **Database migrations** | a pipeline step: `dotnet ef migrations bundle` builds one self-contained `efbundle`; `migrate:<env>` runs it through an **SSH tunnel** to the server's PostgreSQL (no database port is opened to the network) with the password from a CI/CD variable scoped to the environment |
| Registry | the image goes to the GitLab Container Registry: `registry.maresdemo.com/examples/dotnet-api:<pipeline id>` |
| Deploy | over SSH: `docker login` with the project's **deploy token** (`gitlab-deploy-token`, `read_registry`), `docker pull`, write `IMAGE=...` to `/var/lib/examples/dotnet-api/<env>/image.env`, restart `dotnet-api-<env>.service` (its `ExecStart` is `docker run ...`) |
| HTTPS | Caddy: https://dotnet-api-staging.examples.maresdemo.com, https://dotnet-api-production.examples.maresdemo.com |
| Environments | `staging` (automatic on `main`), `production` (manual; protected environment) |
| Rollback | manual `rollback:<env>` job runs the previous image again; migrations are not undone, so write them backward compatible |

Run it on a laptop: `docker run -d -p 5432:5432 -e POSTGRES_PASSWORD=postgres postgres:16`, then
`dotnet ef database update --project src/DotnetApi` and `dotnet run --project src/DotnetApi`.
