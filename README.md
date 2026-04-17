# Vion.Contracts

Shared contracts — MQTT topics, payloads, FlatBuffers schemas, and introspection models — used by the [Vion Dale SDK](https://github.com/vion-iot/dale-sdk) and the Vion Dale runtime.

Full documentation: **https://docs.vion.swiss**

## Install

```bash
dotnet add package Vion.Contracts
```

Targets `netstandard2.1`. This package exists in its own repository because it is consumed by both public and private code and has a release cadence independent of either.

## Source-available

This repository is source-available under [Apache 2.0](LICENSE). Issues and pull requests are not accepted from outside the `vion-iot` organization. See [CONTRIBUTING.md](CONTRIBUTING.md), [SUPPORT.md](SUPPORT.md), and [SECURITY.md](SECURITY.md).

Maintainers: the release process lives in [docs/releasing.md](docs/releasing.md).
