using Microsoft.AspNetCore.Mvc;
using OpenCBS.Api.Services;
using OpenCBS.Core.Domain.Savings;

namespace OpenCBS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SavingsController : ControllerBase
{
    private readonly IBankingService _bankingService;

    public SavingsController(IBankingService bankingService)
    {
        _bankingService = bankingService;
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SavingsAccount>> GetById(int id)
    {
        var account = await _bankingService.GetSavingsAccountByIdAsync(id);
        if (account == null) return NotFound(new { error = $"Savings account with ID {id} not found." });
        return Ok(account);
    }

    [HttpGet("client/{clientId:int}")]
    public async Task<ActionResult<List<SavingsAccount>>> GetByClient(int clientId)
    {
        var accounts = await _bankingService.GetSavingsAccountsByClientAsync(clientId);
        return Ok(accounts);
    }

    [HttpPost("open")]
    public async Task<ActionResult<SavingsAccount>> Open(
        [FromQuery] int clientId,
        [FromQuery] SavingsAccountType type = SavingsAccountType.DemandDeposit,
        [FromQuery] decimal initialDeposit = 0m,
        [FromQuery] decimal overdraftLimit = 0m)
    {
        var account = await _bankingService.OpenSavingsAccountAsync(clientId, type, initialDeposit, overdraftLimit);
        return CreatedAtAction(nameof(GetById), new { id = account.Id }, account);
    }

    [HttpPost("{id:int}/deposit")]
    public async Task<ActionResult<SavingsAccount>> Deposit(int id, [FromQuery] decimal amount, [FromQuery] string description = "Cash Deposit")
    {
        var account = await _bankingService.DepositAsync(id, amount, description);
        return Ok(account);
    }

    [HttpPost("{id:int}/withdraw")]
    public async Task<ActionResult<SavingsAccount>> Withdraw(int id, [FromQuery] decimal amount, [FromQuery] string description = "Cash Withdrawal")
    {
        var account = await _bankingService.WithdrawAsync(id, amount, description);
        return Ok(account);
    }
}
