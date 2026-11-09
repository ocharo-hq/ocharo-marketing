using OcharoBlog.Api.Blog.Errors;

namespace OcharoBlog.Tests.Support;

/// <summary>Reads the error a refused result carries, failing the test when the result succeeded, and the status the
/// host answers it with.</summary>
public static class Refusals
{
    /// <summary>Returns the error <paramref name="result"/> failed with.</summary>
    public static AppError Of<T>(Result<T> result) where T : notnull
    {
        return result.Should().BeOfType<Result<T>.Failure>().Subject.Error;
    }

    /// <summary>Returns the HTTP status the host answers <paramref name="error"/> with.</summary>
    public static int Status(AppError error)
    {
        return new BlogErrorHttpStatusCodeMapper().ToStatusCode(error);
    }

    /// <summary>Returns the error <paramref name="result"/> failed with.</summary>
    public static AppError Of(Result result)
    {
        return result.Should().BeOfType<Result.Failure>().Subject.Error;
    }
}
