# NomadFramework Tests

The test suite is organized first by framework module, then by test kind, then
by the feature area under test. Test files keep their original namespaces and
fixture names so NUnit discovery remains stable while the filesystem layout is
easier to scan.

## Layout

- `Mocks/`, `Util/`, and module-local `Support/`: shared test doubles and
  fixtures.
- `Nomad.*.Tests/Unit/`: fast, isolated coverage for feature areas.
- `Nomad.*.Tests/Integration/`: bootstrap, wrapper, fixture, and cross-service
  coverage.
- `Nomad.*.Tests/Benchmark/`: performance, stress, and measurement-oriented
  coverage.

For example:

- `Nomad.Core.Tests/Unit/Guards/ArgumentGuardTests.cs`
- `Nomad.Events.Tests/Benchmark/SubscriptionSetPerformanceAndMemoryTests.cs`
- `Nomad.Save.Tests/Integration/Public/SaveBootstrapperTests.cs`

## Categories

Fixtures are labeled with:

- Module categories such as `Nomad.Core`, `Nomad.Events`, and `Nomad.Save`.
- Feature categories derived from their folder, such as `Guards`,
  `Streams.Memory`, `Sections.Reading`, or `Lobbies`.
- Test-kind categories such as `Unit`, `Integration`, `Performance`, `Stress`,
  and `Regression`.

Examples:

```powershell
dotnet test --filter "TestCategory=Nomad.Events"
dotnet test --filter "TestCategory=Performance"
dotnet test --filter "TestCategory!=Steam"
```
