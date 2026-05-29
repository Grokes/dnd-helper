using dnd_helper.Features.Auth;
using dnd_helper.Features.Rules;
using dnd_helper.Features.ReferenceData;
using dnd_helper.Infrastructure.Persistence.Postgres;
using dnd_helper.Infrastructure.Seeding;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace dnd_helper.Features.Characters;

public static class CharacterEndpoints
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static IEndpointRouteBuilder MapCharacterEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/my/characters", async (
            ClaimsPrincipal principal,
            UserManager<ApplicationUser> userManager,
            AppDbContext dbContext) =>
        {
            var user = await userManager.GetUserAsync(principal);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var isGameMaster = await userManager.IsInRoleAsync(user, ApplicationRoles.GameMaster);
            var query = dbContext.Characters.AsNoTracking();
            if (!isGameMaster)
            {
                query = query.Where(character => character.OwnerUserId == user.Id);
            }

            var characters = await query.OrderBy(character => character.Name).ToListAsync();
            return Results.Ok(characters.Select(character => character.ToSummaryDto()));
        }).RequireAuthorization();

        endpoints.MapGet("/api/characters/{id:guid}", async (
            Guid id,
            ClaimsPrincipal principal,
            UserManager<ApplicationUser> userManager,
            AppDbContext dbContext) =>
        {
            var user = await userManager.GetUserAsync(principal);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var character = await dbContext.Characters.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);
            if (character is null)
            {
                return Results.NotFound();
            }

            var isGameMaster = await userManager.IsInRoleAsync(user, ApplicationRoles.GameMaster);
            var isOwner = character.OwnerUserId == user.Id;
            var isRoomViewer = false;

            if (!isGameMaster && !isOwner)
            {
                isRoomViewer = await dbContext.RoomMemberships
                    .AsNoTracking()
                    .Where(member => member.UserId == user.Id)
                    .Join(
                        dbContext.RoomMembershipCharacters.AsNoTracking().Where(member => member.CharacterId == id),
                        currentMember => currentMember.RoomId,
                        targetMember => targetMember.RoomId,
                        (currentMember, targetMember) => targetMember.RoomId)
                    .AnyAsync();
            }

            if (!isGameMaster && !isOwner && !isRoomViewer)
            {
                return Results.Forbid();
            }

            return Results.Ok(character.ToDto(canEdit: isGameMaster || isOwner));
        }).RequireAuthorization();

        endpoints.MapGet("/api/characters/{id:guid}/calculation-trace", async (
            Guid id,
            ClaimsPrincipal principal,
            UserManager<ApplicationUser> userManager,
            AppDbContext dbContext) =>
        {
            var user = await userManager.GetUserAsync(principal);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var character = await dbContext.Characters.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (character is null)
            {
                return Results.NotFound();
            }

            var isGameMaster = await userManager.IsInRoleAsync(user, ApplicationRoles.GameMaster);
            var canView = isGameMaster || character.OwnerUserId == user.Id ||
                          await dbContext.RoomMemberships
                              .AsNoTracking()
                              .Where(member => member.UserId == user.Id)
                              .Join(
                                  dbContext.RoomMembershipCharacters.AsNoTracking().Where(member => member.CharacterId == id),
                                  currentMember => currentMember.RoomId,
                                  targetMember => targetMember.RoomId,
                                  (currentMember, targetMember) => targetMember.RoomId)
                              .AnyAsync();

            if (!canView)
            {
                return Results.Forbid();
            }

            return Results.Ok(character.ToDto(canEdit: isGameMaster || character.OwnerUserId == user.Id).CalculationTrace);
        }).RequireAuthorization();

        endpoints.MapPost("/api/characters", async (
            CreateCharacterRequest request,
            ClaimsPrincipal principal,
            UserManager<ApplicationUser> userManager,
            AppDbContext dbContext,
            CharacterCreationService creationService,
            CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(principal);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var createResult = await creationService.BuildCharacterAsync(request, user.Id, cancellationToken);
            if (!createResult.IsSuccess)
            {
                return Results.ValidationProblem(createResult.Errors!);
            }

            dbContext.Characters.Add(createResult.Character!);
            await dbContext.SaveChangesAsync(cancellationToken);
            return Results.Created($"/api/characters/{createResult.Character!.Id}", createResult.Character.ToDto());
        }).RequireAuthorization();

        endpoints.MapPut("/api/characters/{id:guid}", async (
            Guid id,
            UpdateCharacterRequest request,
            ClaimsPrincipal principal,
            UserManager<ApplicationUser> userManager,
            AppDbContext dbContext,
            CharacterCreationService creationService,
            CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(principal);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var character = await dbContext.Characters.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
            if (character is null)
            {
                return Results.NotFound();
            }

            var isGameMaster = await userManager.IsInRoleAsync(user, ApplicationRoles.GameMaster);
            var isOwner = character.OwnerUserId == user.Id;
            if (!isGameMaster && !isOwner)
            {
                return Results.Forbid();
            }

            var normalizedRequest = new CreateCharacterRequest(
                request.Name,
                request.RaceId,
                request.ClassId,
                request.BackgroundId,
                request.Level,
                request.Alignment,
                request.Notes,
                request.BaseAbilities,
                request.BonusAbilitySelections,
                request.RaceSkillSelections,
                request.ClassSkillSelections,
                request.Spells,
                request.Inventory);

            var updateResult = await creationService.BuildCharacterAsync(normalizedRequest, character.OwnerUserId, cancellationToken);
            if (!updateResult.IsSuccess)
            {
                return Results.ValidationProblem(updateResult.Errors!);
            }

            var rebuilt = updateResult.Character!;
            var previousCurrentHitPoints = character.MaxHitPoints > 0
                ? character.CurrentHitPoints
                : character.HitPoints;
            character.RaceId = rebuilt.RaceId;
            character.ClassId = rebuilt.ClassId;
            character.BackgroundId = rebuilt.BackgroundId;
            character.Name = rebuilt.Name;
            character.Race = rebuilt.Race;
            character.ClassName = rebuilt.ClassName;
            character.Subclass = rebuilt.Subclass;
            character.Level = rebuilt.Level;
            character.Background = rebuilt.Background;
            character.Alignment = rebuilt.Alignment;
            character.ArmorClass = rebuilt.ArmorClass;
            character.WeaponDamage = rebuilt.WeaponDamage;
            character.HitPoints = rebuilt.HitPoints;
            character.MaxHitPoints = rebuilt.MaxHitPoints <= 0 ? rebuilt.HitPoints : rebuilt.MaxHitPoints;
            character.CurrentHitPoints = Math.Clamp(previousCurrentHitPoints, 0, character.MaxHitPoints);
            character.Speed = rebuilt.Speed;
            character.ProficiencyBonus = rebuilt.ProficiencyBonus;
            character.PassivePerception = rebuilt.PassivePerception;
            character.Notes = rebuilt.Notes;
            character.AbilitiesJson = rebuilt.AbilitiesJson;
            character.BaseAbilitiesJson = rebuilt.BaseAbilitiesJson;
            character.BonusAbilitySelectionsJson = rebuilt.BonusAbilitySelectionsJson;
            character.SkillsJson = rebuilt.SkillsJson;
            character.KnownSpellsJson = rebuilt.KnownSpellsJson;
            character.SpellSlotsJson = rebuilt.SpellSlotsJson;
            character.SpentSpellSlotsJson = character.SpentSpellSlotsJson;
            character.PreparedSpellsJson = rebuilt.PreparedSpellsJson;
            character.InventoryJson = rebuilt.InventoryJson;
            character.ActiveEffectsJson = rebuilt.ActiveEffectsJson;
            character.ComputedSnapshotJson = rebuilt.ComputedSnapshotJson;
            character.CalculationTraceJson = rebuilt.CalculationTraceJson;
            character.UpdatedAtUtc = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
            return Results.Ok(character.ToDto(canEdit: isGameMaster || isOwner));
        }).RequireAuthorization();

        endpoints.MapPost("/api/characters/{id:guid}/rest", async (
            Guid id,
            CharacterRestRequest request,
            ClaimsPrincipal principal,
            UserManager<ApplicationUser> userManager,
            AppDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(principal);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var character = await dbContext.Characters.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
            if (character is null)
            {
                return Results.NotFound();
            }

            var isGameMaster = await userManager.IsInRoleAsync(user, ApplicationRoles.GameMaster);
            var isOwner = character.OwnerUserId == user.Id;
            if (!isGameMaster && !isOwner)
            {
                return Results.Forbid();
            }

            var normalizedType = (request.RestType ?? string.Empty).Trim().ToLowerInvariant();
            var maxHitPoints = character.MaxHitPoints > 0 ? character.MaxHitPoints : character.HitPoints;
            var currentHitPoints = character.MaxHitPoints > 0
                ? Math.Clamp(character.CurrentHitPoints, 0, maxHitPoints)
                : maxHitPoints;
            var previousCurrentHitPoints = currentHitPoints;
            var totalHitDice = Math.Max(1, character.Level);
            var spentHitDice = Math.Clamp(character.SpentHitDice, 0, totalHitDice);
            var details = string.Empty;

            if (normalizedType is "short" or "short-rest")
            {
                var hitDiceToSpend = request.HitDiceToSpend ?? 0;
                if (hitDiceToSpend < 0)
                {
                    return Results.ValidationProblem(new Dictionary<string, string[]>
                    {
                        ["hitDiceToSpend"] = ["Количество костей хитов не может быть отрицательным."]
                    });
                }

                var availableHitDice = totalHitDice - spentHitDice;
                if (availableHitDice <= 0)
                {
                    return Results.ValidationProblem(new Dictionary<string, string[]>
                    {
                        ["hitDiceToSpend"] = ["У персонажа нет доступных костей хитов."]
                    });
                }

                if (hitDiceToSpend > availableHitDice)
                {
                    return Results.ValidationProblem(new Dictionary<string, string[]>
                    {
                        ["hitDiceToSpend"] = [$"Можно потратить не более {availableHitDice} костей хитов."]
                    });
                }

                var hitDie = CharacterOptionsCatalog.Classes.FirstOrDefault(item => item.Id == character.ClassId)?.HitDie ?? 8;
                var conModifier = character.ToDto(canEdit: isOwner || isGameMaster).Abilities.FirstOrDefault(item => item.Key == "CON")?.Modifier ?? 0;
                var rolls = new List<int>(Math.Max(0, hitDiceToSpend));
                var healed = 0;
                for (var index = 0; index < hitDiceToSpend; index++)
                {
                    var roll = Random.Shared.Next(1, hitDie + 1);
                    rolls.Add(roll);
                    healed += Math.Max(0, roll + conModifier);
                }

                currentHitPoints = Math.Min(maxHitPoints, currentHitPoints + healed);
                spentHitDice += hitDiceToSpend;
                var rollsText = rolls.Count > 0 ? string.Join(", ", rolls) : "без траты костей";
                details = $"Короткий отдых: потрачено {hitDiceToSpend}к{hitDie}, броски: {rollsText}; мод. Телосложения {conModifier:+#;-#;0}.";

                if (character.ClassId.Equals("warlock", StringComparison.OrdinalIgnoreCase))
                {
                    character.SpentSpellSlotsJson = "{}";
                    details += " Ячейки колдуна восстановлены (классовая особенность).";
                }
            }
            else if (normalizedType is "long" or "long-rest")
            {
                currentHitPoints = maxHitPoints;
                var recoveredHitDice = Math.Max(1, totalHitDice / 2);
                spentHitDice = Math.Max(0, spentHitDice - recoveredHitDice);
                var spentSpellSlots = DeserializeIntMap(character.SpentSpellSlotsJson);
                spentSpellSlots.Clear();
                character.SpentSpellSlotsJson = JsonSerializer.Serialize(spentSpellSlots, JsonOptions);
                details = $"Длительный отдых: хиты восстановлены полностью, кости хитов восстановлены: {recoveredHitDice}, ячейки заклинаний восстановлены.";
            }
            else if (normalizedType is "full-heal" or "heal")
            {
                currentHitPoints = maxHitPoints;
                details = "Полное лечение: текущие хиты восстановлены до максимума.";
            }
            else
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["restType"] = ["Допустимые значения: short, long, full-heal."]
                });
            }

            character.MaxHitPoints = maxHitPoints;
            character.CurrentHitPoints = currentHitPoints;
            character.SpentHitDice = Math.Clamp(spentHitDice, 0, totalHitDice);
            character.UpdatedAtUtc = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);

            var maxSpellSlots = JsonSerializer.Deserialize<List<SpellSlotDto>>(character.SpellSlotsJson, JsonOptions) ?? [];
            var spentSpellSlotsFinal = DeserializeIntMap(character.SpentSpellSlotsJson);
            var currentSpellSlots = maxSpellSlots.Select(slot =>
            {
                var spent = Math.Max(0, spentSpellSlotsFinal.GetValueOrDefault(slot.SpellLevel));
                return new SpellSlotDto(slot.SpellLevel, Math.Max(0, slot.Slots - spent));
            }).ToList();

            return Results.Ok(new CharacterRestResultDto(
                normalizedType,
                previousCurrentHitPoints,
                character.CurrentHitPoints,
                character.MaxHitPoints,
                Math.Max(0, character.CurrentHitPoints - previousCurrentHitPoints),
                character.SpentHitDice,
                Math.Max(0, totalHitDice - character.SpentHitDice),
                currentSpellSlots,
                maxSpellSlots,
                details));
        }).RequireAuthorization();

        endpoints.MapPost("/api/characters/{id:guid}/cast-spell", async (
            Guid id,
            CharacterCastSpellRequest request,
            ClaimsPrincipal principal,
            UserManager<ApplicationUser> userManager,
            AppDbContext dbContext,
            IRulesCatalogRepository rulesRepository,
            CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(principal);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var character = await dbContext.Characters.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
            if (character is null)
            {
                return Results.NotFound();
            }

            var isGameMaster = await userManager.IsInRoleAsync(user, ApplicationRoles.GameMaster);
            var isOwner = character.OwnerUserId == user.Id;
            if (!isGameMaster && !isOwner)
            {
                return Results.Forbid();
            }

            var spellSlug = (request.SpellSlug ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(spellSlug))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["spellSlug"] = ["Выбери заклинание."]
                });
            }

            var knownSpells = JsonSerializer.Deserialize<List<string>>(character.KnownSpellsJson, JsonOptions) ?? [];
            if (!knownSpells.Any(item => item.Equals(spellSlug, StringComparison.OrdinalIgnoreCase)))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["spellSlug"] = ["Персонаж не знает это заклинание."]
                });
            }

            var spell = (await rulesRepository.GetSpellsAsync(RulesDatabaseSeeder.RulesetId, cancellationToken))
                .FirstOrDefault(item => item.Slug.Equals(spellSlug, StringComparison.OrdinalIgnoreCase));
            if (spell is null)
            {
                return Results.NotFound();
            }

            var maxSlots = JsonSerializer.Deserialize<List<SpellSlotDto>>(character.SpellSlotsJson, JsonOptions) ?? [];
            var spentSlots = DeserializeIntMap(character.SpentSpellSlotsJson);
            var spellLevel = Math.Max(0, spell.SpellLevel);
            var chosenSlotLevel = request.SlotLevel;
            var consumedSlot = false;
            var message = $"Заклинание «{spell.Name}» применено.";

            if (spellLevel > 0)
            {
                var levelToUse = chosenSlotLevel ?? spellLevel;
                if (levelToUse < spellLevel)
                {
                    return Results.ValidationProblem(new Dictionary<string, string[]>
                    {
                        ["slotLevel"] = ["Нельзя использовать ячейку ниже круга заклинания."]
                    });
                }

                var slot = maxSlots.FirstOrDefault(item => item.SpellLevel == levelToUse);
                if (slot is null)
                {
                    return Results.ValidationProblem(new Dictionary<string, string[]>
                    {
                        ["slotLevel"] = ["У персонажа нет ячейки этого круга."]
                    });
                }

                var spent = Math.Max(0, spentSlots.GetValueOrDefault(levelToUse));
                if (spent >= slot.Slots)
                {
                    return Results.ValidationProblem(new Dictionary<string, string[]>
                    {
                        ["slotLevel"] = ["Все ячейки этого круга уже израсходованы."]
                    });
                }

                spentSlots[levelToUse] = spent + 1;
                consumedSlot = true;
                chosenSlotLevel = levelToUse;
                message = $"Заклинание «{spell.Name}» применено. Израсходована ячейка круга {levelToUse}.";
            }

            character.SpentSpellSlotsJson = JsonSerializer.Serialize(spentSlots, JsonOptions);
            character.UpdatedAtUtc = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);

            var currentSlots = maxSlots.Select(item =>
            {
                var spent = Math.Max(0, spentSlots.GetValueOrDefault(item.SpellLevel));
                return new SpellSlotDto(item.SpellLevel, Math.Max(0, item.Slots - spent));
            }).ToList();

            var summaryText = spell.Effects.FirstOrDefault(effect => effect.Target.Equals("summary", StringComparison.OrdinalIgnoreCase))?.Reason ?? string.Empty;
            var descriptionText = spell.Effects.FirstOrDefault(effect => effect.Target.Equals("description", StringComparison.OrdinalIgnoreCase))?.Reason ?? string.Empty;
            var hasDamage = TryGetSpellDamageProfile(spell.Slug, summaryText, descriptionText, out var damageDice, out var damageType);
            int? damageRoll = null;
            int? damageTotal = null;
            if (hasDamage && !string.IsNullOrWhiteSpace(damageDice) && TryRollDamage(damageDice!, out _, out var rollResult))
            {
                damageRoll = rollResult;
                damageTotal = rollResult;
            }

            return Results.Ok(new CharacterCastSpellResultDto(
                spell.Slug,
                spell.Name,
                spellLevel,
                chosenSlotLevel,
                consumedSlot,
                currentSlots,
                maxSlots,
                damageDice,
                damageType,
                damageRoll,
                damageTotal,
                message));
        }).RequireAuthorization();

        return endpoints;
    }

    private static Dictionary<int, int> DeserializeIntMap(string? source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<int, int>>(source, JsonOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static bool TryGetSpellDamageProfile(string slug, string summary, string description, out string? damageDice, out string? damageType)
    {
        var map = new Dictionary<string, (string dice, string type)>(StringComparer.OrdinalIgnoreCase)
        {
            ["fire-bolt"] = ("1d10", "огонь"),
            ["chill-touch"] = ("1d8", "некротический"),
            ["ray-of-frost"] = ("1d8", "холод"),
            ["sacred-flame"] = ("1d8", "сияние"),
            ["thorn-whip"] = ("1d6", "колющий"),
            ["poison-spray"] = ("1d12", "яд"),
            ["burning-hands"] = ("3d6", "огонь"),
            ["thunderwave"] = ("2d8", "гром"),
            ["magic-missile"] = ("3d4+3", "силовой"),
            ["chromatic-orb"] = ("3d8", "элементальный"),
            ["guiding-bolt"] = ("4d6", "сияние"),
            ["inflict-wounds"] = ("3d10", "некротический"),
            ["witch-bolt"] = ("1d12", "молния"),
            ["scorching-ray"] = ("2d6", "огонь"),
            ["shatter"] = ("3d8", "гром"),
            ["melfs-acid-arrow"] = ("4d4", "кислота"),
            ["fireball"] = ("8d6", "огонь"),
            ["lightning-bolt"] = ("8d6", "молния"),
            ["blight"] = ("8d8", "некротический"),
            ["cone-of-cold"] = ("8d8", "холод")
        };

        if (map.TryGetValue(slug, out var profile))
        {
            damageDice = profile.dice;
            damageType = profile.type;
            return true;
        }

        var text = $"{summary} {description}";
        var diceMatch = System.Text.RegularExpressions.Regex.Match(text, @"(?<dice>\d+d\d+(\s*[\+\-]\s*\d+)?)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (diceMatch.Success)
        {
            damageDice = diceMatch.Groups["dice"].Value.Replace(" ", string.Empty);
            damageType = InferDamageType(text);
            return true;
        }

        damageDice = null;
        damageType = null;
        return false;
    }

    private static string InferDamageType(string text)
    {
        var normalized = text.ToLowerInvariant();
        if (normalized.Contains("огн")) return "огонь";
        if (normalized.Contains("холод")) return "холод";
        if (normalized.Contains("молн")) return "молния";
        if (normalized.Contains("гром")) return "гром";
        if (normalized.Contains("кисл")) return "кислота";
        if (normalized.Contains("яд")) return "яд";
        if (normalized.Contains("некрот")) return "некротический";
        if (normalized.Contains("сиян")) return "сияние";
        if (normalized.Contains("псих")) return "психический";
        if (normalized.Contains("силов")) return "силовой";
        if (normalized.Contains("колющ")) return "колющий";
        if (normalized.Contains("рубящ")) return "рубящий";
        if (normalized.Contains("дробящ")) return "дробящий";
        return "урон";
    }

    private static bool TryRollDamage(string sourceDice, out string expression, out int result)
    {
        expression = sourceDice;
        result = 0;

        var normalized = sourceDice.Trim().ToLowerInvariant().Replace(" ", string.Empty);
        var match = System.Text.RegularExpressions.Regex.Match(
            normalized,
            @"^(?:(?<count>\d+)d(?<sides>\d+)|(?<flat>\d+))(?:(?<sign>[+-])(?<bonus>\d+))?$");
        if (!match.Success)
        {
            return false;
        }

        var hasDice = match.Groups["count"].Success && match.Groups["sides"].Success;
        var hasFlat = match.Groups["flat"].Success;
        var modifier = 0;
        if (match.Groups["bonus"].Success)
        {
            modifier = int.Parse(match.Groups["bonus"].Value);
            if (match.Groups["sign"].Value == "-")
            {
                modifier *= -1;
            }
        }

        if (hasFlat)
        {
            var flatValue = int.Parse(match.Groups["flat"].Value);
            result = Math.Max(0, flatValue + modifier);
            return true;
        }

        if (!hasDice)
        {
            return false;
        }

        var count = int.Parse(match.Groups["count"].Value);
        var sides = int.Parse(match.Groups["sides"].Value);
        if (count <= 0 || sides <= 0)
        {
            return false;
        }

        var sum = 0;
        for (var index = 0; index < count; index++)
        {
            sum += Random.Shared.Next(1, sides + 1);
        }

        result = Math.Max(0, sum + modifier);
        return true;
    }
}
