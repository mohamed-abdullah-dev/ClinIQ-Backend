using ClinIQ.Application.Features.Clinics;
using ClinIQ.Application.Features.Clinics.Commands;
using ClinIQ.Domain.Clinics;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClinIQ.API.Controllers;

[Route("api/auth/clinic-applicants")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[EnableRateLimiting("auth-registration")]
public sealed class ClinicApplicantsController(ISender sender) : ApiControllerBase
{
    [AllowAnonymous, HttpPost("register")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(17 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 17 * 1024 * 1024)]
    [ProducesResponseType<ClinicApplicantReceipt>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Register([FromForm] ClinicApplicantRegistrationRequest request, CancellationToken ct)
    {
        var documents = new List<ClinicEvidenceUpload>();
        try
        {
            Add(request.FacilityLicense, ClinicEvidenceKind.FacilityLicense); Add(request.ApplicantIdentity, ClinicEvidenceKind.ApplicantIdentity); Add(request.ManagementDelegation, ClinicEvidenceKind.ManagementDelegation);
            var result = await sender.Send(new RegisterClinicApplicantCommand(request.FullName.Trim(), request.Email.Trim(), request.Password, request.Details(), documents), ct);
            return result.Match<IActionResult>(receipt => StatusCode(StatusCodes.Status201Created, receipt), Problem);
        }
        finally { foreach (var document in documents) await document.Content.DisposeAsync(); }
        void Add(IFormFile? file, ClinicEvidenceKind kind)
        { if (file is not null) documents.Add(new ClinicEvidenceUpload(kind, file.FileName, file.ContentType, file.Length, file.OpenReadStream())); }
    }
}
public sealed class ClinicApplicantRegistrationRequest
{
    public string FullName { get; init; } = "";
    public string Email { get; init; } = "";
    public string Password { get; init; } = "";
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
    public IFormFile? FacilityLicense { get; init; }
    public IFormFile? ApplicantIdentity { get; init; }
    public IFormFile? ManagementDelegation { get; init; }
    public ClinicApplicationData Details() => new(ClinicName, Description, BranchName, Address, Latitude, Longitude, PublicPhone, LicensingAuthority, LicenseNumber, LicensedOperator, ApplicantCapacity, AuthorityBasis);
}
