using System.ComponentModel.DataAnnotations;
using ClinIQ.Application.Features.PracticeLocations;
using ClinIQ.Application.Features.PracticeLocations.Commands;
using ClinIQ.Application.Features.PracticeLocations.Queries;
using ClinIQ.Domain.PracticeLocations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinIQ.API.Controllers;

[Authorize(Roles = "Doctor")]
[Route("api/doctors/me/practice-locations")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class DoctorPracticeLocationsController(ISender sender) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType<PracticeLocationPage>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken ct, int page = 1, int pageSize = 20, PracticeLocationStatus? status = null)
    {
        var result = await sender.Send(new ListPracticeLocationsQuery(false, page, pageSize, status), ct); return result.Match<IActionResult>(Ok, Problem);
    }
    [HttpGet("{id:guid}")]
    [ProducesResponseType<PracticeLocationDetail>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    { var result = await sender.Send(new GetPracticeLocationQuery(id, false), ct); return result.Match<IActionResult>(Ok, Problem); }
    [HttpPost]
    [RequestSizeLimit(16 * 1024)]
    [ProducesResponseType<PracticeLocationDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(PracticeLocationDraftRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new CreatePracticeLocationCommand(request.Input()), ct);
        return result.Match<IActionResult>(row => CreatedAtAction(nameof(Get), new { id = row.Id }, row), Problem);
    }
    [HttpPut("{id:guid}/draft")]
    [RequestSizeLimit(16 * 1024)]
    [ProducesResponseType<PracticeLocationDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateDraft(Guid id, PracticeLocationDraftRequest request, CancellationToken ct)
    { var result = await sender.Send(new UpdatePracticeLocationDraftCommand(id, request.Input(), request.Version), ct); return result.Match<IActionResult>(Ok, Problem); }
    [HttpPut("{id:guid}/fee")]
    [RequestSizeLimit(16 * 1024)]
    [ProducesResponseType<PracticeLocationDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ChangeFee(Guid id, PracticeLocationFeeRequest request, CancellationToken ct)
    { var result = await sender.Send(new ChangePracticeLocationFeeCommand(id, request.ConsultationFee!.Value, request.Currency, request.Version), ct); return result.Match<IActionResult>(Ok, Problem); }
    [HttpPost("{id:guid}/submissions")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(12 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 12 * 1024 * 1024)]
    [ProducesResponseType<PracticeLocationDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Submit(Guid id, [FromForm] PracticeLocationSubmissionRequest request, CancellationToken ct)
    {
        var documents = new List<PracticeLocationUpload>();
        try
        {
            Add(request.FacilityLicense, PracticeLocationEvidenceKind.FacilityLicense);
            Add(request.WorkAuthorization, PracticeLocationEvidenceKind.WorkAuthorization);
            var result = await sender.Send(new SubmitPracticeLocationCommand(id, request.Version, documents), ct);
            return result.Match<IActionResult>(row => CreatedAtAction(nameof(GetSubmission), new { id, submissionId = row.CurrentSubmissionId }, row), Problem);
        }
        finally { foreach (var document in documents) await document.Content.DisposeAsync(); }
        void Add(IFormFile? file, PracticeLocationEvidenceKind kind)
        { if (file is not null) documents.Add(new PracticeLocationUpload(kind, file.FileName, file.ContentType, file.Length, file.OpenReadStream())); }
    }
    [HttpGet("{id:guid}/submissions")]
    [ProducesResponseType<PracticeLocationSubmissionPage>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListSubmissions(Guid id, CancellationToken ct, int page = 1, int pageSize = 20)
    { var result = await sender.Send(new ListPracticeLocationSubmissionsQuery(id, false, page, pageSize), ct); return result.Match<IActionResult>(Ok, Problem); }
    [HttpGet("{id:guid}/submissions/{submissionId:guid}")]
    [ProducesResponseType<PracticeLocationSubmissionDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSubmission(Guid id, Guid submissionId, CancellationToken ct)
    { var result = await sender.Send(new GetPracticeLocationSubmissionQuery(id, submissionId, false), ct); return result.Match<IActionResult>(Ok, Problem); }
    [HttpGet("{id:guid}/submissions/{submissionId:guid}/documents/{documentId:guid}")]
    public async Task<IActionResult> Download(Guid id, Guid submissionId, Guid documentId, CancellationToken ct)
    {
        var result = await sender.Send(new DownloadPracticeLocationEvidenceQuery(id, submissionId, documentId, false), ct);
        return result.Match<IActionResult>(file => { Response.Headers.XContentTypeOptions = "nosniff"; return File(file.Content, file.MediaType, file.FileName, enableRangeProcessing: false); }, Problem);
    }
}
public sealed class PracticeLocationDraftRequest
{
    public string Name { get; init; } = "";
    public string Address { get; init; } = "";
    public decimal? Latitude { get; init; }
    public decimal? Longitude { get; init; }
    [Required] public decimal? ConsultationFee { get; init; }
    public string Currency { get; init; } = "EGP";
    public string Version { get; init; } = "";
    public PracticeLocationDraft Input() => new(Name.Trim(), Address.Trim(), Latitude, Longitude, ConsultationFee!.Value, Currency);
}
public sealed class PracticeLocationFeeRequest
{
    [Required] public decimal? ConsultationFee { get; init; }
    public string Currency { get; init; } = "EGP";
    public string Version { get; init; } = "";
}
public sealed class PracticeLocationSubmissionRequest
{
    public string Version { get; init; } = "";
    public IFormFile? FacilityLicense { get; init; }
    public IFormFile? WorkAuthorization { get; init; }
}
