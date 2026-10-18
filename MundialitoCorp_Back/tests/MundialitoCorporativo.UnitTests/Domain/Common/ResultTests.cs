using MundialitoCorporativo.Domain.Common;

namespace MundialitoCorporativo.UnitTests.Domain.Common;

/// <summary>
/// Prueba mínima del ítem 15: Result Pattern.
/// </summary>
public class ResultTests
{
    private static readonly Error SampleError = Error.Conflict("Ya existe.", "Sample.Conflict");

    [Fact]
    public void Success_ShouldBeSuccessfulWithoutError()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(Error.None, result.Error);
        Assert.Null(result.ErrorMessage);
        Assert.Null(result.ErrorCode);
    }

    [Fact]
    public void Failure_ShouldExposeErrorMessageAndErrorCode()
    {
        var result = Result.Failure(SampleError);

        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);
        Assert.Equal("Ya existe.", result.ErrorMessage);
        Assert.Equal("Sample.Conflict", result.ErrorCode);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
    }

    [Fact]
    public void Failure_WithoutErrorCode_ShouldBeAllowed()
    {
        var result = Result.Failure(Error.Validation("Dato inválido."));

        Assert.True(result.IsFailure);
        Assert.Equal("Dato inválido.", result.ErrorMessage);
        Assert.Null(result.ErrorCode);
    }

    [Fact]
    public void Failure_WithErrorNone_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() => Result.Failure(Error.None));
    }

    [Fact]
    public void GenericSuccess_ShouldExposeValue()
    {
        var result = Result.Success(42);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void GenericFailure_AccessingValue_ShouldThrow()
    {
        var result = Result.Failure<int>(SampleError);

        Assert.True(result.IsFailure);
        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void ImplicitConversionFromValue_ShouldCreateSuccess()
    {
        Result<string> result = "valor";

        Assert.True(result.IsSuccess);
        Assert.Equal("valor", result.Value);
    }

    [Fact]
    public void ImplicitConversionFromError_ShouldCreateFailure()
    {
        Result<string> genericResult = SampleError;
        Result result = SampleError;

        Assert.True(genericResult.IsFailure);
        Assert.True(result.IsFailure);
        Assert.Equal(SampleError, genericResult.Error);
    }

    [Theory]
    [InlineData(ErrorType.Validation)]
    [InlineData(ErrorType.NotFound)]
    [InlineData(ErrorType.Conflict)]
    [InlineData(ErrorType.Failure)]
    public void ErrorFactories_ShouldAssignType(ErrorType expected)
    {
        var error = expected switch
        {
            ErrorType.Validation => Error.Validation("m"),
            ErrorType.NotFound => Error.NotFound("m"),
            ErrorType.Conflict => Error.Conflict("m"),
            _ => Error.Failure("m")
        };

        Assert.Equal(expected, error.Type);
    }

    [Fact]
    public void ValidationError_ShouldGroupFieldErrors()
    {
        var error = new ValidationError([new FieldError("nombre", "Requerido"), new FieldError("abreviatura", "Requerida")]);

        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal(ValidationError.DefaultCode, error.Code);
        Assert.Equal(2, error.Errors.Count);
    }
}
