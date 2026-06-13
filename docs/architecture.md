# Архитектура проекта

`DND Helper` построен как веб-приложение с разделением на backend, frontend и две базы данных. Архитектура ориентирована на явные границы между пользовательским интерфейсом, сценариями приложения, предметной областью, хранилищами данных и справочником правил.

## Архитектурные принципы

- Backend разделён на слои `Domain`, `Application`, `Infrastructure` и `Presentation`.
- HTTP endpoints выполняют роль транспортного слоя и делегируют бизнес-сценарии application-сервисам.
- PostgreSQL хранит операционное состояние приложения.
- MongoDB хранит справочник правил D&D 5e.
- Frontend организован по Feature-Sliced Design.
- Расчёты персонажа сохраняются вместе с объясняющим `calculation_trace`.
- Справочные данные создаются при запуске приложения идемпотентным seeding-механизмом.

## Backend

Backend находится в `Backend/dnd-helper` и использует Clean-style layered architecture.

```text
Backend/dnd-helper
├── Domain
├── Application
├── Infrastructure
├── Presentation
├── GlobalUsings.cs
└── Program.cs
```

### `Domain`

`Domain` содержит предметные сущности и состояние приложения:
- `CharacterEntity`
- `RoomEntity`
- `EncounterEntity`

Сущности описывают данные персонажей, комнат и боевых сцен. Они соответствуют операционному состоянию, сохраняемому в PostgreSQL.

### `Application`

`Application` содержит сценарии приложения, сервисы расчётов, контракты и порты.

Основные модули:
- `Characters` - создание, обновление, отдых, применение заклинаний и расчёт персонажа.
- `Rooms` - доступ к комнатам, участники, чудовища и атаки.
- `Rules` - контракт доступа к справочнику правил.
- `Common` - общие application-компоненты, включая dice roller и use case result.

Use cases инкапсулируют операции, меняющие состояние приложения:
- создание персонажа;
- обновление персонажа;
- отдых персонажа;
- применение заклинания.

### `Infrastructure`

`Infrastructure` содержит реализации внешних зависимостей:
- EF Core контекст PostgreSQL;
- MongoDB repository справочника правил;
- ASP.NET Identity user;
- database initializer;
- application/rules seeders;
- DI-регистрацию инфраструктурного слоя.

### `Presentation`

`Presentation` содержит HTTP API:
- authentication endpoints;
- character endpoints;
- room endpoints;
- rules endpoints;
- equipment endpoints;
- monster endpoints.

Presentation получает HTTP-запросы, вызывает application-сценарии и возвращает HTTP-ответы.

## Frontend

Frontend находится в `Frontend/dnd-helper` и организован по Feature-Sliced Design.

```text
src
├── app
├── pages
├── widgets
├── features
├── entities
├── shared
├── types
└── utils
```

### Слои frontend

- `app` - корень приложения, layout, маршрутизация.
- `pages` - route-level страницы.
- `widgets` - крупные UI-блоки страниц.
- `features` - пользовательские сценарии, API и model hooks.
- `entities` - предметные сущности и их UI.
- `shared` - общая инфраструктура.
- `types` - общие TypeScript-типы API.
- `utils` - общие функции отображения и форматирования.

### API frontend

API-функции разнесены по feature-модулям:
- `features/auth/api/authApi.ts`
- `features/characters/api/charactersApi.ts`
- `features/rooms/api/roomsApi.ts`
- `features/rules/api/rulesApi.ts`

Все HTTP-запросы проходят через `shared/api/http.ts`, где настроены credentials и обработка ошибок API.

## Разделение PostgreSQL и MongoDB

### PostgreSQL

PostgreSQL хранит операционные данные:
- пользователей и роли;
- персонажей;
- комнаты;
- участников комнат;
- выбранных персонажей в комнатах;
- чудовищ в комнатах;
- боевые сцены;
- текущее состояние персонажей.

### MongoDB

MongoDB хранит справочник правил:
- rulesets;
- races;
- classes;
- backgrounds;
- features;
- spells;
- equipment;
- monsters;
- conditions.

Документы правил содержат вложенные `grants`, `requires`, `choices`, `effects`, `modifiers` и `levels`, что позволяет описывать игровые правила как структурированные шаблоны.

## Создание персонажа

Создание персонажа проходит через backend:

1. Frontend отправляет выбранные race/class/background, базовые характеристики и выбранные опции.
2. Backend загружает справочные правила из MongoDB.
3. `RuleResolutionService` собирает применимые правила.
4. `CharacterBuilder` рассчитывает итоговые характеристики, владения, хиты, скорость, spell slots и другие derived values.
5. Backend формирует `computed_snapshot` и `calculation_trace`.
6. Персонаж сохраняется в PostgreSQL и привязывается к пользователю.

## Calculation trace

`calculation_trace` хранит объяснение расчётов персонажа.

Запись содержит:
- `target` - рассчитываемое значение;
- `source` - источник правила;
- `reason` - объяснение применения;
- `value` - числовое значение;
- `operation` - операция.

Такой формат позволяет показывать, почему итоговая характеристика, навык, класс доспеха или другой показатель получил конкретное значение.

## Seeding

Инициализация данных выполняется при старте backend-приложения.

`DatabaseInitializer`:
- подготавливает PostgreSQL;
- создаёт индексы MongoDB;
- запускает seeding операционных данных;
- запускает seeding справочника правил.

MongoDB seed использует upsert по стабильным ключам `rulesetId + slug`. PostgreSQL seed создаёт demo/test записи только при их отсутствии.

## Безопасность и доступ

- Авторизация реализована через ASP.NET Core Identity.
- Персонажи принадлежат пользователям.
- Пользователь видит собственных персонажей.
- В комнате участники могут просматривать персонажей, добавленных другими участниками.
- Редактирование персонажа доступно владельцу персонажа.
- Управление чудовищами в комнате относится к роли ведущего.
