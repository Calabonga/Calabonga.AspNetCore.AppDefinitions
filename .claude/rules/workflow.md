## Правила рабочего процесса

### Git

- Перед изменениями создавай отдельную ветку. Префиксы: `feature/`, `bugfix/`, `hotfix/`,
  `chore/`, `docs/`.
- Коммиты — Conventional Commits: `type: description`
  (`feat, fix, refactor, test, docs, style, perf, build, chore, revert`).
  Для веток `bugfix/` и `hotfix/` тип коммита — `fix:`.
- Атомарные коммиты: одно логическое изменение на коммит.
- `main` защищён workflow: любой push в `main` публикует пакет на nuget.org
  (`.github/workflows/main.yml`). Не пушить в `main` напрямую — только через PR.

### Перед коммитом

- `dotnet build src/Calabonga.AspNetCore.AppDefinitions.slnx -c Release` должен проходить
  без ошибок и новых предупреждений (файл решения в формате SLNX, не `.sln`).
- Тестового проекта в репозитории нет — `dotnet test` не запускается. Если добавляешь тесты,
  заводи отдельный проект в `src/` и подключай его к `Calabonga.AspNetCore.AppDefinitions.slnx`.
- При создании нового класса проверь, нет ли уже файла/типа с таким именем в решении:
  дедупликация определений в `AppDefinitionCollection.GetDistinct()` идёт по простому имени
  типа (`DistinctBy`), коллизии имён приводят к молчаливому отбрасыванию определений.

### Изменения публичного API

- `IAppDefinition`, `AppDefinition`, `AppDefinitionExtensions`, `AppDefinitionItem` —
  публичный контракт пакета (`AppDefinitionCollection` — `internal`). Менять их сигнатуры
  только по явной задаче.
- Любое breaking-изменение сопровождай:
  1. повышением `<Version>` в
     `src/Calabonga.AspNetCore.AppDefinitions/Calabonga.AspNetCore.AppDefinitions.csproj`
     по SemVer (сейчас версия следует за версией платформы: `10.0.0` → net10.0);
  2. записью в разделе «Что нового» в `README.md`;
  3. обновлением `PackageReleaseNotes` в `.csproj`.

### Релиз

- Публикация автоматическая: merge в `main` → workflow делает
  `dotnet build --configuration Release` (пакет собирается за счёт `GeneratePackageOnBuild`)
  и `dotnet nuget push --skip-duplicate` на nuget.org.
- Значит версию в `.csproj` нужно поднять **в той же ветке/PR**, что несёт изменения, иначе
  push с той же версией будет пропущен (`--skip-duplicate`).

### Локальная проверка потребителями

- Downstream-код (приложения ASP.NET Core) видит только опубликованный NuGet. Для локальной
  проверки — `dotnet pack` + локальный feed или bump версии с pre-release-суффиксом.
