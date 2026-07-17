using Microsoft.EntityFrameworkCore;

namespace Assignment1_Repository.Models;

public partial class RagEduContext
{
    public virtual DbSet<QuestionBankItem> QuestionBankItems { get; set; }
    public virtual DbSet<LearningSet> LearningSets { get; set; }
    public virtual DbSet<LearningSetItem> LearningSetItems { get; set; }
    public virtual DbSet<LearningAttempt> LearningAttempts { get; set; }
    public virtual DbSet<LearningAttemptAnswer> LearningAttemptAnswers { get; set; }
    public virtual DbSet<LearningSetVersion> LearningSetVersions { get; set; }
    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        ConfigureQuestionBank(modelBuilder);
        ConfigureLearningSets(modelBuilder);
        ConfigureLearningAttempts(modelBuilder);
        ConfigureLearningSetVersions(modelBuilder);
        ConfigureAuditLogs(modelBuilder);
    }

    private static void ConfigureQuestionBank(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<QuestionBankItem>(entity =>
        {
            entity.ToTable("question_bank_items");
            entity.HasKey(item => item.Id).HasName("PK_question_bank_items");
            entity.HasIndex(item => item.SubjectId, "idx_question_bank_subject");
            entity.HasIndex(item => new { item.SubjectId, item.IsActive }, "idx_question_bank_active");
            entity.HasIndex(item => new { item.SubjectId, item.Difficulty }, "idx_question_bank_difficulty");

            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.SubjectId).HasColumnName("subject_id");
            entity.Property(item => item.ChapterId).HasColumnName("chapter_id");
            entity.Property(item => item.QuestionType).HasMaxLength(30).HasColumnName("question_type");
            entity.Property(item => item.Prompt).HasColumnName("prompt");
            entity.Property(item => item.OptionsJson).HasColumnName("options_json");
            entity.Property(item => item.CorrectAnswer).HasColumnName("correct_answer");
            entity.Property(item => item.Explanation).HasColumnName("explanation");
            entity.Property(item => item.Difficulty).HasMaxLength(20).HasColumnName("difficulty");
            entity.Property(item => item.Topic).HasMaxLength(200).HasColumnName("topic");
            entity.Property(item => item.LearningObjective).HasMaxLength(500).HasColumnName("learning_objective");
            entity.Property(item => item.SourceReferencesJson).HasColumnName("source_references_json");
            entity.Property(item => item.CreatedByUserId).HasColumnName("created_by_user_id");
            entity.Property(item => item.IsAiGenerated).HasDefaultValue(true).HasColumnName("is_ai_generated");
            entity.Property(item => item.AiModel).HasMaxLength(100).HasColumnName("ai_model");
            entity.Property(item => item.IsActive).HasDefaultValue(true).HasColumnName("is_active");
            entity.Property(item => item.CreatedAt).HasDefaultValueSql("getdate()").HasColumnName("created_at");
            entity.Property(item => item.UpdatedAt).HasDefaultValueSql("getdate()").HasColumnName("updated_at");

            entity.HasOne(item => item.Subject).WithMany()
                .HasForeignKey(item => item.SubjectId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_question_bank_subject");
            entity.HasOne(item => item.Chapter).WithMany()
                .HasForeignKey(item => item.ChapterId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_question_bank_chapter");
            entity.HasOne(item => item.CreatedByUser).WithMany()
                .HasForeignKey(item => item.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_question_bank_creator");
        });
    }

    private static void ConfigureLearningSets(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LearningSet>(entity =>
        {
            entity.ToTable("learning_sets");
            entity.HasKey(set => set.Id).HasName("PK_learning_sets");
            entity.HasIndex(set => set.SubjectId, "idx_learning_sets_subject");
            entity.HasIndex(set => new { set.SubjectId, set.IsPublished }, "idx_learning_sets_published");

            entity.Property(set => set.Id).HasColumnName("id");
            entity.Property(set => set.SubjectId).HasColumnName("subject_id");
            entity.Property(set => set.Title).HasMaxLength(300).HasColumnName("title");
            entity.Property(set => set.Description).HasColumnName("description");
            entity.Property(set => set.Instructions).HasColumnName("instructions");
            entity.Property(set => set.ActivityType).HasMaxLength(30).HasColumnName("activity_type");
            entity.Property(set => set.DurationMinutes).HasColumnName("duration_minutes");
            entity.Property(set => set.IsPublished).HasDefaultValue(false).HasColumnName("is_published");
            entity.Property(set => set.ShuffleQuestions).HasDefaultValue(true).HasColumnName("shuffle_questions");
            entity.Property(set => set.ShuffleOptions).HasDefaultValue(true).HasColumnName("shuffle_options");
            entity.Property(set => set.CreatedByUserId).HasColumnName("created_by_user_id");
            entity.Property(set => set.AiModel).HasMaxLength(100).HasColumnName("ai_model");
            entity.Property(set => set.IsDeleted).HasDefaultValue(false).HasColumnName("is_deleted");
            entity.Property(set => set.DeletedAt).HasColumnName("deleted_at");
            entity.Property(set => set.CreatedAt).HasDefaultValueSql("getdate()").HasColumnName("created_at");
            entity.Property(set => set.UpdatedAt).HasDefaultValueSql("getdate()").HasColumnName("updated_at");

            entity.HasOne(set => set.Subject).WithMany()
                .HasForeignKey(set => set.SubjectId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_learning_sets_subject");
            entity.HasOne(set => set.CreatedByUser).WithMany()
                .HasForeignKey(set => set.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_learning_sets_creator");
        });

        modelBuilder.Entity<LearningSetItem>(entity =>
        {
            entity.ToTable("learning_set_items");
            entity.HasKey(item => item.Id).HasName("PK_learning_set_items");
            entity.HasIndex(item => new { item.LearningSetId, item.QuestionBankItemId }, "uq_learning_set_question")
                .IsUnique();
            entity.HasIndex(item => new { item.LearningSetId, item.OrderIndex }, "idx_learning_set_order");

            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.LearningSetId).HasColumnName("learning_set_id");
            entity.Property(item => item.QuestionBankItemId).HasColumnName("question_bank_item_id");
            entity.Property(item => item.OrderIndex).HasColumnName("order_index");
            entity.Property(item => item.Points).HasPrecision(8, 2).HasDefaultValue(1m).HasColumnName("points");

            entity.HasOne(item => item.LearningSet).WithMany(set => set.Items)
                .HasForeignKey(item => item.LearningSetId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_learning_set_items_set");
            entity.HasOne(item => item.QuestionBankItem).WithMany(question => question.LearningSetItems)
                .HasForeignKey(item => item.QuestionBankItemId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_learning_set_items_question");
        });
    }

    private static void ConfigureLearningAttempts(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LearningAttempt>(entity =>
        {
            entity.ToTable("learning_attempts");
            entity.HasKey(attempt => attempt.Id).HasName("PK_learning_attempts");
            entity.HasIndex(attempt => attempt.UserId, "idx_learning_attempts_user");
            entity.HasIndex(attempt => attempt.LearningSetId, "idx_learning_attempts_set");

            entity.Property(attempt => attempt.Id).HasColumnName("id");
            entity.Property(attempt => attempt.LearningSetId).HasColumnName("learning_set_id");
            entity.Property(attempt => attempt.UserId).HasColumnName("user_id");
            entity.Property(attempt => attempt.StartedAt).HasColumnName("started_at");
            entity.Property(attempt => attempt.CompletedAt).HasColumnName("completed_at");
            entity.Property(attempt => attempt.Score).HasPrecision(8, 2).HasColumnName("score");
            entity.Property(attempt => attempt.TotalPoints).HasPrecision(8, 2).HasColumnName("total_points");
            entity.Property(attempt => attempt.CorrectCount).HasColumnName("correct_count");
            entity.Property(attempt => attempt.TotalQuestions).HasColumnName("total_questions");

            entity.HasOne(attempt => attempt.LearningSet).WithMany(set => set.Attempts)
                .HasForeignKey(attempt => attempt.LearningSetId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_learning_attempts_set");
            entity.HasOne(attempt => attempt.User).WithMany()
                .HasForeignKey(attempt => attempt.UserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_learning_attempts_user");
        });

        modelBuilder.Entity<LearningAttemptAnswer>(entity =>
        {
            entity.ToTable("learning_attempt_answers");
            entity.HasKey(answer => answer.Id).HasName("PK_learning_attempt_answers");
            entity.HasIndex(answer => answer.LearningAttemptId, "idx_learning_answers_attempt");
            entity.HasIndex(answer => answer.QuestionBankItemId, "idx_learning_answers_question");

            entity.Property(answer => answer.Id).HasColumnName("id");
            entity.Property(answer => answer.LearningAttemptId).HasColumnName("learning_attempt_id");
            entity.Property(answer => answer.QuestionBankItemId).HasColumnName("question_bank_item_id");
            entity.Property(answer => answer.SelectedAnswer).HasColumnName("selected_answer");
            entity.Property(answer => answer.IsCorrect).HasColumnName("is_correct");
            entity.Property(answer => answer.AwardedPoints).HasPrecision(8, 2).HasColumnName("awarded_points");
            entity.Property(answer => answer.AnsweredAt).HasColumnName("answered_at");

            entity.HasOne(answer => answer.LearningAttempt).WithMany(attempt => attempt.Answers)
                .HasForeignKey(answer => answer.LearningAttemptId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_learning_answers_attempt");
            entity.HasOne(answer => answer.QuestionBankItem).WithMany(question => question.AttemptAnswers)
                .HasForeignKey(answer => answer.QuestionBankItemId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_learning_answers_question");
        });
    }

    private static void ConfigureLearningSetVersions(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LearningSetVersion>(entity =>
        {
            entity.ToTable("learning_set_versions");
            entity.HasKey(version => version.Id).HasName("PK_learning_set_versions");
            entity.HasIndex(
                    version => new { version.LearningSetId, version.VersionNumber },
                    "uq_learning_set_versions_number")
                .IsUnique();
            entity.HasIndex(version => version.CreatedAt, "idx_learning_set_versions_created");

            entity.Property(version => version.Id).HasColumnName("id");
            entity.Property(version => version.LearningSetId).HasColumnName("learning_set_id");
            entity.Property(version => version.VersionNumber).HasColumnName("version_number");
            entity.Property(version => version.SnapshotJson).HasColumnName("snapshot_json");
            entity.Property(version => version.ChangeSummary).HasMaxLength(500).HasColumnName("change_summary");
            entity.Property(version => version.CreatedByUserId).HasColumnName("created_by_user_id");
            entity.Property(version => version.CreatedAt)
                .HasDefaultValueSql("getdate()")
                .HasColumnName("created_at");

            entity.HasOne(version => version.LearningSet).WithMany(set => set.Versions)
                .HasForeignKey(version => version.LearningSetId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_learning_set_versions_set");
            entity.HasOne(version => version.CreatedByUser).WithMany()
                .HasForeignKey(version => version.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_learning_set_versions_creator");
        });
    }

    private static void ConfigureAuditLogs(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("audit_logs");
            entity.HasKey(log => log.Id).HasName("PK_audit_logs");
            entity.HasIndex(log => log.UserId, "idx_audit_logs_user");
            entity.HasIndex(log => log.CreatedAt, "idx_audit_logs_created");
            entity.HasIndex(
                log => new { log.Category, log.Action },
                "idx_audit_logs_category_action");

            entity.Property(log => log.Id).HasColumnName("id");
            entity.Property(log => log.UserId).HasColumnName("user_id");
            entity.Property(log => log.RoleId).HasColumnName("role_id");
            entity.Property(log => log.Action).HasMaxLength(100).HasColumnName("action");
            entity.Property(log => log.Category).HasMaxLength(100).HasColumnName("category");
            entity.Property(log => log.EntityType).HasMaxLength(100).HasColumnName("entity_type");
            entity.Property(log => log.EntityId).HasMaxLength(100).HasColumnName("entity_id");
            entity.Property(log => log.Description).HasMaxLength(1000).HasColumnName("description");
            entity.Property(log => log.DetailsJson).HasColumnName("details_json");
            entity.Property(log => log.IpAddress).HasMaxLength(64).HasColumnName("ip_address");
            entity.Property(log => log.UserAgent).HasMaxLength(500).HasColumnName("user_agent");
            entity.Property(log => log.RequestPath).HasMaxLength(1000).HasColumnName("request_path");
            entity.Property(log => log.HttpMethod).HasMaxLength(10).HasColumnName("http_method");
            entity.Property(log => log.StatusCode).HasColumnName("status_code");
            entity.Property(log => log.TraceIdentifier).HasMaxLength(100).HasColumnName("trace_identifier");
            entity.Property(log => log.CreatedAt)
                .HasDefaultValueSql("getdate()")
                .HasColumnName("created_at");

            entity.HasOne(log => log.User).WithMany()
                .HasForeignKey(log => log.UserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_audit_logs_user");
        });
    }
}
