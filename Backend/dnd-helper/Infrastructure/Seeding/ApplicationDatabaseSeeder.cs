using dnd_helper.Infrastructure.Persistence.Postgres;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace dnd_helper.Infrastructure.Seeding;

public sealed class ApplicationDatabaseSeeder(
    AppDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    CharacterCreationService characterCreationService,
    ILogger<ApplicationDatabaseSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await EnsureRolesAsync();

        var playerUser = await EnsureUserAsync("player@example.com", "Player123!", "Игрок", ApplicationRoles.User);
        var gameMasterUser = await EnsureUserAsync("gm@example.com", "Gamemaster123!", "Гейм-мастер", ApplicationRoles.GameMaster);

        if (!await dbContext.Characters.AnyAsync(cancellationToken))
        {
            logger.LogInformation("PostgreSQL: creating demo character...");
            var request = new CreateCharacterRequest(
                Name: "Эларис Вейн",
                RaceId: "half-elf",
                ClassId: "bard",
                BackgroundId: "charlatan",
                Level: 5,
                Alignment: "Хаотично-добрый",
                Notes: "Демо-персонаж, созданный при старте приложения.",
                BaseAbilities:
                [
                    new BaseAbilityScoreDto("STR", 10),
                    new BaseAbilityScoreDto("DEX", 15),
                    new BaseAbilityScoreDto("CON", 12),
                    new BaseAbilityScoreDto("INT", 13),
                    new BaseAbilityScoreDto("WIS", 8),
                    new BaseAbilityScoreDto("CHA", 14)
                ],
                BonusAbilitySelections: ["DEX", "CON"],
                RaceSkillSelections: ["Insight", "History"],
                ClassSkillSelections: ["Performance", "Persuasion", "Arcana"],
                Spells: ["healing-word", "detect-magic"],
                Inventory: []);

            var result = await characterCreationService.BuildCharacterAsync(request, playerUser.Id, cancellationToken);
            if (!result.IsSuccess)
            {
                var details = string.Join("; ", result.Errors!.SelectMany(x => x.Value));
                throw new InvalidOperationException($"Не удалось создать demo-персонажа: {details}");
            }

            dbContext.Characters.Add(result.Character!);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (!await dbContext.Rooms.AnyAsync(cancellationToken))
        {
            logger.LogInformation("PostgreSQL: creating demo room...");
            var character = await dbContext.Characters.OrderBy(x => x.CreatedAtUtc).FirstAsync(cancellationToken);
            dbContext.Rooms.Add(new RoomEntity
            {
                Id = Guid.NewGuid(),
                Name = "Тестовая игровая комната",
                JoinCode = "DRAGON",
                InviteToken = "room-dragon-invite",
                OwnerUserId = gameMasterUser.Id,
                CreatedAtUtc = DateTime.UtcNow,
                Members =
                [
                    new RoomMembershipEntity
                    {
                        UserId = gameMasterUser.Id,
                        Role = RoomMemberRoles.GameMaster,
                        JoinedAtUtc = DateTime.UtcNow
                    },
                    new RoomMembershipEntity
                    {
                        UserId = playerUser.Id,
                        Role = RoomMemberRoles.Player,
                        JoinedAtUtc = DateTime.UtcNow,
                        Characters =
                        [
                            new RoomMembershipCharacterEntity
                            {
                                CharacterId = character.Id
                            }
                        ]
                    }
                ]
            });

            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task EnsureRolesAsync()
    {
        foreach (var roleName in new[] { ApplicationRoles.User, ApplicationRoles.GameMaster })
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }
    }

    private async Task<ApplicationUser> EnsureUserAsync(string email, string password, string displayName, string role)
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
            var details = string.Join("; ", result.Errors.Select(x => x.Description));
            throw new InvalidOperationException($"Не удалось создать пользователя {email}: {details}");
        }

        await userManager.AddToRoleAsync(user, role);
        return user;
    }
}
