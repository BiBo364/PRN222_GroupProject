using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Assignment1_Repository.Models;

public partial class RagEduContext : DbContext
{
    public RagEduContext()
    {
    }

    public RagEduContext(DbContextOptions<RagEduContext> options)
        : base(options)
    {
    }

    public virtual DbSet<BenchmarkResult> BenchmarkResults { get; set; }

    public virtual DbSet<BenchmarkRun> BenchmarkRuns { get; set; }

    public virtual DbSet<BenchmarkSummary> BenchmarkSummaries { get; set; }

    public virtual DbSet<Chapter> Chapters { get; set; }

    public virtual DbSet<Chunk> Chunks { get; set; }

    public virtual DbSet<ChunkingConfig> ChunkingConfigs { get; set; }

    public virtual DbSet<Document> Documents { get; set; }

    public virtual DbSet<Embedding> Embeddings { get; set; }

    public virtual DbSet<EmbeddingModel> EmbeddingModels { get; set; }

    public virtual DbSet<LoginLog> LoginLogs { get; set; }

    public virtual DbSet<Message> Messages { get; set; }

    public virtual DbSet<MessageCitation> MessageCitations { get; set; }

    public virtual DbSet<Permission> Permissions { get; set; }

    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Session> Sessions { get; set; }

    public virtual DbSet<Subject> Subjects { get; set; }

    public virtual DbSet<TestQuestion> TestQuestions { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }

    public virtual DbSet<PaymentTicket> PaymentTickets { get; set; }

    public virtual DbSet<UserSubscription> UserSubscriptions { get; set; }

    public virtual DbSet<StudentChatUsage> StudentChatUsages { get; set; }

    

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BenchmarkResult>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__benchmar__3213E83F7EF06FBE");

            entity.ToTable("benchmark_results");

            entity.HasIndex(e => e.RunId, "idx_bench_results_run");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AnswerCorrectness).HasColumnName("answer_correctness");
            entity.Property(e => e.AnswerRelevancy).HasColumnName("answer_relevancy");
            entity.Property(e => e.ContextPrecision).HasColumnName("context_precision");
            entity.Property(e => e.ContextRecall).HasColumnName("context_recall");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.ErrorMsg).HasColumnName("error_msg");
            entity.Property(e => e.Faithfulness).HasColumnName("faithfulness");
            entity.Property(e => e.GeneratedAnswer).HasColumnName("generated_answer");
            entity.Property(e => e.LatencyMs).HasColumnName("latency_ms");
            entity.Property(e => e.QuestionId).HasColumnName("question_id");
            entity.Property(e => e.RetrievedChunkIds).HasColumnName("retrieved_chunk_ids");
            entity.Property(e => e.RunId).HasColumnName("run_id");

            entity.HasOne(d => d.Question).WithMany(p => p.BenchmarkResults)
                .HasForeignKey(d => d.QuestionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__benchmark__quest__0E6E26BF");

            entity.HasOne(d => d.Run).WithMany(p => p.BenchmarkResults)
                .HasForeignKey(d => d.RunId)
                .HasConstraintName("FK__benchmark__run_i__0D7A0286");
        });

        modelBuilder.Entity<BenchmarkRun>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__benchmar__3213E83FE727A584");

            entity.ToTable("benchmark_runs");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ChunkingConfigId).HasColumnName("chunking_config_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.EmbeddingModelId).HasColumnName("embedding_model_id");
            entity.Property(e => e.ExtraParams).HasColumnName("extra_params");
            entity.Property(e => e.FinishedAt).HasColumnName("finished_at");
            entity.Property(e => e.LlmModel)
                .HasMaxLength(100)
                .HasDefaultValue("claude-sonnet-4-20250514")
                .HasColumnName("llm_model");
            entity.Property(e => e.Name)
                .HasMaxLength(200)
                .HasColumnName("name");
            entity.Property(e => e.StartedAt).HasColumnName("started_at");
            entity.Property(e => e.Status)
                .HasMaxLength(10)
                .HasDefaultValue("pending")
                .HasColumnName("status");
            entity.Property(e => e.TopK)
                .HasDefaultValue(5)
                .HasColumnName("top_k");
            entity.Property(e => e.UseReranker)
                .HasDefaultValue(false)
                .HasColumnName("use_reranker");

            entity.HasOne(d => d.ChunkingConfig).WithMany(p => p.BenchmarkRuns)
                .HasForeignKey(d => d.ChunkingConfigId)
                .HasConstraintName("FK__benchmark__chunk__09A971A2");

            entity.HasOne(d => d.EmbeddingModel).WithMany(p => p.BenchmarkRuns)
                .HasForeignKey(d => d.EmbeddingModelId)
                .HasConstraintName("FK__benchmark__embed__08B54D69");
        });

        modelBuilder.Entity<BenchmarkSummary>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__benchmar__3213E83FC33A0F75");

            entity.ToTable("benchmark_summaries");

            entity.HasIndex(e => e.RunId, "UQ__benchmar__7D3D901AB4A3C819").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AvgAnswerCorrectness).HasColumnName("avg_answer_correctness");
            entity.Property(e => e.AvgAnswerRelevancy).HasColumnName("avg_answer_relevancy");
            entity.Property(e => e.AvgContextPrecision).HasColumnName("avg_context_precision");
            entity.Property(e => e.AvgContextRecall).HasColumnName("avg_context_recall");
            entity.Property(e => e.AvgFaithfulness).HasColumnName("avg_faithfulness");
            entity.Property(e => e.AvgLatencyMs).HasColumnName("avg_latency_ms");
            entity.Property(e => e.ComputedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("computed_at");
            entity.Property(e => e.P95LatencyMs).HasColumnName("p95_latency_ms");
            entity.Property(e => e.RunId).HasColumnName("run_id");
            entity.Property(e => e.TotalQuestions)
                .HasDefaultValue(0)
                .HasColumnName("total_questions");

            entity.HasOne(d => d.Run).WithOne(p => p.BenchmarkSummary)
                .HasForeignKey<BenchmarkSummary>(d => d.RunId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__benchmark__run_i__14270015");
        });

        modelBuilder.Entity<Chapter>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__chapters__3213E83FC7623125");

            entity.ToTable("chapters");

            entity.HasIndex(e => e.SubjectId, "idx_chapters_subject");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Number).HasColumnName("number");
            entity.Property(e => e.SubjectId).HasColumnName("subject_id");
            entity.Property(e => e.Title)
                .HasMaxLength(300)
                .HasColumnName("title");

            entity.HasOne(d => d.Subject).WithMany(p => p.Chapters)
                .HasForeignKey(d => d.SubjectId)
                .HasConstraintName("FK__chapters__subjec__48CFD27E");
        });

        modelBuilder.Entity<Chunk>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__chunks__3213E83FF88130C5");

            entity.ToTable("chunks");

            entity.HasIndex(e => e.ChunkingConfigId, "idx_chunks_config");

            entity.HasIndex(e => e.DocumentId, "idx_chunks_document");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CharEnd).HasColumnName("char_end");
            entity.Property(e => e.CharStart).HasColumnName("char_start");
            entity.Property(e => e.ChunkIndex).HasColumnName("chunk_index");
            entity.Property(e => e.ChunkingConfigId).HasColumnName("chunking_config_id");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.DocumentId).HasColumnName("document_id");
            entity.Property(e => e.Metadata).HasColumnName("metadata");
            entity.Property(e => e.PageNumber).HasColumnName("page_number");
            entity.Property(e => e.TokenCount).HasColumnName("token_count");

            entity.HasOne(d => d.ChunkingConfig).WithMany(p => p.Chunks)
                .HasForeignKey(d => d.ChunkingConfigId)
                .HasConstraintName("FK__chunks__chunking__628FA481");

            entity.HasOne(d => d.Document).WithMany(p => p.Chunks)
                .HasForeignKey(d => d.DocumentId)
                .HasConstraintName("FK__chunks__document__619B8048");
        });

        modelBuilder.Entity<ChunkingConfig>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__chunking__3213E83F293B77C5");

            entity.ToTable("chunking_configs");

            entity.HasIndex(e => e.Name, "UQ__chunking__72E12F1B9A9FA9C9").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ChunkOverlap)
                .HasDefaultValue(50)
                .HasColumnName("chunk_overlap");
            entity.Property(e => e.ChunkSize)
                .HasDefaultValue(512)
                .HasColumnName("chunk_size");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.Params).HasColumnName("params");
            entity.Property(e => e.Strategy)
                .HasMaxLength(20)
                .HasColumnName("strategy");
        });

        modelBuilder.Entity<Document>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__document__3213E83F4F8BB70F");

            entity.ToTable("documents");

            entity.HasIndex(e => e.Status, "idx_documents_status");

            entity.HasIndex(e => e.SubjectId, "idx_documents_subject");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ChapterId).HasColumnName("chapter_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.ErrorMsg).HasColumnName("error_msg");
            entity.Property(e => e.FileHash)
                .HasMaxLength(64)
                .HasColumnName("file_hash");
            entity.Property(e => e.FileSize).HasColumnName("file_size");
            entity.Property(e => e.FileType)
                .HasMaxLength(10)
                .HasColumnName("file_type");
            entity.Property(e => e.Filename)
                .HasMaxLength(255)
                .HasColumnName("filename");
            entity.Property(e => e.IndexedAt).HasColumnName("indexed_at");
            entity.Property(e => e.OriginalName)
                .HasMaxLength(500)
                .HasColumnName("original_name");
            entity.Property(e => e.PageCount).HasColumnName("page_count");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("pending")
                .HasColumnName("status");
            entity.Property(e => e.StoragePath)
                .HasMaxLength(500)
                .HasColumnName("storage_path");
            entity.Property(e => e.SubjectId).HasColumnName("subject_id");
            entity.Property(e => e.UploadedBy).HasColumnName("uploaded_by");

            entity.HasOne(d => d.Chapter).WithMany(p => p.Documents)
                .HasForeignKey(d => d.ChapterId)
                .HasConstraintName("FK__documents__chapt__5070F446");

            entity.HasOne(d => d.Subject).WithMany(p => p.Documents)
                .HasForeignKey(d => d.SubjectId)
                .HasConstraintName("FK__documents__subje__4F7CD00D");

            entity.HasOne(d => d.UploadedByNavigation).WithMany(p => p.Documents)
                .HasForeignKey(d => d.UploadedBy)
                .HasConstraintName("FK__documents__uploa__5165187F");
        });

        modelBuilder.Entity<Embedding>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__embeddin__3213E83F5B06BD53");

            entity.ToTable("embeddings");

            entity.HasIndex(e => e.ChunkId, "idx_embeddings_chunk");

            entity.HasIndex(e => e.EmbeddingModelId, "idx_embeddings_model");

            entity.HasIndex(e => new { e.ChunkId, e.EmbeddingModelId }, "uq_chunk_model").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ChunkId).HasColumnName("chunk_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.EmbeddingModelId).HasColumnName("embedding_model_id");
            entity.Property(e => e.Vector).HasColumnName("vector");

            entity.HasOne(d => d.Chunk).WithMany(p => p.Embeddings)
                .HasForeignKey(d => d.ChunkId)
                .HasConstraintName("FK__embedding__chunk__6754599E");

            entity.HasOne(d => d.EmbeddingModel).WithMany(p => p.Embeddings)
                .HasForeignKey(d => d.EmbeddingModelId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__embedding__embed__68487DD7");
        });

        modelBuilder.Entity<EmbeddingModel>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__embeddin__3213E83FE2B4336E");

            entity.ToTable("embedding_models");

            entity.HasIndex(e => e.Name, "UQ__embeddin__72E12F1B71D46AC5").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ApiKeyEnv)
                .HasMaxLength(100)
                .HasColumnName("api_key_env");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Dimension).HasColumnName("dimension");
            entity.Property(e => e.IsFree)
                .HasDefaultValue(true)
                .HasColumnName("is_free");
            entity.Property(e => e.Language)
                .HasMaxLength(20)
                .HasDefaultValue("multilingual")
                .HasColumnName("language");
            entity.Property(e => e.ModelId)
                .HasMaxLength(200)
                .HasColumnName("model_id");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.Provider)
                .HasMaxLength(50)
                .HasColumnName("provider");
        });

        modelBuilder.Entity<LoginLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__login_lo__3213E83F5E0DA565");

            entity.ToTable("login_logs");

            entity.HasIndex(e => e.UserId, "idx_login_user");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.IpAddress)
                .HasMaxLength(45)
                .HasColumnName("ip_address");
            entity.Property(e => e.Status)
                .HasMaxLength(10)
                .HasDefaultValue("success")
                .HasColumnName("status");
            entity.Property(e => e.UserAgent)
                .HasMaxLength(500)
                .HasColumnName("user_agent");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.LoginLogs)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__login_log__user___3F466844");
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__messages__3213E83FC310585C");

            entity.ToTable("messages");

            entity.HasIndex(e => e.SessionId, "idx_messages_session");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.Role)
                .HasMaxLength(10)
                .HasColumnName("role");
            entity.Property(e => e.SessionId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("session_id");

            entity.HasOne(d => d.Session).WithMany(p => p.Messages)
                .HasForeignKey(d => d.SessionId)
                .HasConstraintName("FK__messages__sessio__73BA3083");
        });

        modelBuilder.Entity<MessageCitation>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__message___3213E83FCC55B978");

            entity.ToTable("message_citations");

            entity.HasIndex(e => e.MessageId, "idx_citations_message");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ChunkId).HasColumnName("chunk_id");
            entity.Property(e => e.MessageId).HasColumnName("message_id");
            entity.Property(e => e.RankOrder).HasColumnName("rank_order");
            entity.Property(e => e.SimilarityScore).HasColumnName("similarity_score");
            entity.Property(e => e.WasUsed)
                .HasDefaultValue(true)
                .HasColumnName("was_used");

            entity.HasOne(d => d.Chunk).WithMany(p => p.MessageCitations)
                .HasForeignKey(d => d.ChunkId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__message_c__chunk__787EE5A0");

            entity.HasOne(d => d.Message).WithMany(p => p.MessageCitations)
                .HasForeignKey(d => d.MessageId)
                .HasConstraintName("FK__message_c__messa__778AC167");
        });

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__permissi__3213E83FD1E2D352");

            entity.ToTable("permissions");

            entity.HasIndex(e => e.Code, "UQ__permissi__357D4CF97ADC590C").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code)
                .HasMaxLength(100)
                .HasColumnName("code");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Label)
                .HasMaxLength(200)
                .HasColumnName("label");
            entity.Property(e => e.Module)
                .HasMaxLength(50)
                .HasColumnName("module");
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__refresh___3213E83F9F8F4203");

            entity.ToTable("refresh_tokens");

            entity.HasIndex(e => e.TokenHash, "UQ__refresh___9F6BDB135DA1AC7A").IsUnique();

            entity.HasIndex(e => e.UserId, "idx_token_user");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.Revoked)
                .HasDefaultValue(false)
                .HasColumnName("revoked");
            entity.Property(e => e.TokenHash)
                .HasMaxLength(255)
                .HasColumnName("token_hash");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.RefreshTokens)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__refresh_t__user___44FF419A");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__roles__3213E83F4AAA8BDF");

            entity.ToTable("roles");

            entity.HasIndex(e => e.Name, "UQ__roles__72E12F1BA36136EE").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Label)
                .HasMaxLength(100)
                .HasColumnName("label");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("name");

            entity.HasMany(d => d.Permissions).WithMany(p => p.Roles)
                .UsingEntity<Dictionary<string, object>>(
                    "RolePermission",
                    r => r.HasOne<Permission>().WithMany()
                        .HasForeignKey("PermissionId")
                        .HasConstraintName("FK__role_perm__permi__2D27B809"),
                    l => l.HasOne<Role>().WithMany()
                        .HasForeignKey("RoleId")
                        .HasConstraintName("FK__role_perm__role___2C3393D0"),
                    j =>
                    {
                        j.HasKey("RoleId", "PermissionId").HasName("PK__role_per__C85A54639E9E348D");
                        j.ToTable("role_permissions");
                        j.IndexerProperty<int>("RoleId").HasColumnName("role_id");
                        j.IndexerProperty<int>("PermissionId").HasColumnName("permission_id");
                    });
        });

        modelBuilder.Entity<Session>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__sessions__3213E83FAFE8AC79");

            entity.ToTable("sessions");

            entity.Property(e => e.Id)
                .HasMaxLength(36)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.IsArchived)
                .HasDefaultValue(false)
                .HasColumnName("is_archived");
            entity.Property(e => e.SubjectId).HasColumnName("subject_id");
            entity.Property(e => e.Title)
                .HasMaxLength(500)
                .HasColumnName("title");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId)
                .HasMaxLength(100)
                .HasDefaultValue("anonymous")
                .HasColumnName("user_id");

            entity.HasOne(d => d.Subject).WithMany(p => p.Sessions)
                .HasForeignKey(d => d.SubjectId)
                .HasConstraintName("FK__sessions__subjec__6EF57B66");
        });

        modelBuilder.Entity<Subject>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__subjects__3213E83FCC974B0A");

            entity.ToTable("subjects");

            entity.HasIndex(e => e.Code, "UQ__subjects__357D4CF995E5BEAD").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code)
                .HasMaxLength(20)
                .HasColumnName("code");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Name)
                .HasMaxLength(200)
                .HasColumnName("name");
        });

        modelBuilder.Entity<TestQuestion>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__test_que__3213E83F3356F1E6");

            entity.ToTable("test_questions");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Category)
                .HasMaxLength(50)
                .HasColumnName("category");
            entity.Property(e => e.ChapterId).HasColumnName("chapter_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(100)
                .HasDefaultValue("human")
                .HasColumnName("created_by");
            entity.Property(e => e.Difficulty)
                .HasMaxLength(10)
                .HasDefaultValue("medium")
                .HasColumnName("difficulty");
            entity.Property(e => e.GroundTruth).HasColumnName("ground_truth");
            entity.Property(e => e.GroundTruthChunks).HasColumnName("ground_truth_chunks");
            entity.Property(e => e.Question).HasColumnName("question");
            entity.Property(e => e.SubjectId).HasColumnName("subject_id");

            entity.HasOne(d => d.Chapter).WithMany(p => p.TestQuestions)
                .HasForeignKey(d => d.ChapterId)
                .HasConstraintName("FK__test_ques__chapt__00200768");

            entity.HasOne(d => d.Subject).WithMany(p => p.TestQuestions)
                .HasForeignKey(d => d.SubjectId)
                .HasConstraintName("FK__test_ques__subje__7F2BE32F");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__users__3213E83FD3A1E99E");

            entity.ToTable("users");

            entity.HasIndex(e => e.Email, "UQ__users__AB6E6164AE548D02").IsUnique();

            entity.HasIndex(e => e.Username, "UQ__users__F3DBC5724F00B951").IsUnique();

            entity.HasIndex(e => e.Email, "idx_users_email");

            entity.HasIndex(e => e.RoleId, "idx_users_role");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AvatarUrl)
                .HasMaxLength(500)
                .HasColumnName("avatar_url");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasMaxLength(200)
                .HasColumnName("email");
            entity.Property(e => e.FullName)
                .HasMaxLength(200)
                .HasColumnName("full_name");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.LastLoginAt).HasColumnName("last_login_at");
            entity.Property(e => e.Password)
                .HasMaxLength(100)
                .HasColumnName("password");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.SubjectId).HasColumnName("subject_id");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("updated_at");
            entity.Property(e => e.Username)
                .HasMaxLength(100)
                .HasColumnName("username");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__users__role_id__38996AB5");

            entity.HasOne(d => d.Subject).WithMany(p => p.Users)
                .HasForeignKey(d => d.SubjectId)
                .HasConstraintName("FK__users__subject_i__398D8EEE");
        });

        modelBuilder.Entity<SubscriptionPlan>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_subscription_plans");

            entity.ToTable("subscription_plans");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Price)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("price");
            entity.Property(e => e.DurationDays).HasColumnName("duration_days");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");
        });

        modelBuilder.Entity<PaymentTicket>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_payment_tickets");

            entity.ToTable("payment_tickets");

            entity.HasIndex(e => e.UserId, "idx_payment_tickets_user");
            entity.HasIndex(e => e.Status, "idx_payment_tickets_status");
            entity.HasIndex(e => e.MomoRequestId, "idx_payment_tickets_momo_request_id")
                .HasFilter("[momo_request_id] IS NOT NULL");
            entity.HasIndex(e => e.MomoOrderId, "UQ_payment_tickets_momo_order_id")
                .IsUnique()
                .HasFilter("[momo_order_id] IS NOT NULL");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.PlanId).HasColumnName("plan_id");
            entity.Property(e => e.Amount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("amount");
            entity.Property(e => e.TransferReference)
                .HasMaxLength(500)
                .HasColumnName("transfer_reference");
            entity.Property(e => e.PaymentMethod)
                .HasMaxLength(20)
                .HasColumnName("payment_method");
            entity.Property(e => e.MomoOrderId)
                .HasMaxLength(100)
                .HasColumnName("momo_order_id");
            entity.Property(e => e.MomoRequestId)
                .HasMaxLength(100)
                .HasColumnName("momo_request_id");
            entity.Property(e => e.MomoTransId)
                .HasMaxLength(100)
                .HasColumnName("momo_trans_id");
            entity.Property(e => e.MomoPayUrl)
                .HasMaxLength(1000)
                .HasColumnName("momo_pay_url");
            entity.Property(e => e.MomoResponseJson).HasColumnName("momo_response_json");
            entity.Property(e => e.MomoIpnJson).HasColumnName("momo_ipn_json");
            entity.Property(e => e.MomoResultCode).HasColumnName("momo_result_code");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue(PaymentTicketStatus.Pending)
                .HasColumnName("status");
            entity.Property(e => e.AdminNote).HasColumnName("admin_note");
            entity.Property(e => e.ReviewedBy).HasColumnName("reviewed_by");
            entity.Property(e => e.ReviewedAt).HasColumnName("reviewed_at");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");

            entity.HasOne(d => d.User).WithMany(p => p.PaymentTickets)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_payment_tickets_user");

            entity.HasOne(d => d.Plan).WithMany(p => p.PaymentTickets)
                .HasForeignKey(d => d.PlanId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_payment_tickets_plan");

            entity.HasOne(d => d.ReviewedByNavigation).WithMany(p => p.ReviewedPaymentTickets)
                .HasForeignKey(d => d.ReviewedBy)
                .HasConstraintName("FK_payment_tickets_reviewer");
        });

        modelBuilder.Entity<UserSubscription>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_user_subscriptions");

            entity.ToTable("user_subscriptions");

            entity.HasIndex(e => e.UserId, "idx_user_subscriptions_user");
            entity.HasIndex(e => e.PaymentTicketId, "UQ_user_subscriptions_ticket")
                .IsUnique()
                .HasFilter("[payment_ticket_id] IS NOT NULL");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.PlanId).HasColumnName("plan_id");
            entity.Property(e => e.StartAt).HasColumnName("start_at");
            entity.Property(e => e.EndAt).HasColumnName("end_at");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.PaymentTicketId).HasColumnName("payment_ticket_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");

            entity.HasOne(d => d.User).WithMany(p => p.UserSubscriptions)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_user_subscriptions_user");

            entity.HasOne(d => d.Plan).WithMany(p => p.UserSubscriptions)
                .HasForeignKey(d => d.PlanId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_user_subscriptions_plan");

            entity.HasOne(d => d.PaymentTicket).WithOne(p => p.UserSubscription)
                .HasForeignKey<UserSubscription>(d => d.PaymentTicketId)
                .HasConstraintName("FK_user_subscriptions_ticket");
        });

        modelBuilder.Entity<StudentChatUsage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_student_chat_usages");

            entity.ToTable("student_chat_usages");

            entity.HasIndex(e => new { e.UserId, e.SubjectId, e.WindowStart }, "UQ_student_chat_usages_window")
                .IsUnique();

            entity.HasIndex(e => e.UserId, "idx_student_chat_usages_user");

            entity.HasIndex(e => e.SubjectId, "idx_student_chat_usages_subject");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.SubjectId).HasColumnName("subject_id");
            entity.Property(e => e.WindowStart).HasColumnName("window_start");
            entity.Property(e => e.QuestionCount)
                .HasDefaultValue(0)
                .HasColumnName("question_count");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.User).WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_student_chat_usages_user");

            entity.HasOne(d => d.Subject).WithMany()
                .HasForeignKey(d => d.SubjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_student_chat_usages_subject");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
