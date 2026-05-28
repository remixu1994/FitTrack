using System.Text.Json;
using FitTrack.Copilot.Data;
using Microsoft.EntityFrameworkCore;

namespace FitTrack.Copilot.Service;

public class ConversationService : IConversationService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IWebHostEnvironment _environment;

    public ConversationService(ApplicationDbContext dbContext, IWebHostEnvironment environment)
    {
        _dbContext = dbContext;
        _environment = environment;
    }

    public async Task<IReadOnlyList<ConversationThread>> ListThreadsAsync(string userId, CancellationToken ct = default)
        => await _dbContext.ConversationThreads
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.UpdatedAt)
            .ToListAsync(ct);

    public async Task<ConversationThread> CreateThreadAsync(string userId, string title, CancellationToken ct = default)
    {
        var thread = new ConversationThread
        {
            UserId = userId,
            Title = string.IsNullOrWhiteSpace(title) ? $"Session {DateTime.UtcNow:u}" : title.Trim()
        };

        _dbContext.ConversationThreads.Add(thread);
        await _dbContext.SaveChangesAsync(ct);
        return thread;
    }

    public async Task<ConversationThread?> GetThreadAsync(string userId, string threadId, CancellationToken ct = default)
        => await _dbContext.ConversationThreads
            .Include(t => t.Messages.OrderBy(m => m.TurnIndex))
                .ThenInclude(m => m.Attachments)
            .Include(t => t.Snapshots.OrderByDescending(s => s.CreatedAt))
            .FirstOrDefaultAsync(t => t.Id == threadId && t.UserId == userId, ct);

    public async Task DeleteThreadAsync(string userId, string threadId, CancellationToken ct = default)
    {
        var thread = await _dbContext.ConversationThreads.FirstOrDefaultAsync(t => t.Id == threadId && t.UserId == userId, ct);
        if (thread is null)
        {
            return;
        }

        _dbContext.ConversationThreads.Remove(thread);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task<ConversationMessage> CreateMessageAsync(string threadId, string role, string kind, string? contentText, object? contentJson, CancellationToken ct = default)
    {
        var turnIndex = (await _dbContext.ConversationMessages
            .Where(m => m.ThreadId == threadId)
            .MaxAsync(m => (int?)m.TurnIndex, ct) ?? 0) + 1;

        var message = new ConversationMessage
        {
            ThreadId = threadId,
            Role = role,
            Kind = kind,
            ContentText = string.IsNullOrWhiteSpace(contentText) ? null : contentText.Trim(),
            ContentJson = contentJson is null ? null : JsonSerializer.Serialize(contentJson),
            TurnIndex = turnIndex
        };

        _dbContext.ConversationMessages.Add(message);
        await TouchThreadAsync(threadId, ct);
        await _dbContext.SaveChangesAsync(ct);
        return message;
    }

    public async Task<ConversationAttachment> CreateAttachmentAsync(
        string userId,
        string threadId,
        string messageId,
        string kind,
        string dataUrl,
        string? fileName,
        string? mimeType,
        long? fileSize,
        CancellationToken ct = default)
    {
        var threadExists = await _dbContext.ConversationThreads.AnyAsync(t => t.Id == threadId && t.UserId == userId, ct);
        if (!threadExists)
        {
            throw new InvalidOperationException("Thread not found for attachment storage.");
        }

        var (effectiveMimeType, bytes) = ParseDataUrl(dataUrl, mimeType);
        var attachmentId = Guid.NewGuid().ToString("N");
        var relativePath = BuildRelativeStoragePath(userId, threadId, attachmentId, fileName, effectiveMimeType);
        var absolutePath = ResolveAbsolutePath(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);
        await File.WriteAllBytesAsync(absolutePath, bytes, ct);

        var attachment = new ConversationAttachment
        {
            Id = attachmentId,
            ThreadId = threadId,
            MessageId = messageId,
            Kind = string.IsNullOrWhiteSpace(kind) ? "meal_photo" : kind,
            FileName = NormalizeFileName(fileName),
            MimeType = effectiveMimeType,
            FileSize = fileSize ?? bytes.LongLength,
            StoragePath = relativePath.Replace('\\', '/')
        };

        _dbContext.ConversationAttachments.Add(attachment);
        await TouchThreadAsync(threadId, ct);
        await _dbContext.SaveChangesAsync(ct);
        return attachment;
    }

    public async Task<NutritionSnapshot?> CreateSnapshotAsync(string threadId, string messageId, AgentNutritionSnapshot? snapshot, CancellationToken ct = default)
    {
        if (snapshot is null)
        {
            return null;
        }

        var entity = new NutritionSnapshot
        {
            ThreadId = threadId,
            MessageId = messageId,
            TrainingType = snapshot.TrainingType,
            DayType = snapshot.DayType,
            TargetCalories = snapshot.TargetCalories,
            TargetProteinG = snapshot.TargetProteinG,
            TargetCarbsG = snapshot.TargetCarbsG,
            TargetFatG = snapshot.TargetFatG,
            ConsumedCalories = snapshot.ConsumedCalories,
            ConsumedProteinG = snapshot.ConsumedProteinG,
            ConsumedCarbsG = snapshot.ConsumedCarbsG,
            ConsumedFatG = snapshot.ConsumedFatG,
            RemainingCalories = snapshot.RemainingCalories,
            RemainingProteinG = snapshot.RemainingProteinG,
            RemainingCarbsG = snapshot.RemainingCarbsG,
            RemainingFatG = snapshot.RemainingFatG,
            NextSuggestions = snapshot.NextSuggestions is null ? null : JsonSerializer.Serialize(snapshot.NextSuggestions)
        };

        _dbContext.NutritionSnapshots.Add(entity);
        await TouchThreadAsync(threadId, ct);
        await _dbContext.SaveChangesAsync(ct);
        return entity;
    }

    public async Task<IReadOnlyList<ConversationMessage>> GetMessagesAsync(string threadId, CancellationToken ct = default)
        => await _dbContext.ConversationMessages
            .Where(m => m.ThreadId == threadId)
            .Include(m => m.Attachments.OrderBy(a => a.CreatedAt))
            .OrderBy(m => m.TurnIndex)
            .ToListAsync(ct);

    public async Task<(ConversationAttachment Attachment, Stream Stream)?> OpenAttachmentAsync(string userId, string attachmentId, CancellationToken ct = default)
    {
        var attachment = await _dbContext.ConversationAttachments
            .Include(a => a.Thread)
            .FirstOrDefaultAsync(a => a.Id == attachmentId && a.Thread.UserId == userId, ct);

        if (attachment is null)
        {
            return null;
        }

        var absolutePath = ResolveAbsolutePath(attachment.StoragePath);
        if (!File.Exists(absolutePath))
        {
            return null;
        }

        var stream = new FileStream(absolutePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return (attachment, stream);
    }

    public async Task TouchThreadAsync(string threadId, CancellationToken ct = default)
    {
        var thread = await _dbContext.ConversationThreads.FirstOrDefaultAsync(t => t.Id == threadId, ct);
        if (thread is null)
        {
            return;
        }

        thread.UpdatedAt = DateTime.UtcNow;
    }

    private (string MimeType, byte[] Bytes) ParseDataUrl(string dataUrl, string? fallbackMimeType)
    {
        if (string.IsNullOrWhiteSpace(dataUrl))
        {
            throw new InvalidOperationException("Attachment data URL is required.");
        }

        var parts = dataUrl.Split(',', 2);
        if (parts.Length != 2)
        {
            throw new InvalidOperationException("Attachment data URL is invalid.");
        }

        var header = parts[0];
        var mimeType = fallbackMimeType;
        if (header.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            var headerMime = header[5..].Split(';', 2)[0];
            if (!string.IsNullOrWhiteSpace(headerMime))
            {
                mimeType = headerMime;
            }
        }

        return (mimeType ?? "application/octet-stream", Convert.FromBase64String(parts[1]));
    }

    private string ResolveAbsolutePath(string relativePath)
    {
        var root = Path.GetFullPath(Path.Combine(_environment.ContentRootPath, "Data", "attachments"));
        var combined = Path.GetFullPath(Path.Combine(_environment.ContentRootPath, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        if (!combined.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Attachment path is outside the configured storage root.");
        }

        return combined;
    }

    private static string BuildRelativeStoragePath(string userId, string threadId, string attachmentId, string? fileName, string? mimeType)
    {
        var extension = GetExtension(fileName, mimeType);
        return Path.Combine("Data", "attachments", SanitizeSegment(userId), SanitizeSegment(threadId), $"{attachmentId}{extension}");
    }

    private static string NormalizeFileName(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return "meal-photo";
        }

        var invalid = Path.GetInvalidFileNameChars();
        return new string(fileName.Trim().Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
    }

    private static string GetExtension(string? fileName, string? mimeType)
    {
        var fromName = Path.GetExtension(fileName);
        if (!string.IsNullOrWhiteSpace(fromName))
        {
            return fromName;
        }

        return mimeType?.ToLowerInvariant() switch
        {
            "image/jpeg" or "image/jpg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            "image/gif" => ".gif",
            _ => ".bin"
        };
    }

    private static string SanitizeSegment(string value)
        => string.Join(string.Empty, value.Select(ch => char.IsLetterOrDigit(ch) ? ch : '_'));
}
