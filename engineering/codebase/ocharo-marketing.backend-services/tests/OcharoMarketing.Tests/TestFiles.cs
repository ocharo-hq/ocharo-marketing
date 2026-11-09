using System.Collections.Concurrent;
using System.Security.Cryptography;
using Microsoft.Extensions.DependencyInjection;
using WoW.Two.Sdk.Beta.Be.Foundation.Errors;
using WoW.Two.Sdk.Beta.Be.Foundation.Results;
using WoW.Two.Sdk.Beta.Be.Files;
using WoW.Two.Sdk.Beta.Be.Files.References;
using WoW.Two.Sdk.Beta.Be.Files.Store;
using WoW.Two.Sdk.Beta.Be.Files.Uploads;
using WoW.Two.Sdk.Beta.Be.Files.Urls;

namespace OcharoMarketing.Tests;

/// <summary>An in-memory Files tenant, copied from Catalogue's tests: uploads keep their bytes, idempotency key and app;
/// references attach; links name the file, the asked width and disposition; reads serve the kept bytes. An
/// <see cref="Outage"/> fails every call, as an unreachable Files does.</summary>
public sealed class TestFiles : IFileUploadsBroker, IFileStoreBroker, IFileReferencesBroker, IFileUrlsBroker
{
    /// <summary>Holds the Files root the tests configure.</summary>
    public const string Url = "https://files.example.test/";

    private readonly ConcurrentDictionary<FileId, FileModel> files = new();
    private readonly ConcurrentDictionary<FileId, byte[]> contents = new();
    private readonly ConcurrentDictionary<string, FileId> keys = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<FileId, ConcurrentBag<FileReference>> references = new();

    public int Uploads { get; private set; }

    /// <summary>Gets or sets the error every call answers with; none for a reachable Files.</summary>
    public AppError? Outage { get; set; }

    /// <summary>Puts this tenant behind every Files broker a Marketing host resolves.</summary>
    public void Register(IServiceCollection services)
    {
        services.AddSingleton(this);
        services.AddSingleton<IFileUploadsBroker>(this);
        services.AddSingleton<IFileStoreBroker>(this);
        services.AddSingleton<IFileReferencesBroker>(this);
        services.AddSingleton<IFileUrlsBroker>(this);
    }

    /// <summary>Lists the files an app holds.</summary>
    public IReadOnlyList<FileModel> Of(string appId) => files.Values.Where(file => file.AppId == appId).ToList();

    public static Uri Link(FileId id, int? width, FileDisposition disposition = FileDisposition.Inline) =>
        new($"{Url}f/{id}?w={width}{(disposition == FileDisposition.Attachment ? "&d=attachment" : "")}");

    public FileModel Model(string id) => files[FileId.TryParse(id, out var parsed) ? parsed : default];

    public IReadOnlyCollection<FileReference> ReferencesOf(FileId id) => references.TryGetValue(id, out var bag) ? bag : [];

    /// <summary>Replaces a file's bytes behind its back, as a damaged store would.</summary>
    public void Tamper(FileId id) => contents[id] = [.. contents[id], 0];

    /// <summary>Holds a file as the migration would leave it: moved, with its reference attached.</summary>
    public FileModel Add(byte[] bytes, string appId, FileReference reference, string contentType = "image/png")
    {
        var file = NewModel(bytes, $"{appId}-moved", appId, contentType);
        files[file.Id] = file;
        contents[file.Id] = bytes;
        references.GetOrAdd(file.Id, _ => []).Add(reference);
        return file;
    }

    public async Task<Result<FileModel>> UploadAsync(Stream content, UploadSpec spec, CancellationToken cancellationToken = default)
    {
        if (Outage is { } outage)
            return Result<FileModel>.Fail(outage);
        if (spec.IdempotencyKey is { } key && keys.TryGetValue(key, out var first))
            return Result<FileModel>.Ok(files[first]);
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        var file = NewModel(buffer.ToArray(), spec.Name, spec.AppId, spec.ContentType ?? "application/octet-stream");
        files[file.Id] = file;
        contents[file.Id] = buffer.ToArray();
        Uploads++;
        if (spec.IdempotencyKey is { } idempotency)
            keys[idempotency] = file.Id;
        return Result<FileModel>.Ok(file);
    }

    public Task<Result> AttachAsync(FileId id, FileReference reference, CancellationToken cancellationToken = default)
    {
        if (Outage is { } outage)
            return Task.FromResult(Result.Fail(outage));
        references.GetOrAdd(id, _ => []).Add(reference);
        return Task.FromResult(Result.Ok());
    }

    public Task<Result<SignedFileUrl>> IssueAsync(FileId id, UrlSpec spec, CancellationToken cancellationToken = default) =>
        Task.FromResult(Outage is { } outage ? Result<SignedFileUrl>.Fail(outage)
            : files.ContainsKey(id)
                ? Result<SignedFileUrl>.Ok(new SignedFileUrl(Link(id, spec.Width, spec.Disposition), DateTimeOffset.UtcNow + spec.Lifetime))
                : Result<SignedFileUrl>.Fail(AppErrorFactory.NotFound("No such file.")));

    public Task<Result<FileModel>> GetAsync(FileId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(files.TryGetValue(id, out var file)
            ? Result<FileModel>.Ok(file)
            : Result<FileModel>.Fail(AppErrorFactory.NotFound("No such file.")));

    public Task<Result<FilePage>> ListAsync(FileQuery query, CancellationToken cancellationToken = default) =>
        Task.FromResult(Outage is { } outage ? Result<FilePage>.Fail(outage)
            : Result<FilePage>.Ok(new FilePage(files.Values.Where(file => query.AppId is null || file.AppId == query.AppId).ToList(), null)));

    public Task<Result<FileContent>> OpenReadAsync(FileId id, ByteRange? range = null, CancellationToken cancellationToken = default) =>
        Task.FromResult(Outage is { } outage ? Result<FileContent>.Fail(outage)
            : files.TryGetValue(id, out var file) && contents.TryGetValue(id, out var bytes)
                ? Result<FileContent>.Ok(new FileContent(new MemoryStream(bytes, false), file.ContentType, bytes.LongLength, bytes.LongLength, null))
                : Result<FileContent>.Fail(AppErrorFactory.NotFound("No such file.")));

    public Task<Result<FileModel>> UpdateAsync(FileId id, FilePatch patch, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task<Result<FileModel>> DeleteAsync(FileId id, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task<Result<FileModel>> RestoreAsync(FileId id, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task<Result> DetachAsync(FileId id, FileReference reference, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task<Result<IReadOnlyList<FileReference>>> ListAsync(FileId id, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task<Result<Uri>> GetPublicUrlAsync(FileId id, int? width = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    private static FileModel NewModel(byte[] bytes, string name, string? appId, string contentType) => new()
    {
        Id = FileId.New(),
        TenantId = "ocharo",
        AppId = appId,
        Name = name,
        ContentType = contentType,
        Kind = contentType.StartsWith("image/", StringComparison.Ordinal) ? FileKind.Image : FileKind.Document,
        Size = bytes.LongLength,
        Sha256 = Convert.ToHexStringLower(SHA256.HashData(bytes)),
        Visibility = FileVisibility.Private,
        State = FileState.Ready,
        CreatedAtUtc = DateTimeOffset.UtcNow,
    };
}
