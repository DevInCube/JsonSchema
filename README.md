# My.Json.Schema

A [JSON Schema][json-schema-home] framework for .NET supporting draft-04, draft-06, draft-07, and draft 2019-09.

[![CI](https://github.com/DevInCube/JsonSchema/actions/workflows/ci.yml/badge.svg)](https://github.com/DevInCube/JsonSchema/actions/workflows/ci.yml)

| Draft | Mandatory | Optional |
|-------|-----------|----------|
| draft-04 | [![Draft-04 Mandatory](https://img.shields.io/endpoint?url=https://raw.githubusercontent.com/DevInCube/JsonSchema/master/badges/draft4-mandatory-tests.json)](https://github.com/DevInCube/JsonSchema/actions/workflows/ci.yml) | [![Draft-04 Optional](https://img.shields.io/endpoint?url=https://raw.githubusercontent.com/DevInCube/JsonSchema/master/badges/draft4-optional-tests.json)](https://github.com/DevInCube/JsonSchema/actions/workflows/ci.yml) |
| draft-06 | [![Draft-06 Mandatory](https://img.shields.io/endpoint?url=https://raw.githubusercontent.com/DevInCube/JsonSchema/master/badges/draft6-mandatory-tests.json)](https://github.com/DevInCube/JsonSchema/actions/workflows/ci.yml) | [![Draft-06 Optional](https://img.shields.io/endpoint?url=https://raw.githubusercontent.com/DevInCube/JsonSchema/master/badges/draft6-optional-tests.json)](https://github.com/DevInCube/JsonSchema/actions/workflows/ci.yml) |
| draft-07 | [![Draft-07 Mandatory](https://img.shields.io/endpoint?url=https://raw.githubusercontent.com/DevInCube/JsonSchema/master/badges/draft7-mandatory-tests.json)](https://github.com/DevInCube/JsonSchema/actions/workflows/ci.yml) | [![Draft-07 Optional](https://img.shields.io/endpoint?url=https://raw.githubusercontent.com/DevInCube/JsonSchema/master/badges/draft7-optional-tests.json)](https://github.com/DevInCube/JsonSchema/actions/workflows/ci.yml) |
| draft 2019-09 | [![Draft 2019-09 Mandatory](https://img.shields.io/endpoint?url=https://raw.githubusercontent.com/DevInCube/JsonSchema/master/badges/draft2019-09-mandatory-tests.json)](https://github.com/DevInCube/JsonSchema/actions/workflows/ci.yml) | [![Draft 2019-09 Optional](https://img.shields.io/endpoint?url=https://raw.githubusercontent.com/DevInCube/JsonSchema/master/badges/draft2019-09-optional-tests.json)](https://github.com/DevInCube/JsonSchema/actions/workflows/ci.yml) |

## Test suite

Compliance is verified against the [JSON Schema Test Suite](https://github.com/json-schema-org/JSON-Schema-Test-Suite), included as a git submodule at `JSON-Schema-Test-Suite/`. Clone with submodules to run tests locally:

```sh
git clone --recurse-submodules <repo-url>
dotnet run --project My.Json.Schema.TestConsole -c Release
```

[json-schema-home]: http://json-schema.org
