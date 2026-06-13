# Runbook

Документ описывает запуск, остановку и диагностику проекта.

## Требования

- Docker и Docker Compose.
- .NET SDK 8.
- Node.js 20+ или совместимая версия для Vite.
- npm.

## Запуск всего проекта в Docker

```bash
docker compose up --build
```

Адреса:
- Frontend: `http://localhost:5173`
- Backend: `http://localhost:5026`
- Swagger: `http://localhost:5026/swagger`

Остановить:

```bash
docker compose down
```

Остановить и удалить volumes:

```bash
docker compose down -v
```

## Запуск только баз в Docker

```bash
docker compose up -d postgres mongo
```

Backend локально:

```bash
dotnet run --project Backend/dnd-helper/dnd-helper.csproj
```

Frontend локально:

```bash
cd Frontend/dnd-helper
npm run dev -- --host 0.0.0.0
```

## Локальная сборка

Backend:

```bash
dotnet build dnd-helper.sln
```

Frontend:

```bash
cd Frontend/dnd-helper
npm run build
```

## Тесты

```bash
dotnet test dnd-helper.sln
```

## Проверка контейнеров

```bash
docker compose ps
docker compose logs backend
docker compose logs frontend
docker compose logs postgres
docker compose logs mongo
```

Логи в реальном времени:

```bash
docker compose logs -f backend
```

## Частые проблемы

### Frontend показывает `Request failed` или Vite proxy error

Проверить, что backend доступен:

```bash
curl http://localhost:5026/api/auth/me
```

Для неавторизованного пользователя ответ `401` нормален. Ошибка соединения означает, что backend не запущен или слушает другой порт.

### Backend не стартует

Проверить базы:

```bash
docker compose ps
docker compose logs postgres
docker compose logs mongo
```

Проверить настройки:
- `Backend/dnd-helper/appsettings.json`
- `Backend/dnd-helper/appsettings.Development.json`
- `docker-compose.yml`

### Обновление локального справочника данных

MongoDB seed использует upsert. Для полного пересоздания локального справочника и операционных данных можно удалить volumes:

```bash
docker compose down -v
docker compose up --build
```

Важно: это удалит локальные данные.

### Нельзя подключиться извне к frontend в Docker

Проверить:
- порт `5173` проброшен в `docker-compose.yml`;
- сервис слушает `0.0.0.0`, а не только `localhost`;
- firewall разрешает входящие подключения;
- проброс порта на роутере ведёт на нужный ПК;
- провайдер не использует CG-NAT.

Команды:

```bash
ss -ltnp | grep 5173
docker compose ps
docker compose logs frontend
```

## Очистка локальных артефактов

В репозитории не должны храниться:
- `bin/`
- `obj/`
- `node_modules/`
- `dist/`
- локальные `.db`
- `.env`

Они уже перечислены в `.gitignore`.
