# Данные и базы

Проект использует две базы данных:
- PostgreSQL - операционная база приложения.
- MongoDB - справочник правил D&D 5e.

## Почему две базы

D&D-приложение хранит два принципиально разных типа данных.

Операционные данные:
- пользователь;
- персонаж конкретного пользователя;
- комната;
- участник комнаты;
- текущее состояние чудовища;
- текущие хиты персонажа;
- выбранные заклинания и инвентарь.

Справочные данные:
- раса;
- класс;
- предыстория;
- особенность;
- заклинание;
- предмет;
- чудовище;
- состояние.

Операционные данные хорошо ложатся в PostgreSQL. Справочные правила естественно выглядят как документы с вложенными grants, choices, effects и modifiers, поэтому используются MongoDB-документы.

## PostgreSQL

PostgreSQL хранит:
- users and roles through ASP.NET Identity;
- rooms;
- room members;
- selected room characters;
- room monsters;
- characters;
- encounters;
- encounter combatants.

Персонаж хранит:
- выбранные `raceId`, `classId`, `backgroundId`;
- базовые характеристики;
- выбранные опции;
- рассчитанные характеристики;
- inventory state;
- known spells;
- spell slots;
- current/max hit points;
- computed snapshot;
- calculation trace.

Основной файл:
- `Backend/dnd-helper/Infrastructure/Persistence/Postgres/AppDbContext.cs`

## MongoDB

MongoDB хранит:
- `rulesets`;
- `races`;
- `classes`;
- `backgrounds`;
- `features`;
- `spells`;
- `equipment`;
- `monsters`;
- `conditions`.

Документы правил содержат:
- `grants`;
- `requires`;
- `choices`;
- `effects`;
- `modifiers`;
- `source`;
- `levels`.

Основные файлы:
- `Backend/dnd-helper/Infrastructure/Persistence/Mongo/RuleDocuments.cs`
- `Backend/dnd-helper/Infrastructure/Persistence/Mongo/MongoRulesCatalogRepository.cs`
- `Backend/dnd-helper/Application/Rules/IRulesCatalogRepository.cs`

## Seeding

Данные создаются при старте backend.

Основной entry point:
- `Backend/dnd-helper/Infrastructure/Seeding/DatabaseInitializer.cs`

Сидеры:
- `ApplicationDatabaseSeeder.cs` - начальные данные PostgreSQL.
- `RulesDatabaseSeeder.cs` - справочник правил MongoDB.
- `RulesDatabaseSeeder.Conditions.cs` - справочник состояний.

## Idempotent seeding

Повторный запуск не должен создавать дубликаты.

Правила:
- MongoDB documents добавляются через upsert по `rulesetId + slug`.
- PostgreSQL начальные данные создаются только если отсутствуют.
- Существующие пользователи не удаляются при обычном запуске.
- После полного удаления volumes приложение должно восстановить минимальный набор данных.

## Как пересоздать базы

Остановить контейнеры:

```bash
docker compose down
```

Удалить volumes:

```bash
docker volume rm vkr_pg_data vkr_mongo_data
```

Запустить снова:

```bash
docker compose up --build
```

Если имена volumes отличаются, посмотреть их можно так:

```bash
docker volume ls
```

## Подключение к PostgreSQL из DBeaver

Если PostgreSQL запущен через `docker-compose.yml`, подключение обычно такое:

- Host: `localhost`
- Port: `5432`
- Database: значение из compose/env, обычно `dnd_helper`
- User: значение из compose/env, обычно `dnd_helper`
- Password: значение из compose/env

Точные значения смотри в `docker-compose.yml`.

## Добавление справочных данных

1. Выбрать тип документа: race, class, background, spell, equipment, monster, condition.
2. Добавить стабильный `slug`.
3. Добавить документ в соответствующую секцию `RulesDatabaseSeeder`.
4. Перезапустить backend.
5. Проверить данные через `/api/rules/*`, `/api/equipment`, `/api/monsters` или страницу справочника.

## Важное ограничение

В репозиторий не добавляются большие защищённые авторским правом материалы. В коде хранится только необходимая структурированная справочная информация для работы приложения.
