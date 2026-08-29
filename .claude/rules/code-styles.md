## Стиль кода C#

Базовый источник правил — `src/.editorconfig` (действует на весь solution). Ниже — то, что
важно держать в голове при правках именно этого пакета.

### Синтаксис

- File-scoped namespaces.
- `using`-директивы — вне `namespace` (`csharp_using_directive_placement = outside_namespace`);
  System-директивы отдельно не поднимаются (`dotnet_sort_system_directives_first = false`),
  группы не разделяются (`dotnet_separate_import_directive_groups = false`).
- `ImplicitUsings=enable` — не добавляй `using` для того, что уже входит в неявный набор.
  Отдельного `GlobalUsings.cs` в проекте нет (сгенерированный лежит в `obj/`); специфичные
  `using` пиши в самом файле.
- `var`, если тип очевиден из правой части (уровень — `suggestion`).
- Pattern matching и `switch`-выражения предпочтительнее приведений и цепочек `if/else`
  (см. `Predicate` в `AppDefinitionExtensions.cs`).
- `nameof(...)` вместо строковых литералов для имён членов.
- Expression-bodied члены допустимы и уже используются (методы/свойства — `suggestion`,
  локальные функции — `false`).
- Обязательные фигурные скобки: `csharp_prefer_braces = true:error`.

### Nullable

- `<Nullable>enable</Nullable>`. Не глушить предупреждения через `!` без комментария с причиной.

### Модификаторы и структура класса

- Явно указывай модификатор доступа у всех членов
  (`dotnet_style_require_accessibility_modifiers = for_non_interface_members`).
- Порядок модификаторов — `csharp_preferred_modifier_order` из `.editorconfig`
  (`public, private, protected, internal, static, ... , sealed, override, readonly, ... , async`).
- `sealed` по умолчанию для классов, не рассчитанных на наследование
  (`AppDefinitionCollection`, `AppDefinitionItem` — `sealed`).
  `AppDefinition` намеренно открыт — это база для пользовательских определений.
- Приватные поля — `_camelCase` (`dotnet_naming_style.instance_field_style`: префикс `_`,
  camelCase).
- `readonly`-поля (`dotnet_style_readonly_field = true:error`) и авто-свойства
  (`dotnet_style_prefer_auto_properties = true:error`) — уровень `error`, не понижать.
- Порядок членов: приватные `readonly` поля → конструкторы → публичные члены →
  приватные вспомогательные методы → `override`. Для статических классов-расширений
  (`AppDefinitionExtensions`) — публичные методы, приватные хелперы рядом с местом вызова.

### Неизменяемость

- `record` / `readonly record struct` для DTO-подобных носителей данных
  (`AppDefinitionItem` — `sealed record` с позиционными параметрами).

### Обработка ошибок

- Fail fast. На этапе конфигурации приложения допустимо и ожидаемо бросать исключения:
  в коде уже используются `DirectoryNotFoundException` (папка модулей не найдена) и
  `ArgumentNullException` (обязательный ключ конфигурации, см. пример в `README.md`).
- Зависимостей у пакета нет вообще: единственная ссылка — `<FrameworkReference
  Include="Microsoft.AspNetCore.App" />`. Не добавлять NuGet-пакеты в этот лёгкий пакет
  без явной задачи (в т.ч. `Calabonga.Results` и подобные обёртки результата).
- `try-catch` — только для реально исключительных ситуаций, не для управления потоком.
  Существующий паттерн (`AddDefinitions`, `AddDefinitionsWithModules`): обернуть операцию,
  залогировать через `ILogger` и пробросить (`throw;`).

### Логирование

- `ILogger<IAppDefinition>` / `ILogger<AppDefinition>`, тип приходит из framework reference
  `Microsoft.AspNetCore.App`.
- Логгер на этапе конфигурации получают через `builder.Services.BuildServiceProvider()` —
  это одноразовый provider, паттерн намеренный, сохраняй его при правках.
- Structured logging с именованными плейсхолдерами (`{ModuleName}`, `{@items}`,
  `{@ServiceOrderIndex}`).
- Перед дорогими сообщениями проверяй уровень: `logger.IsEnabled(LogLevel.Debug)`
  (в коде так делается везде).

### Чего в этом пакете нет (не тащить в правки без явной задачи)

- Нет `async`/`await` — публичное API синхронное; `CancellationToken` в сигнатурах отсутствует.
- Нет EF Core / БД / `DbContext`, нет MVC-контроллеров, нет Blazor — пакет отвечает только
  за организацию стартового кода (`ConfigureServices` / `ConfigureApplication`).
- Нет WPF/XAML, работы с датами/`TimeProvider`.
- Нет тестового проекта.

### API как контракт

Пакет потребляется как опубликованный NuGet приложениями ASP.NET Core.
Изменение членов `IAppDefinition` / `AppDefinition` или публичных сигнатур
`AppDefinitionExtensions` (`AddDefinitions`, `AddDefinitionsWithModules`, `UseDefinitions`)
и записи `AppDefinitionItem` — breaking change: сопровождай сменой версии и записью
в `README.md`.
