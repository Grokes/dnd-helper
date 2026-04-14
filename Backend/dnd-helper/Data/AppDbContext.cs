using dnd_helper.Features.Auth;
using dnd_helper.Features.Characters;
using dnd_helper.Features.Rooms;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace dnd_helper.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<CharacterEntity> Characters => Set<CharacterEntity>();
    public DbSet<RoomEntity> Rooms => Set<RoomEntity>();
    public DbSet<RoomMembershipEntity> RoomMemberships => Set<RoomMembershipEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<CharacterEntity>(entity =>
        {
            entity.ToTable("characters");
            entity.HasKey(item => item.Id);

            entity.Property(item => item.Name).HasMaxLength(120);
            entity.Property(item => item.OwnerUserId).HasMaxLength(450).HasColumnName("owner_user_id");
            entity.Property(item => item.RaceId).HasMaxLength(80).HasColumnName("race_id");
            entity.Property(item => item.ClassId).HasMaxLength(80).HasColumnName("class_id");
            entity.Property(item => item.BackgroundId).HasMaxLength(80).HasColumnName("background_id");
            entity.Property(item => item.Race).HasMaxLength(80);
            entity.Property(item => item.ClassName).HasMaxLength(80);
            entity.Property(item => item.Subclass).HasMaxLength(120);
            entity.Property(item => item.Background).HasMaxLength(120);
            entity.Property(item => item.Alignment).HasMaxLength(80);
            entity.Property(item => item.AbilitiesJson).HasColumnName("abilities_json");
            entity.Property(item => item.BaseAbilitiesJson).HasColumnName("base_abilities_json");
            entity.Property(item => item.BonusAbilitySelectionsJson).HasColumnName("bonus_ability_selections_json");
            entity.Property(item => item.SkillsJson).HasColumnName("skills_json");
            entity.Property(item => item.SpellsJson).HasColumnName("spells_json");
            entity.Property(item => item.InventoryJson).HasColumnName("inventory_json");
            entity.Property(item => item.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(item => item.UpdatedAtUtc).HasColumnName("updated_at_utc");

            entity.HasOne(item => item.OwnerUser)
                .WithMany()
                .HasForeignKey(item => item.OwnerUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RoomEntity>(entity =>
        {
            entity.ToTable("rooms");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Name).HasMaxLength(160);
            entity.Property(item => item.JoinCode).HasMaxLength(12).HasColumnName("join_code");
            entity.Property(item => item.InviteToken).HasMaxLength(64).HasColumnName("invite_token");
            entity.Property(item => item.OwnerUserId).HasMaxLength(450).HasColumnName("owner_user_id");
            entity.Property(item => item.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(item => item.ActiveMemberUserId).HasMaxLength(450).HasColumnName("active_member_user_id");
            entity.Property(item => item.SessionUpdatedAtUtc).HasColumnName("session_updated_at_utc");
            entity.HasIndex(item => item.JoinCode).IsUnique();
            entity.HasIndex(item => item.InviteToken).IsUnique();

            entity.HasOne(item => item.OwnerUser)
                .WithMany()
                .HasForeignKey(item => item.OwnerUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RoomMembershipEntity>(entity =>
        {
            entity.ToTable("room_memberships");
            entity.HasKey(item => new { item.RoomId, item.UserId });
            entity.Property(item => item.UserId).HasMaxLength(450).HasColumnName("user_id");
            entity.Property(item => item.CharacterId).HasColumnName("character_id");
            entity.Property(item => item.Role).HasMaxLength(40).HasColumnName("role");
            entity.Property(item => item.JoinedAtUtc).HasColumnName("joined_at_utc");
            entity.Property(item => item.LastSeenAtUtc).HasColumnName("last_seen_at_utc");

            entity.HasOne(item => item.Room)
                .WithMany(room => room.Members)
                .HasForeignKey(item => item.RoomId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(item => item.User)
                .WithMany()
                .HasForeignKey(item => item.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(item => item.Character)
                .WithMany()
                .HasForeignKey(item => item.CharacterId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(item => item.DisplayName).HasMaxLength(120);
        });

        modelBuilder.Entity<IdentityRole>(entity =>
        {
            entity.Property(item => item.Name).HasMaxLength(120);
        });
    }
}
