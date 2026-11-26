using ClinIQ.Application.Features.Clinics;
using ClinIQ.Application.Features.Clinics.Commands;
using ClinIQ.Application.Features.Clinics.Queries;
using ClinIQ.Domain.Clinics;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinIQ.API.Controllers;

[Authorize(Policy = "ClinicWeb")]
[Route("api/clinic-applications")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ClinicApplicationsController(ISender sender) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType<ClinicApplicationPage>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken ct, int page = 1, int pageSize = 20, ClinicApplicationStatus? status = null)
    { var r = await sender.Send(new ListClinicApplicationsQuery(false, page, pageSize, status), ct); return r.Match<IActionResult>(Ok, Problem); }
    [HttpGet("{id:guid}")]
    [ProducesResponseType<ClinicApplicationDetail>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    { var r = await sender.Send(new GetClinicApplicationQuery(id, false), ct); return r.Match<IActionResult>(Ok, Problem); }
    [HttpPost]
    [RequestSizeLimit(20 * 1024)]
    [ProducesResponseType<ClinicApplicationDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(ClinicApplicationDraftRequest request, CancellationToken ct)
    { var r = await sender.Send(new CreateClinicApplicationCommand(request.Input(), null), ct); return r.Match<IActionResult>(row => CreatedAtAction(nameof(Get), new { id = row.Id }, row), Problem); }
    [HttpPut("{id:guid}/draft")]
    [RequestSizeLimit(20 * 1024)]
    [ProducesResponseType<ClinicApplicationDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, ClinicApplicationDraftRequest request, CancellationToken ct)
    { var r = await sender.Send(new UpdateClinicApplicationCommand(id, request.Input(), request.Version), ct); return r.Match<IActionResult>(Ok, Problem); }
    [HttpPost("{id:guid}/submissions")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(17 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 17 * 1024 * 1024)]
    [ProducesResponseType<ClinicApplicationDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Submit(Guid id, [FromForm] ClinicApplicationSubmissionRequest request, CancellationToken ct)
    {
        var uploads = new List<ClinicEvidenceUpload>();
        try
        {
            Add(request.FacilityLicense, ClinicEvidenceKind.FacilityLicense); Add(request.ApplicantIdentity, ClinicEvidenceKind.ApplicantIdentity); Add(request.ManagementDelegation, ClinicEvidenceKind.ManagementDelegation);
            var r = await sender.Send(new SubmitClinicApplicationCommand(id, request.Version, uploads), ct);
            return r.Match<IActionResult>(row => CreatedAtAction(nameof(GetSubmission), new { id, submissionId = row.CurrentSubmissionId }, row), Problem);
        }
        finally { foreach (var file in uploads) await file.Content.DisposeAsync(); }
        void Add(IFormFile? file, ClinicEvidenceKind kind) { if (file is not null) uploads.Add(new(kind, file.FileName, file.ContentType, file.Length, file.OpenReadStream())); }
    }
    [HttpGet("{id:guid}/submissions")]
    [ProducesResponseType<ClinicSubmissionPage>(StatusCodes.Status200OK)]
    public async Task<IActionResult> History(Guid id, CancellationToken ct, int page = 1, int pageSize = 20)
    { var r = await sender.Send(new ListClinicSubmissionsQuery(id, false, page, pageSize), ct); return r.Match<IActionResult>(Ok, Problem); }
    [HttpGet("{id:guid}/submissions/{submissionId:guid}")]
    [ProducesResponseType<ClinicSubmissionDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSubmission(Guid id, Guid submissionId, CancellationToken ct)
    { var r = await sender.Send(new GetClinicSubmissionQuery(id, submissionId, false), ct); return r.Match<IActionResult>(Ok, Problem); }
    [HttpGet("{id:guid}/submissions/{submissionId:guid}/documents/{documentId:guid}")]
    public async Task<IActionResult> Download(Guid id, Guid submissionId, Guid documentId, CancellationToken ct)
    { var r = await sender.Send(new DownloadClinicEvidenceQuery(id, submissionId, documentId, false), ct); return r.Match<IActionResult>(file => { Response.Headers.XContentTypeOptions = "nosniff"; return File(file.Content, file.MediaType, file.FileName, enableRangeProcessing: false); }, Problem); }
}
public sealed class ClinicApplicationDraftRequest
{
    public string ClinicName { get; init; } = "";
    public string Description { get; init; } = "";
    public string BranchName { get; init; } = "";
    public string Address { get; init; } = "";
    public decimal? Latitude { get; init; }
    public decimal? Longitude { get; init; }
    public string PublicPhone { get; init; } = "";
    public string LicensingAuthority { get; init; } = "";
    public string LicenseNumber { get; init; } = "";
    public string LicensedOperator { get; init; } = "";
    public ClinicApplicantCapacity ApplicantCapacity { get; init; }
    public ManagementAuthorityBasis AuthorityBasis { get; init; }
    public string Version { get; init; } = "";
    public ClinicApplicationData Input() => new(ClinicName, Description, BranchName, Address, Latitude, Longitude, PublicPhone, LicensingAuthority, LicenseNumber, LicensedOperator, ApplicantCapacity, AuthorityBasis);
}
public sealed class ClinicApplicationSubmissionRequest
{
    public string Version { get; init; } = "";
    public IFormFile? FacilityLicense { get; init; }
    public IFormFile? ApplicantIdentity { get; init; }
    public IFormFile? ManagementDelegation { get; init; }
}
