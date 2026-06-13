# DND Helper

Веб-приложение для поддержки D&D 5e: создание и ведение персонажей, игровые комнаты, справочник правил, базовая боевая логика для персонажей и чудовищ.

Проект состоит из:
- `Backend/dnd-helper` - ASP.NET Core 8 backend
- `Frontend/dnd-helper` - React + Vite frontend
- PostgreSQL - операционные данные приложения
- MongoDB - справочник правил, рас, классов, заклинаний, снаряжения и чудовищ

## Возможности

- регистрация и вход через `ASP.NET Core Identity`
- роли пользователя и ведущего
- создание персонажей по правилам D&D 5e
- расчёт характеристик, бонусов, владений, spell slots и snapshot персонажа
- сохранение `calculation_trace` для объяснения вычислений
- комнаты игроков и ведущих
- добавление персонажей в комнаты
- просмотр персонажей внутри комнаты
- добавление чудовищ в комнату и базовые броски атаки/урона
- справочник по расам, классам, предысториям, заклинаниям, снаряжению, состояниям и чудовищам

## Технологии

- Backend:
  - `.NET 8`
  - `ASP.NET Core Minimal API`
  - `Entity Framework Core`
  - `ASP.NET Core Identity`
  - `Npgsql`
  - `MongoDB.Driver`
- Frontend:
  - `React 19`
  - `TypeScript`
  - `Vite`
  - `react-router-dom`
- Databases:
  - `PostgreSQL 16`
  - `MongoDB 7`
- Infra:
  - `Docker Compose`
- Tests:
  - `xUnit`

## Архитектура

Backend приведён к layered/Clean-style структуре. Основная идея:

- `Domain` хранит сущности и состояние предметной области.
- `Application` хранит сценарии использования, сервисы, контракты и порты.
- `Infrastructure` хранит внешние реализации: PostgreSQL, MongoDB, Identity, seeding.
- `Presentation` хранит HTTP endpoints и слой экспонирования API.

Это не "академически чистая" hexagonal-архитектура с полным CQRS и MediatR, но структура уже выстроена так, чтобы:
- доменная логика не лежала в endpoints
- база и Mongo не протекали в UI-слой
- сервисы можно было покрывать тестами
- `Program.cs` оставался composition root, а не "главным файлом на всё"

## Структура репозитория

```text
.
├── Backend/
│   ├── dnd-helper/
│   │   ├── Application/
│   │   │   ├── Auth/
│   │   │   ├── Characters/
│   │   │   ├── Common/
│   │   │   ├── DependencyInjection/
│   │   │   ├── ReferenceData/
│   │   │   ├── Rooms/
│   │   │   └── Rules/
│   │   ├── Domain/
│   │   │   ├── Characters/
│   │   │   └── Rooms/
│   │   ├── Infrastructure/
│   │   │   ├── DependencyInjection/
│   │   │   ├── Identity/
│   │   │   ├── Persistence/
│   │   │   │   ├── Mongo/
│   │   │   │   └── Postgres/
│   │   │   └── Seeding/
│   │   ├── Presentation/
│   │   │   ├── Auth/
│   │   │   ├── Characters/
│   │   │   ├── DependencyInjection/
│   │   │   ├── Equipment/
│   │   │   ├── Monsters/
│   │   │   ├── Rooms/
│   │   │   └── Rules/
│   │   ├── GlobalUsings.cs
│   │   └── Program.cs
│   └── dnd-helper.Tests/
│       └── Features/
├── Frontend/
│   └── dnd-helper/
│       ├── src/
│       │   ├── components/
│       │   ├── pages/
│       │   ├── services/
│       │   ├── types/
│       │   └── utils/
│       └── vite.config.ts
├── docker-compose.yml
└── README_DB.md
```

## Backend по слоям

### `Domain`

Содержит сущности:
- [CharacterEntity.cs](/home/grokes/repos/VKR/Backend/dnd-helper/Domain/Characters/CharacterEntity.cs)
- [EncounterEntity.cs](/home/grokes/repos/VKR/Backend/dnd-helper/Domain/Characters/EncounterEntity.cs)
- [RoomEntity.cs](/home/grokes/repos/VKR/Backend/dnd-helper/Domain/Rooms/RoomEntity.cs)

Здесь лежит состояние, которое хранится в PostgreSQL, и методы преобразования сущностей в DTO.

### `Application`

Содержит бизнес-логику и сценарии использования:

- создание персонажа:
  - [CharacterCreationService.cs](/home/grokes/repos/VKR/Backend/dnd-helper/Application/Characters/CharacterCreationService.cs)
  - [RuleResolutionService.cs](/home/grokes/repos/VKR/Backend/dnd-helper/Application/Characters/RuleResolutionService.cs)
  - [CharacterBuilder.cs](/home/grokes/repos/VKR/Backend/dnd-helper/Application/Characters/CharacterBuilder.cs)
- отдых и магия:
  - [CharacterRestService.cs](/home/grokes/repos/VKR/Backend/dnd-helper/Application/Characters/CharacterRestService.cs)
  - [CharacterSpellService.cs](/home/grokes/repos/VKR/Backend/dnd-helper/Application/Characters/CharacterSpellService.cs)
- комнаты:
  - [RoomAccessService.cs](/home/grokes/repos/VKR/Backend/dnd-helper/Application/Rooms/RoomAccessService.cs)
  - [RoomMonsterService.cs](/home/grokes/repos/VKR/Backend/dnd-helper/Application/Rooms/RoomMonsterService.cs)
- общая игровая утилита:
  - [DiceRoller.cs](/home/grokes/repos/VKR/Backend/dnd-helper/Application/Common/Dice/DiceRoller.cs)
- контракты:
  - [CharacterContracts.cs](/home/grokes/repos/VKR/Backend/dnd-helper/Application/Characters/CharacterContracts.cs)
  - [RoomContracts.cs](/home/grokes/repos/VKR/Backend/dnd-helper/Application/Rooms/RoomContracts.cs)
  - [AuthContracts.cs](/home/grokes/repos/VKR/Backend/dnd-helper/Application/Auth/AuthContracts.cs)
- порт доступа к справочнику:
  - [IRulesCatalogRepository.cs](/home/grokes/repos/VKR/Backend/dnd-helper/Application/Rules/IRulesCatalogRepository.cs)

### `Infrastructure`

Содержит всё, что зависит от внешнего мира:

- PostgreSQL:
  - [AppDbContext.cs](/home/grokes/repos/VKR/Backend/dnd-helper/Infrastructure/Persistence/Postgres/AppDbContext.cs)
- MongoDB:
  - [RuleDocuments.cs](/home/grokes/repos/VKR/Backend/dnd-helper/Infrastructure/Persistence/Mongo/RuleDocuments.cs)
  - [MongoRulesCatalogRepository.cs](/home/grokes/repos/VKR/Backend/dnd-helper/Infrastructure/Persistence/Mongo/MongoRulesCatalogRepository.cs)
  - [MongoDbOptions.cs](/home/grokes/repos/VKR/Backend/dnd-helper/Infrastructure/Persistence/Mongo/MongoDbOptions.cs)
- Identity:
  - [ApplicationUser.cs](/home/grokes/repos/VKR/Backend/dnd-helper/Infrastructure/Identity/ApplicationUser.cs)
- Seeding:
  - [DatabaseInitializer.cs](/home/grokes/repos/VKR/Backend/dnd-helper/Infrastructure/Seeding/DatabaseInitializer.cs)
  - [ApplicationDatabaseSeeder.cs](/home/grokes/repos/VKR/Backend/dnd-helper/Infrastructure/Seeding/ApplicationDatabaseSeeder.cs)
  - [RulesDatabaseSeeder.cs](/home/grokes/repos/VKR/Backend/dnd-helper/Infrastructure/Seeding/RulesDatabaseSeeder.cs)

### `Presentation`

Содержит HTTP API:

- [AuthEndpoints.cs](/home/grokes/repos/VKR/Backend/dnd-helper/Presentation/Auth/AuthEndpoints.cs)
- [CharacterEndpoints.cs](/home/grokes/repos/VKR/Backend/dnd-helper/Presentation/Characters/CharacterEndpoints.cs)
- [RoomEndpoints.cs](/home/grokes/repos/VKR/Backend/dnd-helper/Presentation/Rooms/RoomEndpoints.cs)
- [RuleEndpoints.cs](/home/grokes/repos/VKR/Backend/dnd-helper/Presentation/Rules/RuleEndpoints.cs)
- [LegacyReferenceEndpoints.cs](/home/grokes/repos/VKR/Backend/dnd-helper/Presentation/Rules/LegacyReferenceEndpoints.cs)
- [EquipmentEndpoints.cs](/home/grokes/repos/VKR/Backend/dnd-helper/Presentation/Equipment/EquipmentEndpoints.cs)
- [MonstersEndpoints.cs](/home/grokes/repos/VKR/Backend/dnd-helper/Presentation/Monsters/MonstersEndpoints.cs)

Регистрация слоёв и маршрутов:
- [Application/DependencyInjection/ServiceCollectionExtensions.cs](/home/grokes/repos/VKR/Backend/dnd-helper/Application/DependencyInjection/ServiceCollectionExtensions.cs)
- [Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs](/home/grokes/repos/VKR/Backend/dnd-helper/Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs)
- [Presentation/DependencyInjection/ServiceCollectionExtensions.cs](/home/grokes/repos/VKR/Backend/dnd-helper/Presentation/DependencyInjection/ServiceCollectionExtensions.cs)
- [Presentation/DependencyInjection/EndpointRouteBuilderExtensions.cs](/home/grokes/repos/VKR/Backend/dnd-helper/Presentation/DependencyInjection/EndpointRouteBuilderExtensions.cs)

## Frontend

Frontend находится в `Frontend/dnd-helper`.

Основные части:
- маршрутизация приложения: [App.tsx](/home/grokes/repos/VKR/Frontend/dnd-helper/src/App.tsx)
- авторизация: [AuthProvider.tsx](/home/grokes/repos/VKR/Frontend/dnd-helper/src/components/AuthProvider.tsx)
- страницы:
  - персонажи
  - создание/редактирование персонажа
  - просмотр персонажа
  - комнаты
  - просмотр комнаты
  - приглашение в комнату
  - справочник
  - профиль
  - вход/регистрация

Главные страницы:
- [CharactersPage.tsx](/home/grokes/repos/VKR/Frontend/dnd-helper/src/pages/CharactersPage.tsx)
- [CharacterCreatePage.tsx](/home/grokes/repos/VKR/Frontend/dnd-helper/src/pages/CharacterCreatePage.tsx)
- [CharacterDetailPage.tsx](/home/grokes/repos/VKR/Frontend/dnd-helper/src/pages/CharacterDetailPage.tsx)
- [RoomsPage.tsx](/home/grokes/repos/VKR/Frontend/dnd-helper/src/pages/RoomsPage.tsx)
- [RoomDetailPage.tsx](/home/grokes/repos/VKR/Frontend/dnd-helper/src/pages/RoomDetailPage.tsx)
- [DataCheckPage.tsx](/home/grokes/repos/VKR/Frontend/dnd-helper/src/pages/DataCheckPage.tsx)

Vite proxy:
- [vite.config.ts](/home/grokes/repos/VKR/Frontend/dnd-helper/vite.config.ts)

По умолчанию frontend проксирует `/api` на `http://localhost:5026`.

## Базы данных

### PostgreSQL

Хранит операционные данные:
- пользователи и роли
- персонажи
- комнаты и участников комнат
- состав комнат
- encounter / combatants
- сериализованные snapshot-данные персонажа

### MongoDB

Хранит справочную базу правил:
- `rulesets`
- `races`
- `classes`
- `backgrounds`
- `features`
- `spells`
- `equipment`
- `monsters`
- `conditions`

Подробнее про БД: [README_DB.md](/home/grokes/repos/VKR/README_DB.md)

## Seeding и инициализация

При старте backend:
- подготавливает PostgreSQL
- создаёт Mongo indexes
- добавляет базовый ruleset
- upsert'ит справочные документы в MongoDB
- создаёт demo/test данные приложения в PostgreSQL, если они отсутствуют

Ключевая точка входа:
- [DatabaseInitializer.cs](/home/grokes/repos/VKR/Backend/dnd-helper/Infrastructure/Seeding/DatabaseInitializer.cs)

Это означает:
- после удаления БД проект может восстановить минимальный набор данных сам
- повторный запуск не должен плодить дубликаты

## Основные API-группы

### Auth

- `POST /api/auth/register`
- `POST /api/auth/login`
- `POST /api/auth/logout`
- `GET /api/auth/me`
- `DELETE /api/auth/account`

### Characters

- `GET /api/my/characters`
- `GET /api/characters/{id}`
- `PUT /api/characters/{id}`
- `POST /api/characters`
- `POST /api/characters/{id}/rest`
- `POST /api/characters/{id}/cast-spell`
- `GET /api/characters/{id}/calculation-trace`

### Rooms

- `GET /api/rooms`
- `POST /api/rooms`
- `GET /api/rooms/{id}`
- `POST /api/rooms/join`
- `POST /api/rooms/join/invite`
- операции с персонажами комнаты
- операции с чудовищами комнаты

### Rules / Reference

- `GET /api/rulesets`
- `GET /api/rules/races`
- `GET /api/rules/classes`
- `GET /api/rules/backgrounds`
- `GET /api/rules/features`
- `GET /api/rules/spells`
- `GET /api/rules/conditions`
- `GET /api/equipment`
- `GET /api/monsters`

## Требования для локальной разработки

- `.NET SDK 8`
- `Node.js 20+`
- `npm`
- `Docker` и `Docker Compose` для контейнерного режима

## Запуск

### Вариант 1. Всё в Docker

Из корня репозитория:

```bash
docker compose up --build
```

Адреса:
- frontend: `http://localhost:5173`
- backend: `http://localhost:5026`
- postgres: `localhost:5432`
- mongo: `localhost:27017`

Остановить:

```bash
docker compose down
```

### Вариант 2. Базы в Docker, backend и frontend локально

Запустить только БД:

```bash
docker compose up -d postgres mongo
```

Запустить backend:

```bash
dotnet run --project Backend/dnd-helper/dnd-helper.csproj
```

Запустить frontend:

```bash
cd Frontend/dnd-helper
npm install
npm run dev
```

### Вариант 3. Полностью локально

Нужно вручную поднять:
- PostgreSQL на `localhost:5432`
- MongoDB на `localhost:27017`

После этого:

```bash
dotnet run --project Backend/dnd-helper/dnd-helper.csproj
cd Frontend/dnd-helper
npm install
npm run dev
```

## Конфигурация

Backend development settings:
- [appsettings.Development.json](/home/grokes/repos/VKR/Backend/dnd-helper/appsettings.Development.json)

По умолчанию:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=dnd_helper;Username=dnd;Password=dndpass"
  },
  "MongoDb": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "dnd_helper_rules"
  }
}
```

## Тесты

Тестовый проект:
- [dnd-helper.Tests.csproj](/home/grokes/repos/VKR/Backend/dnd-helper.Tests/dnd-helper.Tests.csproj)

Запуск всех тестов решения:

```bash
dotnet test dnd-helper.sln
```

Сейчас есть тесты для:
- бросков кубов
- отдыха персонажа
- правил доступа к комнатам

## Полезные команды

### Backend

```bash
dotnet build Backend/dnd-helper/dnd-helper.csproj
dotnet run --project Backend/dnd-helper/dnd-helper.csproj
```

### Frontend

```bash
cd Frontend/dnd-helper
npm run dev
npm run build
```

### Tests

```bash
dotnet test dnd-helper.sln
```

### Docker

```bash
docker compose up --build
docker compose down
docker compose up -d postgres mongo
```

## Разработка новых фич

Рекомендуемый порядок:

1. Сначала определить, это `Domain`, `Application`, `Infrastructure` или `Presentation`.
2. Если это сценарий использования, добавлять его в `Application`.
3. Если нужна новая внешняя интеграция или хранилище - в `Infrastructure`.
4. Если это новый HTTP endpoint - в `Presentation`.
5. Если логика нетривиальна, сразу добавлять `xUnit` тест.

Практическое правило:
- endpoint не должен сам считать игровые правила
- игровые правила должны жить в сервисах application-слоя
- infrastructure не должна навязывать свои детали в presentation

## Известные замечания

- В проекте ещё есть исторический слой `CharacterOptionsCatalog` в [CharacterOptions.cs](/home/grokes/repos/VKR/Backend/dnd-helper/Application/ReferenceData/CharacterOptions.cs), который местами помогает presentation/domain DTO и совместимости. Основной источник правил при создании персонажа уже MongoDB, но часть вспомогательной логики всё ещё опирается на этот каталог.
- В репозитории присутствуют собранные артефакты `bin/`, `obj/`, `dist/`, `node_modules/`. Для живой разработки это не критично, но для долгосрочной чистоты репозитория обычно лучше держать их вне version control.
- Файл [dnd-helper.http](/home/grokes/repos/VKR/Backend/dnd-helper/dnd-helper.http) устарел и пока не отражает актуальные маршруты API.

## Куда смотреть в первую очередь

Если нужно быстро понять проект:

- backend composition root: [Program.cs](/home/grokes/repos/VKR/Backend/dnd-helper/Program.cs)
- инициализация БД: [DatabaseInitializer.cs](/home/grokes/repos/VKR/Backend/dnd-helper/Infrastructure/Seeding/DatabaseInitializer.cs)
- создание персонажа: [CharacterCreationService.cs](/home/grokes/repos/VKR/Backend/dnd-helper/Application/Characters/CharacterCreationService.cs)
- правила расчёта: [RuleResolutionService.cs](/home/grokes/repos/VKR/Backend/dnd-helper/Application/Characters/RuleResolutionService.cs)
- комнаты и чудовища: [RoomEndpoints.cs](/home/grokes/repos/VKR/Backend/dnd-helper/Presentation/Rooms/RoomEndpoints.cs), [RoomMonsterService.cs](/home/grokes/repos/VKR/Backend/dnd-helper/Application/Rooms/RoomMonsterService.cs)
- frontend routes: [App.tsx](/home/grokes/repos/VKR/Frontend/dnd-helper/src/App.tsx)

## История документации

- корневая документация проекта: `README.md`
- документация по БД и сидированию: [README_DB.md](/home/grokes/repos/VKR/README_DB.md)
- стандартный Vite README frontend: [Frontend/dnd-helper/README.md](/home/grokes/repos/VKR/Frontend/dnd-helper/README.md)
