using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using OcharoBlog.Application.Blog;
using OcharoBlog.Application.Blog.Repositories;
using OcharoBlog.Application.Blog.Services;
using OcharoBlog.Domain.Blog;
using WoW.Two.Sdk.Beta.Be.Media.BackgroundRemoval;
using WoW.Two.Sdk.Beta.Be.Media.Images;
using WoW.Two.Sdk.Beta.Be.Storage.Core;

namespace OcharoBlog.Infrastructure.Blog;

/// <summary>Preserves validated original bytes and creates upright alpha-preserving web renditions through the SDK, in the
/// local blob folder. It serves a run with no WoW2 Files (<c>Files:BaseUrl</c> unset), and <see cref="FilesMediaStore"/>
/// reads through it the files stored before Files until the migration fills their ids.</summary>
public sealed class MediaStore(
    IBlogRepository repository, IBlobRepository blobs, IOptions<BlogOptions> options, TimeProvider time,
    IImageService images)
    : IMediaStore
{
    public Task<MediaBlobRecord> StoreAsync(byte[] bytes,string fileName,CancellationToken ct)=>StoreCoreAsync(bytes,fileName,null,ct);
    public Task<MediaBlobRecord> StoreEditedAsync(byte[] bytes,string fileName,MediaEditRecord edit,CancellationToken ct)=>StoreCoreAsync(bytes,fileName,edit,ct);

    private async Task<MediaBlobRecord> StoreCoreAsync(byte[] bytes,string fileName,MediaEditRecord? edit,CancellationToken ct)
    {
        var (info,sha)=MediaInput.Check(bytes,edit,options.Value.MaximumImageBytes);
        if(await repository.FindMediaAsync(sha,ct) is {} existing)
        {
            if(edit is not null)await repository.SaveMediaWithEditAsync(existing,edit,ct);
            return existing;
        }
        var path=$"media/{sha[..2]}/{sha}";
        var previewPath=$"previews/{sha[..2]}/{sha}.webp";
        var preview=await MediaInput.PreviewAsync(images,bytes,options.Value.PreviewSize,ct);
        await using(var input=new MemoryStream(bytes,false))await blobs.SaveAsync(path,input,info.ContentType,ct);
        await using(var rendition=new MemoryStream(preview,false))await blobs.SaveAsync(previewPath,rendition,"image/webp",ct);
        var record=new MediaBlobRecord{Sha256=sha,FileName=MediaInput.SafeName(fileName),
            ContentType=info.ContentType,ByteLength=bytes.LongLength,BlobPath=path,PreviewPath=previewPath,
            Width=info.Width,Height=info.Height,StoredAt=time.GetUtcNow()};
        if(edit is null)await repository.SaveMediaAsync(record,ct);
        else await repository.SaveMediaWithEditAsync(record,edit,ct);
        return record;
    }

    public async Task<MediaContent?> OpenAsync(string sha256,bool preview,CancellationToken ct)
    {
        if(!MediaInput.IsSha256(sha256))return null;
        var record=await repository.FindMediaAsync(sha256,ct);
        return record is null?null:await OpenLocalAsync(record,preview,ct);
    }

    public async Task<(MediaBlobRecord Record,Stream Content)?> ReadAsync(string sha256,bool preview,CancellationToken ct)=>
        await OpenAsync(sha256,preview,ct) is {Content:{} content} opened?(opened.Record,content):null;

    /// <summary>A run without WoW2 Files has no file to reference, so the caller copies the bytes.</summary>
    public Task<MediaBlobRecord?> ReferenceAsync(string sha256,string fileId,string fileName,CancellationToken ct)=>Task.FromResult<MediaBlobRecord?>(null);

    /// <summary>Opens a stored file's bytes from the local blob folder, its preview when it has one; none once they are gone.</summary>
    internal async Task<MediaContent?> OpenLocalAsync(MediaBlobRecord record,bool preview,CancellationToken ct)
    {
        if(string.IsNullOrEmpty(record.BlobPath))return null;
        var fromPreview=preview&&record.PreviewPath is not null;
        var stream=await blobs.OpenReadAsync(fromPreview?record.PreviewPath!:record.BlobPath,ct);
        return stream is null?null:new MediaContent(record,fromPreview?"image/webp":record.ContentType,null,stream);
    }

    /// <summary>Reads a stored file's bytes from the local blob folder; none once they are gone.</summary>
    internal async Task<(MediaBlobRecord Record,Stream Content)?> ReadLocalAsync(MediaBlobRecord record,bool preview,CancellationToken ct)=>
        await OpenLocalAsync(record,preview,ct) is {Content:{} content}?(record,content):null;
}

/// <summary>The checks and renditions every stored image gets, whichever store keeps it.</summary>
internal static class MediaInput
{
    /// <summary>The widest web preview: the local store writes it, and WoW2 Files serves it as a variant.</summary>
    public const int MaximumPreviewSize = 1400;

    /// <summary>Validates an image and, for an edited derivative, its receipt; returns its raster facts and SHA-256.</summary>
    public static (RasterImageInfo Info,string Sha256) Check(byte[] bytes,MediaEditRecord? edit,long maximumImageBytes)
    {
        var maximum=(int)Math.Clamp(maximumImageBytes,1,MediaEditingService.MaximumBytes);
        if(bytes.Length==0||bytes.LongLength>maximum)throw new BlogException("Choose a non-empty image up to 20 MiB.",413);
        RasterImageInfo info;
        try{info=RasterImageGuard.Validate(bytes,maximum);}
        catch(BackgroundRemovalException e){throw new BlogException(e.Message);}
        var sha=Convert.ToHexStringLower(SHA256.HashData(bytes));
        if(edit is not null&&edit.Sha256!=sha)throw new BlogException("Edited image does not match its receipt.");
        return (info,sha);
    }

    /// <summary>Tells whether a route value is a lowercase SHA-256, the only key a stored file has.</summary>
    public static bool IsSha256(string value)=>value.Length==64&&value.All(char.IsAsciiHexDigitLower);

    /// <summary>The preview width the options ask, kept within 1 and <see cref="MaximumPreviewSize"/>.</summary>
    public static int PreviewSize(BlogOptions options)=>Math.Clamp(options.PreviewSize,1,MaximumPreviewSize);

    /// <summary>The display name an upload keeps: its own file name when short and printable, else <c>image</c>.</summary>
    public static string SafeName(string fileName)
    {
        var name=Path.GetFileName(fileName.Replace('\\','/'));
        return name is {Length:>0 and <=150}&&!name.Any(char.IsControl)?name:"image";
    }

    /// <summary>Renders the upright WebP preview of an original, at most <paramref name="size"/> pixels a side.</summary>
    public static async Task<byte[]> PreviewAsync(IImageService images,byte[] bytes,int size,CancellationToken ct)
    {
        var longest=Math.Clamp(size,1,MaximumPreviewSize);
        using var source=new MemoryStream(bytes,false);
        var preview=await images.EditAsync(source,new ImageEditSpec{
            Resize=new(){Width=longest,Height=longest,Fit=ImageFit.Max},Output=new(){Format=ImageFormat.Webp,Quality=84}},ct);
        return preview.Content;
    }

    /// <summary>The file extension Files checks the type against.</summary>
    public static string Extension(string contentType)=>contentType switch
    {
        "image/png"=>"png",
        "image/jpeg"=>"jpg",
        _=>"webp",
    };
}
