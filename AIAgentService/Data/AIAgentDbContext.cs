using AIAgentService.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AIAgentService.Data;

public class AIAgentDbContext : DbContext
{
    public AIAgentDbContext(DbContextOptions<AIAgentDbContext> options) : base(options)
    {
    }

    public DbSet<ChatConversation> Conversations => Set<ChatConversation>();
    public DbSet<ChatMessage> Messages => Set<ChatMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ChatConversation>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.HasIndex(c => c.ConversationGuid).IsUnique();
            entity.HasIndex(c => c.UserId);
            entity.Property(c => c.Title).HasMaxLength(255).IsRequired();
            entity.Property(c => c.UserId).HasMaxLength(150);

            entity.HasMany(c => c.Messages)
                  .WithOne(m => m.Conversation)
                  .HasForeignKey(m => m.ConversationId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasKey(m => m.Id);
            entity.HasIndex(m => m.MessageGuid).IsUnique();
            entity.HasIndex(m => m.ConversationId);
            entity.Property(m => m.Sender).HasMaxLength(20).IsRequired();
            entity.Property(m => m.ToolCalled).HasMaxLength(100);
        });
    }
}
