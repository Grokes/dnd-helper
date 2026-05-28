# D&D Helper: Database Architecture

## Зачем PostgreSQL
- PostgreSQL хранит операционные данные приложения:
- пользователи и роли (`ASP.NET Identity`)
- комнаты и участники комнат
- персонажи пользователей
- `computed_snapshot` и `calculation_trace`
- инвентарь, известные/подготовленные заклинания, активные эффекты
- encounter и encounter_combatants

## Зачем MongoDB
- MongoDB хранит справочную базу правил D&D:
- `rulesets`, `races`, `classes`, `backgrounds`, `features`
- `spells`, `equipment`, `monsters`, `conditions`
- связи правил хранятся внутри документов (`grants`, `requires`, `choices`, `effects`, `modifiers`, `levels`, `source`)

## Разделение данных
- PostgreSQL: состояние пользователя и текущей игры.
- MongoDB: правила и шаблоны игровых сущностей.
- Создание персонажа читает правила из MongoDB и сохраняет результат в PostgreSQL.

## Где находятся seed-сервисы
- [DatabaseInitializer.cs](/home/grokes/repos/VKR/Backend/dnd-helper/Infrastructure/Seeding/DatabaseInitializer.cs)
- [ApplicationDatabaseSeeder.cs](/home/grokes/repos/VKR/Backend/dnd-helper/Infrastructure/Seeding/ApplicationDatabaseSeeder.cs)
- [RulesDatabaseSeeder.cs](/home/grokes/repos/VKR/Backend/dnd-helper/Infrastructure/Seeding/RulesDatabaseSeeder.cs)

## Как работает idempotent seeding
- При запуске `DatabaseInitializer`:
- подготавливает PostgreSQL (`EnsureCreated`)
- создаёт MongoDB indexes
- делает upsert правил в MongoDB по `(rulesetId, slug)`
- добавляет demo-данные в PostgreSQL только если их нет
- Повторный запуск не создаёт дубликаты.

## Запуск через docker-compose
1. В корне репозитория:
```bash
docker compose up --build
```
2. Backend: `http://localhost:5026`
3. Frontend: `http://localhost:5173`

## Безопасное удаление и пересоздание БД
1. Остановить контейнеры:
```bash
docker compose down
```
2. Удалить volumes:
```bash
docker volume rm vkr_pg_data vkr_mongo_data
```
3. Запустить снова:
```bash
docker compose up --build
```

После этого приложение заново создаст минимальные данные при старте.

## Как добавить новые справочные данные
1. Добавить документы в `RulesDatabaseSeeder` (race/class/feature/spell/etc.).
2. Использовать стабильный `slug`.
3. Перезапустить backend: сидер выполнит upsert без дублирования.
