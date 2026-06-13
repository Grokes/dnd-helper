# Frontend

Frontend `DND Helper` находится в `Frontend/dnd-helper` и построен на React, TypeScript, Vite и `react-router-dom`.

## Назначение

Frontend предоставляет пользовательский интерфейс для:
- регистрации и входа;
- просмотра личных персонажей;
- создания и редактирования персонажа;
- подробного просмотра листа персонажа;
- управления игровыми комнатами;
- подключения к комнате по приглашению;
- работы ведущего с чудовищами;
- просмотра справочника правил.

## Структура

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

## Слои

### `app`

`app` содержит корень React-приложения.

Основной файл:
- `src/app/App.tsx`

Здесь определены:
- общий shell приложения;
- верхняя навигация;
- меню пользователя;
- маршруты приложения;
- перенаправления для авторизованных и неавторизованных пользователей.

### `pages`

`pages` содержит route-level страницы.

Страницы:
- `CharactersPage.tsx` - список личных персонажей.
- `CharacterCreatePage.tsx` - создание и редактирование персонажа.
- `CharacterDetailPage.tsx` - подробный лист персонажа.
- `RoomsPage.tsx` - список игровых комнат.
- `RoomDetailPage.tsx` - подробная страница комнаты.
- `RoomInvitePage.tsx` - подключение к комнате по invite token.
- `DataCheckPage.tsx` - справочник правил.
- `LoginPage.tsx` - вход.
- `RegisterPage.tsx` - регистрация.
- `ProfilePage.tsx` - личный кабинет.

### `widgets`

`widgets` содержит крупные самостоятельные UI-блоки.

Примеры:
- `widgets/character-create/ui/CharacterBuilderHeader.tsx`
- `widgets/character-detail/ui/CharacterDetailHero.tsx`
- `widgets/character-detail/ui/DiceRollPanel.tsx`
- `widgets/character-detail/ui/AbilityScoresPanel.tsx`
- `widgets/character-detail/ui/CharacterSkillsPanel.tsx`
- `widgets/character-detail/ui/SavingThrowsPanel.tsx`
- `widgets/character-detail/ui/SpellSlotsPanel.tsx`

Widgets получают данные через props и не выполняют прямые HTTP-запросы.

### `features`

`features` содержит пользовательские сценарии, API-клиенты и model hooks.

Модули:
- `auth` - авторизация, регистрация, logout, current user provider.
- `characters` - API персонажей и загрузка списка персонажей.
- `character-create` - модель конструктора персонажа.
- `character-detail` - модель подробного листа персонажа.
- `rooms` - API комнат и модель комнаты.
- `rules` - API и модель справочника.

### `entities`

`entities` содержит UI и модель предметных сущностей.

Сущность персонажа:
- `entities/character/ui/CharacterCard.tsx`

### `shared`

`shared` содержит общую инфраструктуру.

Основной файл:
- `shared/api/http.ts`

`http.ts` оборачивает `fetch`, добавляет `credentials: include`, выставляет JSON headers и нормализует ошибки API.

### `types`

`types/character.ts` содержит TypeScript-типы ответов и запросов backend API: персонажи, комнаты, справочник, auth payloads, ошибки и игровые DTO.

### `utils`

`utils/characterPresentation.ts` содержит функции отображения:
- перевод характеристик;
- перевод навыков;
- форматирование skill level;
- генерация портрета персонажа.

## Маршруты

Основные маршруты:
- `/login`
- `/register`
- `/profile`
- `/characters`
- `/characters/new/:step`
- `/characters/:id`
- `/characters/:id/edit/:step`
- `/rooms`
- `/rooms/:id`
- `/rooms/invite/:inviteToken`
- `/data-check`

Корневой маршрут `/` перенаправляет пользователя:
- на `/characters`, если пользователь авторизован;
- на `/login`, если пользователь не авторизован.

## Авторизация

Авторизация реализована через `features/auth/model/AuthProvider.tsx`.

Provider хранит:
- текущего пользователя;
- состояние загрузки;
- методы `login`, `register`, `logout`, `refreshUser`.

Frontend использует cookie-based authentication через backend. Все API-запросы отправляются с `credentials: include`.

## API

API разделён по доменным сценариям:

- Auth: `features/auth/api/authApi.ts`
- Characters: `features/characters/api/charactersApi.ts`
- Rooms: `features/rooms/api/roomsApi.ts`
- Rules: `features/rules/api/rulesApi.ts`

Vite proxy направляет `/api` на backend.

## Конструктор персонажа

Конструктор персонажа использует пошаговый интерфейс:
- основа;
- раса;
- класс;
- предыстория;
- характеристики;
- заклинания;
- инвентарь;
- проверка.

Модель конструктора находится в `features/character-create/model/characterCreateModel.ts`.

Модель отвечает за:
- draft персонажа;
- список шагов;
- базовые ограничения характеристик;
- расчёт модификаторов;
- расчёт хитов;
- преобразование существующего персонажа в draft;
- работу с выбранной экипировкой.

## Подробный лист персонажа

Подробная страница персонажа отображает:
- портрет и основную информацию;
- свободные броски кубов;
- характеристики;
- ключевые показатели;
- навыки;
- спасброски;
- ячейки заклинаний;
- известные заклинания;
- инвентарь;
- заметки;
- историю бросков.

Модель подробного листа находится в `features/character-detail/model/characterDetailModel.ts`.

## Справочник

Справочник находится на странице `/data-check`.

Он отображает:
- расы;
- классы;
- предыстории;
- заклинания;
- снаряжение;
- существ;
- состояния.

API справочника находится в `features/rules/api/rulesApi.ts`, модель вкладок и переводов - в `features/rules/model/catalogModel.ts`.

## Комнаты

Комнаты позволяют пользователям играть вместе.

Frontend комнаты поддерживает:
- создание комнаты;
- подключение к комнате;
- выбор персонажей для комнаты;
- просмотр персонажей участников;
- работу ведущего с чудовищами;
- атаки чудовищ по персонажам;
- уведомления о действиях в комнате.

API комнат находится в `features/rooms/api/roomsApi.ts`.

## Сборка

```bash
cd Frontend/dnd-helper
npm run build
```

## Dev server

```bash
cd Frontend/dnd-helper
npm run dev -- --host 0.0.0.0
```
