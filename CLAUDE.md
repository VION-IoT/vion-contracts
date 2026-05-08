> **Cross-repo work**: this repo is part of the VION platform.
> Architecture state, decisions, and cross-repo specs live in [`../architecture`](https://github.com/VION-IoT/architecture).
> Clone it: `git clone git@github.com:VION-IoT/architecture.git ../architecture`
> Before planning a feature with scope ≥ 2 repos, read the relevant `architecture/systems/*.md`
> and run `/spec <slug> <repos>` from the architecture repo.

# CLAUDE.md — vion-contracts

The shared schema layer of the VION platform: MQTT topics, JSON event
payloads, FlatBuffers schemas, type-reference / introspection helpers.
Build-time only — no runtime, no listeners. See
[`architecture/libraries/vion-contracts.md`](../architecture/libraries/vion-contracts.md)
for the cross-repo view, and [`architecture/concepts/wire-formats.md`](../architecture/concepts/wire-formats.md)
for how these schemas travel over the platform.

## Build / test

```powershell
dotnet build Vion.Contracts.sln
dotnet test Vion.Contracts.sln
```

Targets `netstandard2.1`. Standard .NET tooling — no special harness.

## Where stuff lives

| Path | Holds |
|------|-------|
| `Vion.Contracts/Mqtt/` | Topic constants, user-property names, MIME types |
| `Vion.Contracts/Events/` | JSON payload classes per direction (CloudToMesh, MeshToCloud, MeshToServiceProvider, ServiceProviderToMesh) |
| `Vion.Contracts/FlatBuffers/` | `.fbs` schemas (Common, Hw, Sw, Remote, System) |
| `Vion.Contracts/FlatBuffers.Generated/` | Generated C# from `.fbs` — **regenerated**, see below |
| `Vion.Contracts/Codec/` | `PropertyValue` encode / decode + JSON-Schema validation |
| `Vion.Contracts/TypeRef/` | Type-reference / property-metadata models |
| `Vion.Contracts/Introspection/` | Logic-block introspection result, plugin-info DTOs |
| `Vion.Contracts/Constants/` | Service-provider identifiers, system-component names |

## Adding payloads

**JSON event payloads** (in `Events/<Direction>/`):

- Decorate the class with `[Schema("YourPayloadName")]`. The attribute
  binds the class to the MQTT `schema` user-property value carried on
  every message — that's how receivers dispatch. Missing or mismatched
  schema names are bugs.
- Pick the direction folder; the folder name is the contract direction
  and consumers grep on it.
- Implement `IMessage` if the payload is a top-level envelope.

**FlatBuffers schemas** (in `FlatBuffers/<Area>/`):

- Edit / add the `.fbs` file. Add new fields **at the end** — field IDs
  are positional unless you use explicit `id:` tags.
- Regenerate via [`FlatBuffers/Generate.ps1`](Vion.Contracts/FlatBuffers/Generate.ps1).
  The output under `FlatBuffers.Generated/` is committed.
- Don't hand-edit `FlatBuffers.Generated/`. PR review rejects diffs that
  aren't reproducible from the schemas.

## Adding MQTT topics or user-properties

Add to `Mqtt/Topics.cs` or `Mqtt/MqttUserProperties.cs`. Names are part
of the wire and **append-only** — never rename, never repurpose. If the
topic is a template (`{tenantId}/{gatewayId}/...`), encode it as a
template constant and document the segments in a comment.

## Backwards compatibility (convention, not enforced)

- Within a major version: **schemas add fields only**. Never reorder,
  never repurpose IDs.
- MQTT topic and user-property constants: **append-only**.
- A breaking change requires a major-version bump and coordinated
  consumer rollout.

There is no CI schema-diff check today; the convention relies on PR
review. Treat any apparent shape change as a major-version question.

## No runtime side effects

This package is types and constants only. No hosted services, no static
constructors that touch I/O, no auto-discovery / reflection scanning at
type load. If you're tempted to add a `static` constructor with logic,
stop and reconsider.

## `netstandard2.1` shims and propagating deps

Two build-only shims are pinned `PrivateAssets="all"` so they don't
propagate to consumers:

- `IsExternalInit` — enables `init` setters.
- `RequiredMemberAttribute` — enables the `required` keyword.

Three runtime deps **do** propagate transitively:

- `System.Text.Json`, `System.Collections.Immutable` —
  `netstandard2.1` doesn't box them.
- `Microsoft.Extensions.Hosting.Abstractions` — types only.

## Versioning & releases

Tag-driven: `v0.2.0` → publishes `0.2.0` to nuget.org and the private
Azure DevOps feed. Pushes to `main` publish `0.0.0-ci.<run>` to the
**private feed only** — never depend on those from shipped code.
Versions on nuget.org are immutable.

Canonical reference: [`docs/releasing.md`](docs/releasing.md) +
[`publish.yml`](.github/workflows/publish.yml).

## Source availability

Apache-2.0; source-available. PRs from outside `vion-iot` are not
accepted — see [`CONTRIBUTING.md`](CONTRIBUTING.md). The package is
public on nuget.org so external SDK consumers
([`dale-sdk`](https://github.com/vion-iot/dale-sdk),
`service-provider-sdk-dotnet`) can resolve it.
