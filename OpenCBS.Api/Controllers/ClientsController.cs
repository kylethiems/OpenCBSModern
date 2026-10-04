using Microsoft.AspNetCore.Mvc;
using OpenCBS.Api.Services;
using OpenCBS.Core.Domain.Clients;

namespace OpenCBS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClientsController : ControllerBase
{
    private readonly IBankingService _bankingService;

    public ClientsController(IBankingService bankingService)
    {
        _bankingService = bankingService;
    }

    [HttpGet]
    public async Task<ActionResult<List<Client>>> GetAll()
    {
        var clients = await _bankingService.GetAllClientsAsync();
        return Ok(clients);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Client>> GetById(int id)
    {
        var client = await _bankingService.GetClientByIdAsync(id);
        if (client == null) return NotFound(new { error = $"Client with ID {id} not found." });
        return Ok(client);
    }

    [HttpPost]
    public async Task<ActionResult<Client>> Create([FromBody] CreateClientDto dto)
    {
        var created = await _bankingService.CreateClientAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}/kyc")]
    public async Task<ActionResult<Client>> UpdateKyc(int id, [FromBody] ClientStatus status)
    {
        var updated = await _bankingService.UpdateKycStatusAsync(id, status);
        return Ok(updated);
    }
}
