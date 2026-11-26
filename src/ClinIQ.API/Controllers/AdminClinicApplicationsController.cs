using ClinIQ.Application.Features.Clinics;
using ClinIQ.Application.Features.Clinics.Commands;
using ClinIQ.Application.Features.Clinics.Queries;
using ClinIQ.Domain.Clinics;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinIQ.API.Controllers;

[Authorize(Roles = "Admin")]
[Route("api/admin/clinic-applications")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[RequestSizeLimit(16 * 1024)]
public sealed class AdminClinicApplicationsController(ISender sender) : ApiControllerBase
{
    [HttpGet]
    [EndpointDescription("Omitted status lists pending applications; an explicitly empty status includes all states.")]
    [ProducesResponseType<ClinicApplicationPage>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken ct, int page = 1, int pageSize = 20, ClinicApplicationStatus? status = null)
    { var filter = Request.Query.ContainsKey(nameof(status)) ? status : ClinicApplicationStatus.PendingReview; var r = await sender.Send(new ListClinicApplicationsQuery(true, page, pageSize, filter), ct); return r.Match<IActionResult>(Ok, Problem); }
    [HttpGet("{id:guid}")]
    [ProducesResponseType<ClinicApplicationDetail>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    { var r = await sender.Send(new GetClinicApplicationQuery(id, true), ct); return r.Match<IActionResult>(Ok, Problem); }
    [HttpGet("{id:guid}/submissions")]
    [ProducesResponseType<ClinicSubmissionPage>(StatusCodes.Status200OK)]
    public async Task<IActionResult> History(Guid id, CancellationToken ct, int page = 1, int pageSize = 20)
    { var r = await sender.Send(new ListClinicSubmissionsQuery(id, true, page, pageSize), ct); return r.Match<IActionResult>(Ok, Problem); }
    [HttpGet("{id:guid}/submissions/{submissionId:guid}")]
    [ProducesResponseType<ClinicSubmissionDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSubmission(Guid id, Guid submissionId, CancellationToken ct)
    { var r = await sender.Send(new GetClinicSubmissionQuery(id, submissionId, true), ct); return r.Match<IActionResult>(Ok, Problem); }
    [HttpPost("{id:guid}/submissions/{submissionId:guid}/approve")]
    [ProducesResponseType<ClinicApplicationDto>(StatusCodes.Status200OK)]
    public Task<IActionResult> Approve(Guid id, Guid submissionId, ClinicApplicationReviewRequest request, CancellationToken ct) => Review(id, submissionId, ClinicApplicationStatus.Approved, request, ct);
    [HttpPost("{id:guid}/submissions/{submissionId:guid}/reject")]
    [ProducesResponseType<ClinicApplicationDto>(StatusCodes.Status200OK)]
    public Task<IActionResult> Reject(Guid id, Guid submissionId, ClinicApplicationReviewRequest request, CancellationToken ct) => Review(id, submissionId, ClinicApplicationStatus.Rejected, request, ct);
    [HttpPost("{id:guid}/submissions/{submissionId:guid}/request-information")]
    [ProducesResponseType<ClinicApplicationDto>(StatusCodes.Status200OK)]
    public Task<IActionResult> RequestInformation(Guid id, Guid submissionId, ClinicApplicationReviewRequest request, CancellationToken ct) => Review(id, submissionId, ClinicApplicationStatus.NeedsInformation, request, ct);
    private async Task<IActionResult> Review(Guid id, Guid submissionId, ClinicApplicationStatus outcome, ClinicApplicationReviewRequest request, CancellationToken ct)
    { var r = await sender.Send(new ReviewClinicApplicationCommand(id, submissionId, outcome, request.Reason, request.Version, request.LicenseDetailsVerified, request.IdentityVerified, request.AuthorityVerified, request.IndependentlyCorroborated), ct); return r.Match<IActionResult>(Ok, Problem); }
    [HttpGet("{id:guid}/submissions/{submissionId:guid}/documents/{documentId:guid}")]
    public async Task<IActionResult> Download(Guid id, Guid submissionId, Guid documentId, CancellationToken ct)
    { var r = await sender.Send(new DownloadClinicEvidenceQuery(id, submissionId, documentId, true), ct); return r.Match<IActionResult>(file => { Response.Headers.XContentTypeOptions = "nosniff"; return File(file.Content, file.MediaType, file.FileName, enableRangeProcessing: false); }, Problem); }
}
public sealed record ClinicApplicationReviewRequest(string Reason, string Version, bool LicenseDetailsVerified = false, bool IdentityVerified = false, bool AuthorityVerified = false, bool IndependentlyCorroborated = false);
