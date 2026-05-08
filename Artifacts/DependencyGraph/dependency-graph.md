# NomadFramework Dependency Graph

| Module | Depends On |
|---|---|
| `Nomad.Audio` | `Nomad.Core`, `Nomad.CVars`, `Nomad.Events` |
| `Nomad.Audio.FMOD` | `Nomad.Audio`, `Nomad.ResourceCache` |
| `Nomad.Console` | `Nomad.Core`, `Nomad.CVars`, `Nomad.Events`, `Nomad.Logger` |
| `Nomad.Core` |  |
| `Nomad.CVars` | `Nomad.Core`, `Nomad.Events` |
| `Nomad.EngineTemplates` | `Nomad.Core`, `Nomad.Events` |
| `Nomad.EngineUtils.Godot` | `Nomad.Console`, `Nomad.Core`, `Nomad.CVars`, `Nomad.EngineTemplates`, `Nomad.Events`, `Nomad.ResourceCache` |
| `Nomad.EngineUtils.Settings` | `Nomad.Audio`, `Nomad.Core`, `Nomad.CVars`, `Nomad.Events` |
| `Nomad.EngineUtils.Unity` | `Nomad.Core`, `Nomad.CVars`, `Nomad.Events` |
| `Nomad.Events` | `Nomad.Core` |
| `Nomad.FileSystem` | `Nomad.Core`, `Nomad.CVars`, `Nomad.Events`, `Nomad.Logger` |
| `Nomad.Input` | `Nomad.Core`, `Nomad.CVars`, `Nomad.Events`, `Nomad.Logger` |
| `Nomad.Logger` | `Nomad.Core` |
| `Nomad.OnlineServices.Steam` | `Nomad.Core`, `Nomad.CVars`, `Nomad.Events`, `Nomad.Logger` |
| `Nomad.ResourceCache` | `Nomad.Core` |
| `Nomad.Save` | `Nomad.Core`, `Nomad.CVars`, `Nomad.Events` |
| `Nomad.SourceGenerators` |  |
