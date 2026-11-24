using ClinIQ.Application.Features.PracticeLocations;
using ClinIQ.Application.Features.PracticeLocations.Commands;
using ClinIQ.Application.Features.PracticeLocations.Queries;
using ClinIQ.Domain.PracticeLocations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinIQ.API.Controllers;

[Authorize(Roles = "Admin")]
[Route("api/admin/practice-locations")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[RequestSizeLimit(16 * 1024)]
public sealed class AdminPracticeLocationsController(ISender sender) : ApiControllerBase
{
    [HttpGet]
    [EndpointDescription("Omitted status lists pending locations; an explicitly empty status includes all states.")]
    [ProducesResponseType<PracticeLocationPage>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken ct, int page = 1, int pageSize = 20, PracticeLocationStatus? status = null)
    {
        var filter = Request.Query.ContainsKey(nameof(status)) ? status : PracticeLocationStatus.PendingReview;
        var result = await sender.Send(new ListPracticeLocationsQuery(true, page, pageSize, filter), ct); return result.Match<IActionResult>(Ok, Problem);
    }
    [HttpGet("{id:guid}")]
    [ProducesResponseType<PracticeLocationDetail>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    { var result = await sender.Send(new GetPracticeLocationQuery(id, true), ct); return result.Match<IActionResult>(Ok, Problem); }
    [HttpGet("{id:guid}/submissions")]
    [ProducesResponseType<PracticeLocationSubmissionPage>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListSubmissions(Guid id, CancellationToken ct, int page = 1, int pageSize = 20)
    { var result = await sender.Send(new ListPracticeLocationSubmissionsQuery(id, true, page, pageSize), ct); return result.Match<IActionResult>(Ok, Problem); }
    [HttpGet("{id:guid}/submissions/{submissionId:guid}")]
    [ProducesResponseType<PracticeLocationSubmissionDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSubmission(Guid id, Guid submissionId, CancellationToken ct)
    { var result = await sender.Send(new GetPracticeLocationSubmissionQuery(id, submissionId, true), ct); return result.Match<IActionResult>(Ok, Problem); }
    [HttpPost("{id:guid}/submissions/{submissionId:guid}/approve")]
    [ProducesResponseType<PracticeLocationDto>(StatusCodes.Status200OK)]
    public Task<IActionResult> Approve(Guid id, Guid submissionId, PracticeLocationReviewRequest request, CancellationToken ct) => Review(id, submissionId, PracticeLocationStatus.Approved, request, ct);
    [HttpPost("{id:guid}/submissions/{submissionId:guid}/reject")]
    [ProducesResponseType<PracticeLocationDto>(StatusCodes.Status200OK)]
    public Task<IActionResult> Reject(Guid id, Guid submissionId, PracticeLocationReviewRequest request, CancellationToken ct) => Review(id, submissionId, PracticeLocationStatus.Rejected, request, ct);
    [HttpPost("{id:guid}/submissions/{submissionId:guid}/request-information")]
    [ProducesResponseType<PracticeLocationDto>(StatusCodes.Status200OK)]
    public Task<IActionResult> RequestInformation(Guid id, Guid submissionId, PracticeLocationReviewRequest request, CancellationToken ct) => Review(id, submissionId, PracticeLocationStatus.NeedsInformation, request, ct);
    private async Task<IActionResult> Review(Guid id, Guid submissionId, PracticeLocationStatus outcome, PracticeLocationReviewRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new ReviewPracticeLocationCommand(id, submissionId, outcome, request.Reason, request.Version,
            request.LicenseMatchesAddress, request.DoctorWorkLinkVerified), ct); return result.Match<IActionResult>(Ok, Problem);
    }
    [HttpGet("{id:guid}/submissions/{submissionId:guid}/documents/{documentId:guid}")]
    public async Task<IActionResult> Download(Guid id, Guid submissionId, Guid documentId, CancellationToken ct)
    {
        var result = await sender.Send(new DownloadPracticeLocationEvidenceQuery(id, submissionId, documentId, true), ct);
        return result.Match<IActionResult>(file => { Response.Headers.XContentTypeOptions = "nosniff"; return File(file.Content, file.MediaType, file.FileName, enableRangeProcessing: false); }, Problem);
    }
}
public sealed record PracticeLocationReviewRequest(string Reason, string Version, bool LicenseMatchesAddress = false, bool DoctorWorkLinkVerified = false);
