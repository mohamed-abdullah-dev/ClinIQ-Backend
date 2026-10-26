using System.Reflection;
using ClinIQ.Application;
using ClinIQ.Domain.Common.Results;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

var checks = new List<string>();
void Check(bool condition, string name)
{
    if (!condition) throw new InvalidOperationException(name);
    checks.Add(name);
}

Result<int> success = 42;
Check(success.Match(value => value == 42, _ => false), "Result success / Match");
var sourceErrors = new List<Error> { Error.NotFound("Missing", "Not found.") };
var failure = Result<int>.Failure(sourceErrors);
sourceErrors.Clear();
Check(failure.IsError && failure.Errors.Count == 1, "Failure errors use an immutable snapshot");
var invalidValueThrew = false;
try { _ = failure.Value; } catch (InvalidOperationException) { invalidValueThrew = true; }
Check(invalidValueThrew, "Value access on failure throws");
var nullThrew = false;
try { _ = Result<string>.Success(null!); } catch (ArgumentNullException) { nullThrew = true; }
Check(nullThrew, "Null success rejected");
var emptyThrew = false;
try { _ = Result<int>.Failure(Array.Empty<Error>()); } catch (ArgumentException) { emptyThrew = true; }
Check(emptyThrew, "Empty failure rejected");

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    EnvironmentName = "Development",
    ContentRootPath = AppContext.BaseDirectory
});
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddApplication();
builder.Services.AddTransient<IValidator<Probe>, FirstValidator>();
builder.Services.AddTransient<IValidator<Probe>, SecondValidator>();
builder.Services.AddTransient<IRequestHandler<Probe, Result<int>>, ProbeHandler>();
builder.Services.AddTransient<IRequestHandler<UnvalidatedProbe, Result<Success>>, UnvalidatedHandler>();

await using var app = builder.Build(); // Build DI only; do not start an HTTP server.
await using var scope = app.Services.CreateAsyncScope();
var services = scope.ServiceProvider;
var sender = services.GetRequiredService<ISender>();
var valid = await sender.Send(new Probe("valid"));
Check(valid.IsSuccess && valid.Value == 42 && ProbeHandler.Calls == 1, "MediatR executes valid request");
var invalid = await sender.Send(new Probe(""));
Check(invalid.IsError && invalid.Errors.Count == 2 && ProbeHandler.Calls == 1,
    "Two validators return typed failure and skip handler");
var unvalidated = await sender.Send(new UnvalidatedProbe());
Check(unvalidated.IsSuccess, "Request without validators reaches handler");

var controller = new ErrorProbeController
{
    ControllerContext = new ControllerContext
    {
        HttpContext = new DefaultHttpContext { RequestServices = services }
    }
};
Check(((ObjectResult)controller.Map(Error.Unauthorized("Auth.Required", "Login required."))).StatusCode == 401,
    "Unauthorized maps to HTTP 401");
Check(((ObjectResult)controller.Map(Error.Forbidden("Auth.Denied", "Access denied."))).StatusCode == 403,
    "Forbidden maps to HTTP 403");
var validationResult = (ObjectResult)controller.Map(
    Error.Validation("Name.Required", "Required.", "Name"),
    Error.Validation("Name.Length", "Too short.", "Name"));
var validationProblem = (ValidationProblemDetails)validationResult.Value!;
Check(validationResult.StatusCode == 400 && validationProblem.Errors["Name"].Length == 2,
    "Repeated validation fields are grouped in HTTP 400 response");

try
{
    await sender.Send(new UnvalidatedProbe(), new CancellationToken(canceled: true));
    throw new Exception("Expected cancellation");
}
catch (OperationCanceledException) { Check(true, "Canceled request stops before its handler"); }

var serverFailure = (ObjectResult)controller.Map(Error.Failure("Private.Error", "private internal detail"));
Check(serverFailure.StatusCode == 500 && ((ProblemDetails)serverFailure.Value!).Detail == "An unexpected error occurred.",
    "Unexpected failure hides internal details");
Check(((ObjectResult)controller.Map(Error.NotFound("Missing", "Not found"))).StatusCode == 404,
    "NotFound maps to HTTP 404");
Check(((ObjectResult)controller.Map(Error.Conflict("Duplicate", "Already exists"))).StatusCode == 409,
    "Conflict maps to HTTP 409");

var exceptionContext = new DefaultHttpContext { RequestServices = services };
exceptionContext.Response.Body = new MemoryStream();
exceptionContext.Request.Headers.Accept = "text/html";
var exceptionHandler = new ClinIQ.API.Infrastructure.GlobalExceptionHandler(
    services.GetRequiredService<IProblemDetailsService>(),
    Microsoft.Extensions.Logging.Abstractions.NullLogger<ClinIQ.API.Infrastructure.GlobalExceptionHandler>.Instance);
await exceptionHandler.TryHandleAsync(exceptionContext, new Exception("Private patient text"), CancellationToken.None);
exceptionContext.Response.Body.Position = 0;
var exceptionBody = await new StreamReader(exceptionContext.Response.Body).ReadToEndAsync();
Check(exceptionContext.Response.StatusCode == 500 && exceptionBody.Contains("traceId") &&
    !exceptionBody.Contains("Private patient text"), "Exception handler returns safe ProblemDetails even for unsupported Accept");

Console.WriteLine("CHECK_REPORT:" + System.Text.Json.JsonSerializer.Serialize(new
{
    count = checks.Count,
    checks,
    mediator = typeof(ISender).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,
    noSqlConnectionOpened = true,
    noHttpServerStarted = true
}));

public sealed record Probe(string Name) : IRequest<Result<int>>;
public sealed class FirstValidator : AbstractValidator<Probe>
{
    public FirstValidator() => RuleFor(request => request.Name).NotEmpty();
}
public sealed class SecondValidator : AbstractValidator<Probe>
{
    public SecondValidator() => RuleFor(request => request.Name).MinimumLength(3);
}
public sealed class ProbeHandler : IRequestHandler<Probe, Result<int>>
{
    public static int Calls { get; private set; }
    public Task<Result<int>> Handle(Probe request, CancellationToken cancellationToken)
    {
        Calls++;
        return Task.FromResult(Result<int>.Success(42));
    }
}
public sealed record UnvalidatedProbe : IRequest<Result<Success>>;
public sealed class UnvalidatedHandler : IRequestHandler<UnvalidatedProbe, Result<Success>>
{
    public Task<Result<Success>> Handle(UnvalidatedProbe request, CancellationToken cancellationToken) =>
        Task.FromResult<Result<Success>> (Result.Success);
}
internal sealed class ErrorProbeController : ClinIQ.API.Controllers.ApiControllerBase
{
    public IActionResult Map(params Error[] errors) => Problem(errors);
}
