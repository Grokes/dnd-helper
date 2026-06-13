# DND Helper

`DND Helper` - веб-приложение для ведения D&D 5e кампаний: создание персонажей, просмотр листов, игровые комнаты, справочник правил, работа с заклинаниями, инвентарём, чудовищами и бросками кубов.

Проект состоит из:
- `Backend/dnd-helper` - ASP.NET Core 8 backend.
- `Frontend/dnd-helper` - React + TypeScript + Vite frontend.
- `Backend/dnd-helper.Tests` - xUnit tests.
- `PostgreSQL` - операционные данные приложения.
- `MongoDB` - справочник правил и шаблоны игровых сущностей.

## Возможности

- Регистрация, вход и сохранение состояния авторизации через `ASP.NET Core Identity`.
- Роли пользователя и ведущего.
- Создание и редактирование персонажа по правилам D&D 5e.
- Расчёт характеристик, модификаторов, класса доспеха, хитов, владений, ячеек заклинаний и боевых показателей.
- Хранение `computed_snapshot` и `calculation_trace` для объяснения расчётов.
- Личные персонажи пользователя без общего публичного списка.
- Игровые комнаты с несколькими участниками.
- Добавление нескольких персонажей пользователя в комнату.
- Просмотр персонажей других игроков внутри комнаты.
- Чудовища в комнате: добавление, удаление, получение урона, атаки по игрокам, броски урона.
- Подробный лист персонажа с бросками характеристик, навыков, спасбросков, инициативы, атаки и урона.
- Справочник рас, классов, предысторий, заклинаний, снаряжения, чудовищ и состояний.

## Технологии

- Backend: `.NET 8`, `ASP.NET Core Minimal API`, `Entity Framework Core`, `ASP.NET Core Identity`, `Npgsql`, `MongoDB.Driver`.
- Frontend: `React 19`, `TypeScript`, `Vite`, `react-router-dom`.
- Databases: `PostgreSQL`, `MongoDB`.
- Infra: `Docker Compose`.
- Tests: `xUnit`.

## Быстрый старт

### Вариант 1. Всё в Docker

```bash
docker compose up --build
```

Адреса по умолчанию:
- Frontend: `http://localhost:5173`
- Backend: `http://localhost:5026`
- Swagger: `http://localhost:5026/swagger`

Остановить:

```bash
docker compose down
```

### Вариант 2. Базы в Docker, backend и frontend локально

```bash
docker compose up -d postgres mongo
dotnet run --project Backend/dnd-helper/dnd-helper.csproj
cd Frontend/dnd-helper
npm run dev -- --host 0.0.0.0
```

Подробнее: [docs/runbook.md](docs/runbook.md).

## Основные команды

```bash
dotnet build dnd-helper.sln
dotnet test dnd-helper.sln
cd Frontend/dnd-helper
npm run build
```

## Документация

- [docs/architecture.md](docs/architecture.md) - общая архитектура, границы слоёв и принятые решения.
- [docs/backend.md](docs/backend.md) - backend-модули, use cases, endpoints и seeding.
- [docs/frontend.md](docs/frontend.md) - frontend-архитектура FSD, слои, модули и правила разбиения UI.
- [docs/data.md](docs/data.md) - PostgreSQL, MongoDB, разделение данных, seeding, сброс и пересоздание БД.
- [docs/runbook.md](docs/runbook.md) - запуск, остановка, Docker, локальный dev, внешнее подключение и отладка.
- [docs/testing.md](docs/testing.md) - тестовая инфраструктура и запуск проверок.
- [docs/development-modules.md](docs/development-modules.md) - помодульное описание разработки с демонстрационными фрагментами кода.
- [README_DB.md](README_DB.md) - короткий вход в документацию по базам данных.

## Структура репозитория

```text
.
├── Backend/
│   ├── dnd-helper/
│   │   ├── Application/
│   │   ├── Domain/
│   │   ├── Infrastructure/
│   │   ├── Presentation/
│   │   ├── Program.cs
│   │   └── dnd-helper.csproj
│   └── dnd-helper.Tests/
├── Frontend/
│   └── dnd-helper/
│       ├── src/
│       │   ├── app/
│       │   ├── entities/
│       │   ├── features/
│       │   ├── pages/
│       │   ├── shared/
│       │   ├── widgets/
│       │   ├── types/
│       │   └── utils/
│       └── vite.config.ts
├── docs/
├── docker-compose.yml
├── README.md
└── README_DB.md
```

## Архитектурный статус

Backend приведён к Clean-style layered architecture:
- `Domain` - сущности и предметное состояние.
- `Application` - сценарии использования, сервисы, контракты и порты.
- `Infrastructure` - PostgreSQL, MongoDB, Identity, seeding и внешние реализации.
- `Presentation` - HTTP endpoints и API mapping.

Frontend организован по FSD:
- `app` - корень приложения и маршрутизация.
- `pages` - route-level страницы.
- `widgets` - крупные UI-блоки страниц.
- `features` - пользовательские сценарии, API и model hooks.
- `entities` - UI и модель предметных сущностей.
- `shared` - общие инфраструктурные утилиты.

Подробнее: [docs/architecture.md](docs/architecture.md).

## Важные соглашения

- Не хранить `bin`, `obj`, `dist`, `node_modules`, локальные БД и секреты в репозитории.
- Не удалять пользовательскую PostgreSQL/MongoDB базу при каждом запуске.
- Справочные данные MongoDB сидятся idempotent upsert'ом по `rulesetId + slug`.
- PostgreSQL demo/test данные создаются только если отсутствуют.
- Новую backend-логику предпочтительно добавлять через `Application` use case/service, а endpoints оставлять тонкими.
- Новую frontend-логику предпочтительно раскладывать по FSD-слоям, не раздувая `pages`.
