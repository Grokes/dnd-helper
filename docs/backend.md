# Backend

Backend `DND Helper` находится в `Backend/dnd-helper` и построен на ASP.NET Core 8 Minimal API.

## Назначение

Backend отвечает за:
- авторизацию и регистрацию пользователей;
- хранение пользователей и ролей;
- создание, обновление и просмотр персонажей;
- расчёт персонажа по правилам D&D 5e;
- хранение комнат и участников;
- управление чудовищами в комнатах;
- предоставление справочника правил frontend-приложению;
- инициализацию PostgreSQL и MongoDB.

## Структура

```text
Backend/dnd-helper
├── Application
├── Domain
├── Infrastructure
├── Presentation
├── GlobalUsings.cs
└── Program.cs
```

## Composition root

`Program.cs` выполняет роль composition root:
- создаёт `WebApplicationBuilder`;
- подключает application, infrastructure и presentation services;
- настраивает middleware;
- запускает database initialization;
- регистрирует endpoints;
- запускает приложение.

## Domain

Путь: `Backend/dnd-helper/Domain`

Domain содержит сущности предметной области.

### Characters

Файлы:
- `CharacterEntity.cs`
- `EncounterEntity.cs`

`CharacterEntity` хранит:
- владельца персонажа;
- выбранные race/class/background identifiers;
- имя, уровень, мировоззрение и заметки;
- рассчитанные показатели;
- базовые характеристики;
- выбранные опции;
- навыки;
- заклинания;
- инвентарь;
- spell slots;
- current/max hit points;
- computed snapshot;
- calculation trace.

`EncounterEntity` описывает боевую сцену и участников боя.

### Rooms

Файл:
- `RoomEntity.cs`

Комната хранит:
- владельца;
- название;
- invite token;
- участников;
- выбранных персонажей участников;
- чудовищ комнаты.

## Application

Путь: `Backend/dnd-helper/Application`

Application содержит сценарии приложения, сервисы расчётов и контракты.

### Auth

Файл:
- `AuthContracts.cs`

Содержит request/response контракты для регистрации, входа и текущего пользователя.

### Characters

Файлы:
- `CharacterContracts.cs`
- `CharacterCreationService.cs`
- `CharacterBuilder.cs`
- `RuleResolutionService.cs`
- `CharacterRestService.cs`
- `CharacterSpellService.cs`
- `UseCases/CreateCharacterUseCase.cs`
- `UseCases/UpdateCharacterUseCase.cs`
- `UseCases/RestCharacterUseCase.cs`
- `UseCases/CastCharacterSpellUseCase.cs`

Модуль персонажей выполняет:
- загрузку правил из MongoDB;
- проверку выбранных race/class/background;
- применение grants, modifiers и choices;
- расчёт итоговых характеристик;
- расчёт хитов, скорости, навыков, спасбросков и spell slots;
- создание computed snapshot;
- создание calculation trace;
- сохранение персонажа в PostgreSQL;
- обновление персонажа;
- отдых;
- применение заклинаний.

### Rooms

Файлы:
- `RoomContracts.cs`
- `RoomAccessService.cs`
- `RoomMonsterService.cs`

Модуль комнат выполняет:
- проверку доступа пользователя к комнате;
- определение роли участника;
- работу с чудовищами комнаты;
- расчёт атаки чудовища;
- расчёт урона чудовища;
- применение урона по персонажу или чудовищу.

### Rules

Файл:
- `IRulesCatalogRepository.cs`

Это application-порт доступа к справочнику правил. Реализация находится в Infrastructure и использует MongoDB.

### Common

Файлы:
- `Common/Dice/DiceRoller.cs`
- `Common/UseCases/UseCaseResult.cs`

`DiceRoller` отвечает за безопасный разбор и выполнение dice expressions.

`UseCaseResult` унифицирует успешные результаты, validation errors, forbidden и not found responses.

## Infrastructure

Путь: `Backend/dnd-helper/Infrastructure`

Infrastructure содержит реализации внешних зависимостей.

### PostgreSQL

Файл:
- `Persistence/Postgres/AppDbContext.cs`

`AppDbContext` хранит:
- Identity users;
- Identity roles;
- characters;
- rooms;
- room members;
- selected room characters;
- room monsters;
- encounters;
- encounter combatants.

### MongoDB

Файлы:
- `Persistence/Mongo/RuleDocuments.cs`
- `Persistence/Mongo/MongoRulesCatalogRepository.cs`
- `Persistence/Mongo/MongoDbOptions.cs`

MongoDB используется для справочника правил.

`RuleDocuments.cs` содержит документы:
- `RulesetDocument`
- `RaceDocument`
- `ClassDocument`
- `BackgroundDocument`
- `FeatureDocument`
- `SpellDocument`
- `EquipmentDocument`
- `MonsterDocument`
- `ConditionDocument`

### Identity

Файл:
- `Identity/ApplicationUser.cs`

`ApplicationUser` расширяет пользователя ASP.NET Core Identity и используется для авторизации, владения персонажами и членства в комнатах.

### Seeding

Файлы:
- `Seeding/DatabaseInitializer.cs`
- `Seeding/ApplicationDatabaseSeeder.cs`
- `Seeding/RulesDatabaseSeeder.cs`
- `Seeding/RulesDatabaseSeeder.Conditions.cs`

Инициализация выполняется при старте backend. PostgreSQL подготавливается через EF Core, MongoDB получает индексы и справочные документы.

## Presentation

Путь: `Backend/dnd-helper/Presentation`

Presentation содержит Minimal API endpoints.

### Auth

Файл:
- `Auth/AuthEndpoints.cs`

Endpoints:
- регистрация;
- вход;
- выход;
- текущий пользователь;
- удаление аккаунта.

### Characters

Файл:
- `Characters/CharacterEndpoints.cs`

Endpoints:
- список личных персонажей;
- создание персонажа;
- просмотр персонажа;
- обновление персонажа;
- отдых;
- применение заклинания;
- calculation trace.

### Rooms

Файл:
- `Rooms/RoomEndpoints.cs`

Endpoints:
- список комнат;
- создание комнаты;
- подключение к комнате;
- подключение по invite token;
- просмотр комнаты;
- выбор персонажей комнаты;
- обновление роли участника;
- presence;
- управление чудовищами;
- атака чудовищем.

### Rules, Equipment, Monsters

Файлы:
- `Rules/RuleEndpoints.cs`
- reference endpoints for character options.
- `Equipment/EquipmentEndpoints.cs`
- `Monsters/MonstersEndpoints.cs`

Endpoints предоставляют frontend-приложению справочник правил, снаряжение и чудовищ.

### Common

Файл:
- `Common/UseCaseResultHttpMapper.cs`

Преобразует application-level `UseCaseResult` в HTTP responses.

## Dependency Injection

DI разнесён по слоям:
- `Application/DependencyInjection/ServiceCollectionExtensions.cs`
- `Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs`
- `Presentation/DependencyInjection/ServiceCollectionExtensions.cs`
- `Presentation/DependencyInjection/EndpointRouteBuilderExtensions.cs`

## Запуск backend

```bash
dotnet run --project Backend/dnd-helper/dnd-helper.csproj
```

Swagger:

```text
http://localhost:5026/swagger
```

## Тесты backend

```bash
dotnet test dnd-helper.sln
```
