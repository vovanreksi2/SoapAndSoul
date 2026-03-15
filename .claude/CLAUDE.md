# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Build the solution
dotnet build SoupAndSoupApp.sln

# Run the desktop app
dotnet run --project SoupAndSoupApp.Desktop

# Publish
dotnet publish SoupAndSoupApp.sln

# EF Core migrations (run from repo root, targeting SoupAndSoup.Data)
dotnet ef migrations add <MigrationName> --project SoupAndSoup.Data --startup-project SoupAndSoupApp.Desktop
dotnet ef database update --project SoupAndSoup.Data --startup-project SoupAndSoupApp.Desktop
```

VS Code tasks are configured in `.vscode/tasks.json`: `build`, `publish`, `watch`.

There are no test projects currently in this repository.

## Architecture

### Projects

| Project | Purpose |
|---|---|
| `SoupAndSoupApp` | Core UI library: Views (AXAML), ViewModels, Helpers, Coordinators |
| `SoupAndSoupApp.Desktop` | Desktop entry point (WinExe); hosts `IHost` and boots Avalonia |
| `SoupAndSoupApp.Browser` | WebAssembly target |
| `SoupAndSoupApp.Android` | Android target |
| `SoupAndSoup.Data` | EF Core entities, `SoapAndSoulContext`, migrations, repositories, services |
| `SoapAndSoul.Infrastructure` | External services: Azure Blob Storage, OpenTelemetry instrumentation |

Note: the infrastructure folder on disk is `SoapAndSoul.Infastructure` (typo), but the csproj and namespace use the correct spelling `SoapAndSoul.Infrastructure`.

All projects target `net10.0`. `Directory.Build.props` enforces `Nullable=enable` and pins `AvaloniaVersion=11.3.9`.

### Key Dependencies

- **Avalonia 11.3.9** with compiled bindings (`AvaloniaUseCompiledBindingsByDefault=true`)
- **ReactiveUI** via `Avalonia.ReactiveUI` — MVVM framework
- **DynamicData** — reactive collection management (`SourceCache` pipelines)
- **FuzzySharp** — fuzzy string matching (used for search)
- **Material.Icons.Avalonia** — icon set
- **EF Core 10.0.5** with SQL Server provider
- **OpenTelemetry** with Azure Monitor exporter

### Startup & Dependency Injection

`SoupAndSoupApp.Desktop/Program.cs` uses `Host.CreateDefaultBuilder` (GenericHost). Database migration (`Database.Migrate()`) runs once at startup in `Program.cs`. Services are wired via extension methods:

- `MainServiceCollectionExtensions.AddSoapAndSoulAppServices()` — UI, ViewModels, dialogs, caching, calculators, mappers, coordinators, rule engine, image service
- `DatabaseServiceCollectionExtensions.AddSoupAndSoulDbServices()` — `SoapAndSoulContext` factory (Singleton), repositories, data services
- `InfrastructureServiceCollectionExtensions.AddInfraSoapAndSoulServices()` — Azure Blob Storage, `InstrumentationOpenTelemetry`

OpenTelemetry (tracing, metrics, logging) is configured inline in `Program.cs` with `AddSource("SoapAndSoul")`. Azure Monitor connection string resolves from: `config["AzureMonitor:ConnectionString"]` → `config["APPLICATIONINSIGHTS_CONNECTION_STRING"]` → environment variable.

After the host is built, `IServiceProvider` is passed into `App` via `app.InjectServiceProvider(host.Services)` in the `AfterSetup` callback. `App.axaml.cs` resolves `MainWindow`/`MainViewModel` from the container. Browser and Android targets skip GenericHost entirely.

### ViewModel Hierarchy

```
MainViewModel (Singleton) — shell, navigation between cosmetic types
  └─ SoapDesignerViewModel (Transient, one per cosmetic type) — central VM, SourceCache pipelines
       ├─ RecipeListViewModel — recipe list, selection
       ├─ RecipeEditorViewModel — recipe editing, auto-save, component pipeline
       └─ ComponentLibraryViewModel — component grid, grouped by type
```

`MainViewModel` creates `SoapDesignerViewModel` instances via `IDesignerViewModelFactory`. Each designer VM owns its sub-VMs and shares a `SourceCache<ComponentModel, int>` between the editor and library views.

### DI Lifetimes

- **Singleton:** `MainWindow`, `MainViewModel`, all data services (`IRecipeService`, etc.), `MeasureTypeCache`, `IDialogService`, `INotificationService`, `IActiveViewModelRegistry`
- **Transient:** `SoapDesignerViewModel`, all coordinators (`RecipeCoordinator`, `ComponentCoordinator`, `DesignerDataLoader`)

### MVVM Pattern

- ViewModels inherit `ViewModelBase` → `ReactiveObject` (ReactiveUI) with a `CompositeDisposable Disposables` for subscription lifetimes
- Views are plain `UserControl`/`Window` subclasses (not `ReactiveUserControl<T>`) with `x:DataType` for compiled bindings
- DataContext is assigned externally via DI or property setting
- `SoapDesignerViewModel` is the central VM — uses `DynamicData.SourceCache` for reactive in-memory stores with filter/sort/group pipelines, but delegates business logic to coordinators (see below)
- `RecipeModel` auto-tracks dirty state via `this.Changed.Throttle(DirtyTrackingThrottle)` with `BeginInit()`/`EndInit()` to suppress during loading

### Coordinator Pattern

`SoapDesignerViewModel` delegates domain operations to three transient coordinators, keeping the VM focused on reactive pipelines and UI state:

- **`RecipeCoordinator`** — Recipe CRUD, save, delete, entity-to-model mapping via `IRecipeMapper`, image synchronization with compensating transactions
- **`ComponentCoordinator`** — Component add/edit/delete, applies `IComponentSelectionRuleEngine` for default amounts, manages cache updates with compensating transactions on failure
- **`DesignerDataLoader`** — Bulk loading of component types, components, and recipes at initialization with OpenTelemetry activity tagging

### Mappers and Rule Engine

- **`IRecipeMapper` / `RecipeMapper`** — Maps between `Recipe` entities and `RecipeModel` view models
- **`IComponentMapper` / `ComponentMapper`** — Maps between `Component` entities and `ComponentModel` view models
- **`IComponentSelectionRuleEngine`** — Chain-of-responsibility pattern using `IComponentSelectionRule` implementations. Five concrete rules (`FormSelectionRule`, `EssentialOilSelectionRule`, `FragranceOilSelectionRule`, `CraftingBaseSelectionRule`, `DefaultAmountSelectionRule`) determine default amounts when adding a component to a recipe based on component type

### Helper Utilities

- **`DesignerActivityHelper`** — Static helper providing `RunWithActivity()` for OpenTelemetry spans, `ExecuteCompensatingTransaction()` for execute/rollback patterns, and `NotifyResult()` for converting success/failure to toast notifications
- **`IImageService` / `ImageService`** — Extracted blob upload/download/delete operations for component and recipe images

### Auto-Save

ViewModels that need to persist state implement `IAutoSaveCandidate` (`ShouldSave()` / `SaveIfNeededAsync()`). `IActiveViewModelRegistry` tracks them via `ConcurrentDictionary`. Registration happens in `DesignerViewModelFactory.CreateDesignerViewModel()`.

Save triggers:
- **On close:** `MainWindow.Closing` is intercepted in `App.axaml.cs`; cancels close, saves all candidates, waits 2 seconds, then force-closes. If any save fails, the window stays open.
- **On navigate:** `MainViewModel.SelectedNavigationItem` setter saves the current VM before switching.

### Data Layer

- Generic `RepositoryBase<T>` wraps every operation in a fresh `DbContext` via `IDbContextFactory` (safe for Singleton services)
- All services (`IRecipeService`, `IComponentService`, `IComponentTypeService`, `IMeasureTypesService`) are Singleton-lifetime
- `Recipe` and `Component` entities use `IsActive` soft-delete query filter
- `RecipeComponent` is an explicit many-to-many join table with composite PK `(RecipeId, ComponentId)`
- `ComponentType` has many-to-many relationships with both `MeasureType` (use/buy) and `CosmeticType` via explicit join tables
- Seed data is defined in `SeedData` with Ukrainian strings

### Key Models

- **`RecipeModel`** — Contains `SourceCache<ComponentByRecipeModel, int> SelectedComponents` for reactive per-recipe ingredient tracking. Implements `IDisposable` with `CompositeDisposable`
- **`ComponentModel`** — Includes `IncreaseAmountCommand`/`DecreaseAmountCommand`, `IsInCurrentRecipe` state, and image properties via `IImageService`
- **`ComponentByRecipeModel`** — Lightweight model representing a component selection within a recipe (ComponentId, Amount, Component reference)

### View Hierarchy

```
MainWindow.axaml
  ├─ Navigation sidebar (Soap / Parfum icons)
  └─ ContentControl → SoapDesignerView.axaml
       ├─ RecipeListView (left panel)
       ├─ RecipeEditorView (center)
       └─ ComponentLibraryView (right, grouped by ComponentType)
```

Custom controls: `SoapToast` (auto-dismiss notifications), `ImagePickerControl`, `EditableTextBlock`, `IngredientItemsControl`, `RecipeMetricsUserControl`.

### Dialog Service

`IDialogService` / `DialogService` handles the add/edit ingredient modal (`AddIngredientDialog`). Uses ReactiveUI `Interaction<TInput, TOutput>` for confirm/cancel with a `TaskCompletionSource` pattern to await modal results.

### Notification System

`INotificationService` uses `Subject<DomainNotificationType>`. `MainViewModel` subscribes on `RxApp.MainThreadScheduler`. `SoapToast` custom control shows auto-dismissing toast notifications (2-second display with Success/Error/Warning styles).

### Configuration & Secrets

Local development uses `appsettings.Development.json`:
- `DatabaseSettings:ConnectionString` — SQL Server LocalDB (`SoapAndSoulDb_new`)
- `DatabaseSettings:IsAzureDb` — `false` for local, `true` for Azure SQL
- `AzureMonitor:ConnectionString` — Application Insights
- `AzureBlobStorageSettings:SasToken` — Azure Blob Storage SAS token (placeholder in config, real value in user secrets)

Sensitive values (production connection strings, SAS tokens, instrumentation keys) are stored in user secrets (`dotnet user-secrets`), not committed. UserSecretsId: `5a75ae89-60d3-4708-8ccf-376375e1647b`.

### Domain

The app manages cosmetic recipes (soap, perfume) with Ukrainian UI strings. Core entities: `Recipe`, `Component` (ingredients with cost), `ComponentType`, `MeasureType`, `CosmeticType`, `RecipeComponent` (many-to-many with quantity), `ComponentImage`, `RecipeImage`. The `IUnitCostCalculator` service computes per-unit ingredient costs with special handling for Form (÷100) and Drop (×20 drops/ml) measure types.
