# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Правила проекта

- @rules/code-styles.md — стиль кода C# (основано на `src/.editorconfig`).
- @rules/workflow.md — Git, порядок перед коммитом, изменения публичного API, релиз.

## Обзор

`Calabonga.AspNetCore.AppDefinitions` — небольшая NuGet-библиотека, которая позволяет разбить стартовый
код `Program.cs` приложения ASP.NET Core на упорядоченные и отключаемые единицы («определения»,
definitions), а также при необходимости загружать их из внешних модулей-DLL (система плагинов). В
репозитории находится **только библиотека** — без примера приложения и без тестов. Версия библиотеки
следует за версией платформы .NET (10.0.0 → net10.0).

## Команды

Файл решения в новом формате SLNX: `src/Calabonga.AspNetCore.AppDefinitions.slnx`.

```bash
dotnet restore src/Calabonga.AspNetCore.AppDefinitions.slnx
dotnet build src/Calabonga.AspNetCore.AppDefinitions.slnx -c Release
```

`GeneratePackageOnBuild` установлен в `true`, поэтому сборка в конфигурации Release создаёт `.nupkg`
(и `.snupkg` с символами). CI (`.github/workflows/main.yml`) запускается при push в `main`: сборка на
`windows-latest` с .NET 10.0.x и `dotnet nuget push` на nuget.org с использованием секрета
`NUGET_API_KEY`. При выпуске новой версии нужно поднять `<Version>` в `.csproj` и добавить раздел
«Что нового» в `README.md`.

## Архитектура

Пять файлов исходного кода в `src/Calabonga.AspNetCore.AppDefinitions/`:

- **`IAppDefinition` / `AppDefinition`** — контракт, который реализуют потребители. Переопределяются
  `ConfigureServices(WebApplicationBuilder)` и/или `ConfigureApplication(WebApplication)`. Свойства:
  `ServiceOrderIndex` и `ApplicationOrderIndex` (отдельные ключи сортировки для двух фаз, по умолчанию 0),
  `Enabled` (по умолчанию `true`), `Exported` (по умолчанию `false` — экспортируется ли это определение,
  когда сборка используется как модуль).
- **`AppDefinitionExtensions`** — публичное API, всё в виде методов расширения:
  - `AddDefinitions(this WebApplicationBuilder, params Type[] entryPointsAssembly)` — через рефлексию
    перебирает `ExportedTypes` сборки каждого типа — точки входа, создаёт экземпляр каждого неабстрактного
    `AppDefinition` через `Activator.CreateInstance`, выполняет `ConfigureServices` в порядке
    `ServiceOrderIndex` и регистрирует `AppDefinitionCollection` как singleton.
  - `AddDefinitionsWithModules(this WebApplicationBuilder, string modulesFolderPath, params Type[])` — то же
    самое, плюс сканирует `*.dll` в папке модулей, загружает каждую через `Assembly.LoadFile` и добавляет
    определения, у которых `Enabled && Exported`, после чего делегирует в `AddDefinitions`.
  - `UseDefinitions(this WebApplication)` — получает singleton-коллекцию и выполняет `ConfigureApplication`
    в порядке `ApplicationOrderIndex`.
- **`AppDefinitionCollection`** (internal) — хранит найденные `AppDefinitionItem` и имена точек входа.
  `GetEnabled()` фильтрует по `Enabled` и сортирует по `ServiceOrderIndex`; `GetDistinct()` дополнительно
  устраняет дубликаты по имени типа (`DistinctBy`) — именно так схлопываются дублирующиеся определения,
  найденные в разных модулях-DLL.
- **`AppDefinitionItem`** — запись `(IAppDefinition Definition, string AssemblyName, bool Enabled, bool Exported)`.

### Что важно знать

- Двухфазная модель: **все** `ConfigureServices` выполняются во время `AddDefinitions*` (на этапе builder),
  **все** `ConfigureApplication` выполняются во время `UseDefinitions` (после `builder.Build()`). Два индекса
  порядка независимы друг от друга.
- Методы расширения вызывают `builder.Services.BuildServiceProvider()`, чтобы получить `ILogger` до того,
  как контейнер окончательно собран. В этом коде так сделано намеренно; при правках сохраняйте этот подход,
  но помните, что здесь создаётся одноразовый provider.
- Диагностическое логирование зависит от уровня: `Information` печатает сводку и счётчики применённых
  определений; `Debug` дополнительно перечисляет каждое определение с его индексом порядка, состоянием
  enabled/disabled, пометкой `(exportable)` и все определения, которые были пропущены (например,
  из-за устранения дубликатов).
- `Predicate` (фильтр определений) выбирает неабстрактные типы, не являющиеся интерфейсами и приводимые к
  `AppDefinition`.
- Целевая платформа — только `net10.0`; `FrameworkReference` на `Microsoft.AspNetCore.App`. Устаревшие
  папки `obj/` для net6.0/8.0/9.0 остались от прежнего мультитаргетинга — их можно игнорировать.
- `README.md` написан на русском языке и упаковывается в NuGet-пакет как readme пакета.
