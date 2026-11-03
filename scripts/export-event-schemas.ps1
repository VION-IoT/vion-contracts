#!/usr/bin/env pwsh
<#
.SYNOPSIS
  Regenerate Vion.Contracts/Events/events.schema.json, the committed JSON Schema export of every Events/* payload.

.DESCRIPTION
  The generator is Vion.Contracts.Test/Events/EventsSchemaExporter.cs: JsonSchemaExporter over
  EventsJsonContext. The test EventsSchemaExportShould.MatchAFreshExportOfTheContext compares a fresh export
  against the committed file on every `dotnet test` (and so in the CI gate on every pull request); with
  VION_CONTRACTS_WRITE_EVENT_SCHEMAS=1 it writes the file instead. This script runs that one test in write mode.

  Review `git diff` afterwards: a changed export is a changed wire shape — a version question, see CLAUDE.md
  § Backwards compatibility.

.EXAMPLE
  pwsh scripts/export-event-schemas.ps1
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot

$env:VION_CONTRACTS_WRITE_EVENT_SCHEMAS = '1'
try
{
    dotnet test (Join-Path $repoRoot 'Vion.Contracts.Test') --nologo --filter 'FullyQualifiedName~EventsSchemaExportShould.MatchAFreshExportOfTheContext'
    if ($LASTEXITCODE -ne 0)
    {
        throw "dotnet test failed with exit code $LASTEXITCODE."
    }
}
finally
{
    Remove-Item Env:VION_CONTRACTS_WRITE_EVENT_SCHEMAS -ErrorAction SilentlyContinue
}

git -C $repoRoot status --short -- 'Vion.Contracts/Events/events.schema.json'
