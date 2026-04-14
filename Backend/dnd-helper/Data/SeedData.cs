using dnd_helper.Features.Auth;
using dnd_helper.Features.Characters;
using dnd_helper.Features.ReferenceData;
using dnd_helper.Features.Rooms;
using Microsoft.AspNetCore.Identity;

namespace dnd_helper.Data;

public static class SeedData
{
    public static async Task InitializeAsync(
        AppDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        foreach (var roleName in new[] { ApplicationRoles.User, ApplicationRoles.GameMaster })
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        var playerUser = await EnsureUserAsync(
            userManager,
            "player@example.com",
            "Player123!",
            "Игрок",
            ApplicationRoles.User);
        var gameMasterUser = await EnsureUserAsync(
            userManager,
            "gm@example.com",
            "Gamemaster123!",
            "Гейм-мастер",
            ApplicationRoles.GameMaster);

        if (dbContext.Characters.Any() || dbContext.Rooms.Any())
        {
            return;
        }

        var races = CharacterOptionsCatalog.Races.ToDictionary(item => item.Id);
        var classes = CharacterOptionsCatalog.Classes.ToDictionary(item => item.Id);
        var backgrounds = CharacterOptionsCatalog.Backgrounds.ToDictionary(item => item.Id);

        var characters = new[]
        {
            CharacterEntity.FromComputed(CharacterBuilder.Compute(
                "Эларис Вейн",
                races["half-elf"],
                classes["bard"],
                backgrounds["charlatan"],
                5,
                "Хаотично-добрый",
                "Любит выуживать информацию через разговоры и старается решать конфликты дипломатией.",
                [
                    new BaseAbilityScoreDto("STR", 10),
                    new BaseAbilityScoreDto("DEX", 15),
                    new BaseAbilityScoreDto("CON", 12),
                    new BaseAbilityScoreDto("INT", 13),
                    new BaseAbilityScoreDto("WIS", 8),
                    new BaseAbilityScoreDto("CHA", 14)
                ],
                ["DEX", "CON"],
                ["Insight", "History"],
                ["Performance", "Persuasion", "Arcana"],
                ["Healing Word", "Dissonant Whispers", "Enhance Ability", "Hypnotic Pattern"],
                ["Лютня", "Кожаный доспех", "Набор для маскировки", "Дневник контактов"]),
                playerUser.Id),
            CharacterEntity.FromComputed(CharacterBuilder.Compute(
                "Торвальд Эш",
                races["mountain-dwarf"],
                classes["paladin"],
                backgrounds["soldier"],
                4,
                "Законопослушно-добрый",
                "Служит живым щитом отряда и держит дисциплину, даже если остальные склонны к импровизации.",
                [
                    new BaseAbilityScoreDto("STR", 15),
                    new BaseAbilityScoreDto("DEX", 10),
                    new BaseAbilityScoreDto("CON", 14),
                    new BaseAbilityScoreDto("INT", 8),
                    new BaseAbilityScoreDto("WIS", 12),
                    new BaseAbilityScoreDto("CHA", 13)
                ],
                [],
                [],
                ["Religion", "Persuasion"],
                ["Bless", "Shield of Faith", "Cure Wounds"],
                ["Щит", "Кольчуга", "Священный символ", "Походный набор"]),
                gameMasterUser.Id),
            CharacterEntity.FromComputed(CharacterBuilder.Compute(
                "Мира Нокс",
                races["tiefling"],
                classes["sorcerer"],
                backgrounds["sage"],
                3,
                "Нейтральный",
                "Собирает аномальные магические явления и документирует каждый странный эффект своих заклинаний.",
                [
                    new BaseAbilityScoreDto("STR", 8),
                    new BaseAbilityScoreDto("DEX", 14),
                    new BaseAbilityScoreDto("CON", 13),
                    new BaseAbilityScoreDto("INT", 12),
                    new BaseAbilityScoreDto("WIS", 10),
                    new BaseAbilityScoreDto("CHA", 15)
                ],
                [],
                [],
                ["Deception", "Insight"],
                ["Chaos Bolt", "Mage Armor", "Mirror Image", "Detect Magic"],
                ["Компонентная сумка", "Книга наблюдений", "Амулет фокусировки"]),
                playerUser.Id)
        };

        dbContext.Characters.AddRange(characters);
        var room = new RoomEntity
        {
            Id = Guid.NewGuid(),
            Name = "Тестовая игровая комната",
            JoinCode = "DRAGON",
            InviteToken = "room-dragon-invite",
            OwnerUserId = gameMasterUser.Id,
            CreatedAtUtc = DateTime.UtcNow,
            ActiveMemberUserId = playerUser.Id,
            SessionUpdatedAtUtc = DateTime.UtcNow,
            Members =
            [
                new RoomMembershipEntity
                {
                    UserId = gameMasterUser.Id,
                    CharacterId = characters[1].Id,
                    Role = RoomMemberRoles.GameMaster,
                    JoinedAtUtc = DateTime.UtcNow,
                    LastSeenAtUtc = DateTime.UtcNow
                },
                new RoomMembershipEntity
                {
                    UserId = playerUser.Id,
                    CharacterId = characters[0].Id,
                    Role = RoomMemberRoles.Player,
                    JoinedAtUtc = DateTime.UtcNow,
                    LastSeenAtUtc = DateTime.UtcNow
                }
            ]
        };

        dbContext.Rooms.Add(room);
        await dbContext.SaveChangesAsync();
    }

    private static async Task<ApplicationUser> EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string password,
        string displayName,
        string role)
    {
        var existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser is not null)
        {
            if (!await userManager.IsInRoleAsync(existingUser, role))
            {
                await userManager.AddToRoleAsync(existingUser, role);
            }

            return existingUser;
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            DisplayName = displayName,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"Не удалось создать сидового пользователя {email}: {errors}");
        }

        await userManager.AddToRoleAsync(user, role);
        return user;
    }
}
