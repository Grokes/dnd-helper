# Разработка модулей

Документ описывает ключевые модули `DND Helper` с точки зрения разработки. Для каждого модуля приведён демонстрационный фрагмент кода и объяснение его роли в системе.

## 1. Модуль авторизации

### Назначение

Модуль авторизации отвечает за регистрацию, вход, выход и получение текущего пользователя. На frontend он представлен `AuthProvider`, а на backend использует ASP.NET Core Identity.

### Фрагмент кода

```tsx
export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(null)
  const [isLoading, setIsLoading] = useState(true)

  async function refreshUser() {
    try {
      const currentUser = await getCurrentUser()
      setUser(currentUser)
    } catch (error) {
      const apiError = error as ApiValidationError
      if (apiError.message.includes('401') || apiError.message.includes('Unauthorized')) {
        setUser(null)
        return
      }

      setUser(null)
    }
  }

  async function login(payload: LoginPayload) {
    await loginUser(payload)
    await refreshUser()
  }

  return (
    <AuthContext.Provider value={{ user, isLoading, login, register, logout, refreshUser }}>
      {children}
    </AuthContext.Provider>
  )
}
```

### Значимость

`AuthProvider` централизует состояние входа пользователя. Компоненты приложения не работают с cookies или `/api/auth/me` напрямую: они получают пользователя и auth-действия через `useAuth`. Это упрощает защиту маршрутов, отображение меню пользователя и сохранение состояния входа после обновления страницы.

## 2. Общий HTTP-клиент frontend

### Назначение

`shared/api/http.ts` стандартизирует все HTTP-запросы frontend-приложения: добавляет cookie-auth, JSON headers и единый формат ошибок.

### Фрагмент кода

```ts
export async function apiRequest<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(path, {
    credentials: 'include',
    headers: {
      'Content-Type': 'application/json',
      ...(init?.headers ?? {}),
    },
    ...init,
  })

  if (!response.ok) {
    throw await parseApiError(response)
  }

  if (response.status === 204) {
    return undefined as T
  }

  return (await response.json()) as T
}
```

### Значимость

Все feature API используют один клиент. Благодаря этому приложение одинаково обрабатывает validation errors, unauthorized responses и пустые ответы `204`. Если меняется способ авторизации или формат ошибок backend, правка выполняется в одном месте.

## 3. Модуль персонажей: создание персонажа

### Назначение

Создание персонажа реализовано как application use case. Endpoint передаёт request и owner user id, а use case вызывает сервис создания, сохраняет сущность и возвращает DTO.

### Фрагмент кода

```csharp
public async Task<UseCaseResult<CharacterDto>> ExecuteAsync(
    CreateCharacterRequest request,
    string ownerUserId,
    CancellationToken cancellationToken)
{
    var createResult = await creationService.BuildCharacterAsync(request, ownerUserId, cancellationToken);
    if (!createResult.IsSuccess)
    {
        return UseCaseResult<CharacterDto>.ValidationFailed(createResult.Errors!);
    }

    var character = createResult.Character!;
    dbContext.Characters.Add(character);
    await dbContext.SaveChangesAsync(cancellationToken);

    var dto = character.ToDto();
    return UseCaseResult<CharacterDto>.Created(dto, $"/api/characters/{character.Id}");
}
```

### Значимость

Use case отделяет HTTP-слой от сценария создания персонажа. Endpoint не знает, как применяются правила D&D, как формируется snapshot и как сохраняется персонаж. Это делает код тестируемым и уменьшает связанность между transport layer и business logic.

## 4. Модуль персонажей: карточка персонажа

### Назначение

`CharacterCard` отображает краткую информацию о персонаже в списке: портрет, имя, расу, класс, уровень и несколько навыков.

### Фрагмент кода

```tsx
export function CharacterCard({ character }: CharacterCardProps) {
  return (
    <article className="character-card">
      <img
        className="character-card__portrait"
        src={getCharacterPortrait(character.name, character.race, character.className)}
        alt={`Портрет персонажа ${character.name}`}
      />

      <div className="character-card__body">
        <div className="character-card__header">
          <div>
            <h3>{character.name}</h3>
            <p>{character.race} • {character.className}</p>
          </div>
          <span className="pill">Ур. {character.level}</span>
        </div>

        <div className="skill-tags">
          {character.skills.slice(0, 3).map((skill) => (
            <span key={skill.skillId} className="skill-tag">
              {formatSkillLevel(skill)}
            </span>
          ))}
        </div>
      </div>
    </article>
  )
}
```

### Значимость

Карточка вынесена в слой `entities`, потому что персонаж является предметной сущностью приложения. Такой компонент можно использовать в разных сценариях: список личных персонажей, выбор персонажа для комнаты, просмотр персонажей участников.

## 5. Модуль списка персонажей

### Назначение

Hook `useMyCharacters` загружает личных персонажей пользователя и хранит состояние загрузки/ошибки.

### Фрагмент кода

```ts
export function useMyCharacters(userId?: string) {
  const [characters, setCharacters] = useState<CharacterSummary[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!userId) {
      setCharacters([])
      setIsLoading(false)
      return
    }

    let isCancelled = false
    setIsLoading(true)

    async function loadCharacters() {
      try {
        const response = await getMyCharacters()
        if (!isCancelled) {
          setCharacters(response)
          setError(null)
        }
      } catch {
        if (!isCancelled) {
          setError('Не удалось загрузить список персонажей.')
        }
      } finally {
        if (!isCancelled) {
          setIsLoading(false)
        }
      }
    }

    void loadCharacters()

    return () => {
      isCancelled = true
    }
  }, [userId])

  return { characters, isLoading, error }
}
```

### Значимость

Hook отделяет загрузку данных от страницы. `CharactersPage` становится композицией UI: проверка авторизации, заголовок и сетка карточек. Логика запроса, отмены состояния после unmount и обработки ошибок находится в feature-модуле.

## 6. Модуль правил и MongoDB-справочника

### Назначение

`MongoRulesCatalogRepository` реализует application-порт `IRulesCatalogRepository` и предоставляет доступ к расам, классам, предысториям, заклинаниям, снаряжению, чудовищам и состояниям.

### Фрагмент кода

```csharp
public sealed class MongoRulesCatalogRepository(IMongoDatabase database) : IRulesCatalogRepository
{
    private readonly IMongoCollection<RaceDocument> _races = database.GetCollection<RaceDocument>("races");
    private readonly IMongoCollection<ClassDocument> _classes = database.GetCollection<ClassDocument>("classes");
    private readonly IMongoCollection<SpellDocument> _spells = database.GetCollection<SpellDocument>("spells");

    public async Task<IReadOnlyList<RaceDocument>> GetRacesAsync(
        string rulesetId,
        CancellationToken cancellationToken = default)
        => await GetByRulesetAsync(_races, rulesetId, cancellationToken);

    public async Task<RaceDocument?> GetRaceBySlugAsync(
        string rulesetId,
        string slug,
        CancellationToken cancellationToken = default)
        => await _races.Find(Builders<RaceDocument>.Filter.And(
                Builders<RaceDocument>.Filter.Eq(x => x.RulesetId, rulesetId),
                Builders<RaceDocument>.Filter.Eq(x => x.Slug, slug)))
            .FirstOrDefaultAsync(cancellationToken);
}
```

### Значимость

Application-слой работает с интерфейсом `IRulesCatalogRepository`, а не с MongoDB напрямую. Это сохраняет границу между бизнес-логикой и инфраструктурой. MongoDB остаётся деталью реализации, а сервисы персонажей получают готовые документы правил.

## 7. Модуль бросков кубов

### Назначение

`DiceRoller` выполняет безопасный разбор dice expressions и броски кубов. Он используется для урона, лечения, атак, инициативы и игровых проверок.

### Фрагмент кода

```csharp
public bool TryRoll(string sourceDice, out DiceRollResult result)
{
    result = new DiceRollResult(sourceDice, sourceDice, [], 0, 0);

    if (string.IsNullOrWhiteSpace(sourceDice))
    {
        return false;
    }

    var normalized = sourceDice.Trim().ToLowerInvariant().Replace(" ", string.Empty);
    var match = Regex.Match(
        normalized,
        @"^(?:(?<count>\d+)d(?<sides>\d+)|(?<flat>\d+))(?:(?<sign>[+-])(?<bonus>\d+))?$");
    if (!match.Success)
    {
        return false;
    }

    var modifier = ParseModifier(match);
    if (modifier is < -10_000 or > 10_000)
    {
        return false;
    }

    // дальнейший расчёт dice/flat value
}
```

### Значимость

Dice expressions приходят из справочника правил и используются в игровой логике. Парсер ограничивает формат, число кубов, число граней и диапазон модификаторов. Это защищает приложение от некорректных выражений и делает броски предсказуемыми для тестирования.

## 8. Модуль HTTP endpoints персонажей

### Назначение

`CharacterEndpoints` публикует API для списка, просмотра, создания, обновления, отдыха и применения заклинаний персонажа.

### Фрагмент кода

```csharp
endpoints.MapPost("/api/characters", async (
    CreateCharacterRequest request,
    ClaimsPrincipal principal,
    UserManager<ApplicationUser> userManager,
    CreateCharacterUseCase useCase,
    CancellationToken cancellationToken) =>
{
    var user = await userManager.GetUserAsync(principal);
    if (user is null)
    {
        return Results.Unauthorized();
    }

    var result = await useCase.ExecuteAsync(request, user.Id, cancellationToken);
    return result.ToHttpResult();
}).RequireAuthorization();
```

### Значимость

Endpoint выполняет только transport-level задачи: получает request, определяет пользователя, вызывает use case и возвращает HTTP result. Валидация правил и сохранение персонажа находятся в application-слое.

## 9. Модуль инициализации баз данных

### Назначение

`DatabaseInitializer` подготавливает PostgreSQL, нормализует данные, создаёт MongoDB-справочник и добавляет начальные application-данные.

### Фрагмент кода

```csharp
public async Task InitializeAsync(CancellationToken cancellationToken = default)
{
    logger.LogInformation("Initializing PostgreSQL schema...");
    await EnsurePostgresSchemaAsync(cancellationToken);
    await RemoveObsoletePostgresColumnsAsync(cancellationToken);
    await NormalizePostgresDataAsync(cancellationToken);

    logger.LogInformation("Initializing MongoDB rules catalog...");
    await rulesSeeder.SeedAsync(cancellationToken);

    logger.LogInformation("Initializing application seed data...");
    await applicationSeeder.SeedAsync(cancellationToken);

    logger.LogInformation("Database initialization completed.");
}
```

### Значимость

Backend сам готовит необходимые схемы и справочные данные при запуске. Это упрощает локальный запуск, Docker-сценарий и восстановление окружения после удаления volumes.

## 10. Модуль подробного листа персонажа

### Назначение

Подробный лист персонажа состоит из widgets: hero, панель кубов, характеристики, навыки, спасброски и ячейки заклинаний.

### Фрагмент кода

```tsx
export function AbilityScoresPanel({ abilities, onRoll }: AbilityScoresPanelProps) {
  return (
    <article className="surface-card">
      <h3>Характеристики</h3>
      <div className="ability-grid">
        {abilities.map((ability) => (
          <button
            type="button"
            className="ability-card ability-card--interactive button-reset"
            key={ability.key}
            onClick={() => onRoll(`Проверка: ${translateAbility(ability.key)}`, 20, ability.modifier)}
          >
            <span className="ability-card__name">{translateAbility(ability.key)}</span>
            <div className="ability-card__values">
              <strong>{ability.score}</strong>
              <small>{ability.modifier >= 0 ? `+${ability.modifier}` : ability.modifier}</small>
            </div>
          </button>
        ))}
      </div>
    </article>
  )
}
```

### Значимость

Панель характеристик не знает, как загружается персонаж и где хранится история бросков. Она получает данные и callback `onRoll`. Такое разделение делает UI-блок переиспользуемым и уменьшает размер route-level страницы.

## 11. Модуль справочника

### Назначение

Справочник отображает структурированные данные правил: расы, классы, предыстории, заклинания, снаряжение, существ и состояния.

### Фрагмент кода

```ts
export const catalogTabs: Array<{ key: CatalogTabKey; label: string }> = [
  { key: 'races', label: 'Расы' },
  { key: 'classes', label: 'Классы' },
  { key: 'backgrounds', label: 'Предыстории' },
  { key: 'spells', label: 'Заклинания' },
  { key: 'equipment', label: 'Снаряжение' },
  { key: 'monsters', label: 'Существа' },
  { key: 'conditions', label: 'Состояния' },
]
```

### Значимость

Вкладки справочника описаны в model-файле feature-модуля. Страница справочника получает готовую конфигурацию и отображает соответствующую категорию данных. Это отделяет структуру справочника от JSX страницы.

## 12. Модуль тестирования application-логики

### Назначение

Тесты проверяют application-сервисы без запуска HTTP-сервера и без реальных баз данных.

### Фрагмент кода

```csharp
var character = CreateCharacter(
    knownSpells: ["burning-hands"],
    slots: [new SpellSlotDto(1, 2)]);
var service = CreateService(
    [CreateSpell("burning-hands", "Огненные ладони", 1)],
    rolls: [2, 3, 4]);

var outcome = await service.CastAsync(
    character,
    new CharacterCastSpellRequest("burning-hands", null),
    CancellationToken.None);

Assert.True(outcome.IsSuccess);
Assert.Equal(9, outcome.Result?.DamageTotal);
```

### Значимость

Тест проверяет игровое правило на уровне application-сервиса: персонаж знает заклинание, ячейка расходуется, урон рассчитывается детерминированно. Такой тест не зависит от HTTP, UI и реальной MongoDB.
