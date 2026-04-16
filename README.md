# Vion.Contracts

Shared contracts — MQTT topics, payloads, FlatBuffers schemas, and introspection models — used by the [Vion Dale SDK](https://github.com/vion-iot/dale-sdk) and the Vion Dale runtime.

This package exists as its own repository because it is consumed by both public and private code, and has a release cadence independent of either.

## Install

```bash
dotnet add package Vion.Contracts
```

Targets `netstandard2.1`.

## Source-available

This repository is source-available. Issues and pull requests are not accepted from outside the `vion-iot` organization. For questions or to report a problem, see [SUPPORT.md](SUPPORT.md).

## Releases

Versions are driven by git tags. There is no version number in any `.csproj`.

| Trigger | Published version | Destination |
|---|---|---|
| Push to `main` | `0.0.0-ci.{run_number}` | Private Azure DevOps feed only — for internal integration testing, never depend on from shipped code |
| Push tag `v0.2.0` | `0.2.0` | Private feed + nuget.org |
| Push tag `v0.3.0-preview.1` | `0.3.0-preview.1` | Private feed + nuget.org (treated as pre-release) |

### Cutting a release

Prerequisites:
- `main` is green on the commit you want to release.
- You have [`gh`](https://cli.github.com/) installed and authenticated (`gh auth status`).

```bash
# Stable release:
gh release create v0.2.0 --target main --generate-notes \
  --title "v0.2.0" --notes "Short release summary."

# Pre-release (add --prerelease for the UI badge; NuGet detects pre-release
# automatically from the SemVer suffix):
gh release create v0.2.0-preview.1 --target main --prerelease --generate-notes \
  --title "v0.2.0-preview.1" --notes "What this preview validates."
```

Creating the release pushes the tag, which triggers [`publish.yml`](.github/workflows/publish.yml):

1. Builds and packs with `Version` taken from the tag (strips the `v` prefix).
2. Pushes `.nupkg` + `.snupkg` to the private Azure DevOps feed.
3. Publishes to nuget.org using [Trusted Publishing](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing) (short-lived OIDC token — no API key stored).

Verify the result under the [VION-IoT profile on nuget.org](https://www.nuget.org/profiles/VION-IoT).

### Version immutability

Once a version is published to nuget.org, the version ID is permanent. You can *unlist* a version (which hides it from search and `dotnet add package`), but the ID stays burned — you cannot re-upload the same version, even after yanking. Pick the next number for any subsequent change, even a tiny fix.

### Required configuration

One-time setup per repo; flag this if you fork or rotate credentials:

- GitHub secret `AZURE_DEVOPS_PAT` — PAT with `Packaging: Read & write` on the Azure DevOps feed.
- GitHub secret `NUGET_USER` — nuget.org profile name that owns the Trusted Publishing policy (currently `VION-IoT`).
- Trusted Publishing policy on nuget.org: Repository Owner `VION-IoT`, Repository `vion-contracts`, Workflow File `publish.yml`. See [NuGet's Trusted Publishing docs](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing) for the UI walkthrough.

## License

Apache 2.0 — see [LICENSE](LICENSE).
