using ClinIQ.Application.Features.Clinics;
using ClinIQ.Application.Features.Clinics.Commands;
using ClinIQ.Application.Features.Clinics.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinIQ.API.Controllers;

[Authorize(Policy = "ClinicWeb", Roles = "ClinicManager")]
[Route("api/managed-clinics")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ManagedClinicsController(ISender sender) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType<ManagedClinicPage>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken ct, int page = 1, int pageSize = 20)
    { var r = await sender.Send(new ListManagedClinicsQuery(page, pageSize), ct); return r.Match<IActionResult>(Ok, Problem); }
    [HttpGet("{id:guid}")]
    [ProducesResponseType<ManagedClinicDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    { var r = await sender.Send(new GetManagedClinicQuery(id), ct); return r.Match<IActionResult>(Ok, Problem); }
    [HttpGet("{id:guid}/branches")]
    [ProducesResponseType<ManagedBranchPage>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Branches(Guid id, CancellationToken ct, int page = 1, int pageSize = 20)
    { var r = await sender.Send(new ListManagedBranchesQuery(id, page, pageSize), ct); return r.Match<IActionResult>(Ok, Problem); }
    [HttpGet("{id:guid}/branches/{branchId:guid}")]
    [ProducesResponseType<ManagedBranchDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Branch(Guid id, Guid branchId, CancellationToken ct)
    { var r = await sender.Send(new GetManagedBranchQuery(id, branchId), ct); return r.Match<IActionResult>(Ok, Problem); }
    [HttpPost("{id:guid}/branch-applications")]
    [RequestSizeLimit(20 * 1024)]
    [ProducesResponseType<ClinicApplicationDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> NewBranch(Guid id, ClinicApplicationDraftRequest request, CancellationToken ct)
    {
        var data = request.Input() with { ClinicName = "Existing clinic", Description = "" };
        var r = await sender.Send(new CreateClinicApplicationCommand(data, id), ct);
        return r.Match<IActionResult>(row => CreatedAtAction("Get", "ClinicApplications", new { id = row.Id }, row), Problem);
    }
}
