# DND Helper: базы данных

Короткий вход в документацию по данным проекта.

Подробное описание находится в [docs/data.md](docs/data.md).

## Кратко

- PostgreSQL хранит операционные данные приложения: пользователей, роли, персонажей, комнаты, участников, чудовищ в комнатах и текущее состояние игры.
- MongoDB хранит справочник правил: rulesets, races, classes, backgrounds, features, spells, equipment, monsters, conditions.
- PostgreSQL отвечает на вопрос: "что сейчас происходит у конкретного пользователя или в конкретной комнате?"
- MongoDB отвечает на вопрос: "какие правила, шаблоны и справочные сущности существуют?"

## Seeding

При старте backend запускает `DatabaseInitializer`, который:
- подготавливает PostgreSQL;
- создаёт MongoDB indexes;
- upsert'ит справочные данные MongoDB;
- создаёт demo/test данные PostgreSQL только если они отсутствуют.

## Пересоздание локальных БД

```bash
docker compose down -v
docker compose up --build
```

Осторожно: команда удалит локальные volumes и все пользовательские данные в контейнерах.
