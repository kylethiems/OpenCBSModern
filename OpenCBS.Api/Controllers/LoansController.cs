using Microsoft.AspNetCore.Mvc;
using OpenCBS.Api.Services;
using OpenCBS.Core.Domain.Loans;

namespace OpenCBS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LoansController : ControllerBase
{
    private readonly IBankingService _bankingService;

    public LoansController(IBankingService bankingService)
    {
        _bankingService = bankingService;
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<LoanContract>> GetById(int id)
    {
        var loan = await _bankingService.GetLoanByIdAsync(id);
        if (loan == null) return NotFound(new { error = $"Loan with ID {id} not found." });
        return Ok(loan);
    }

    [HttpGet("client/{clientId:int}")]
    public async Task<ActionResult<List<LoanContract>>> GetByClient(int clientId)
    {
        var loans = await _bankingService.GetLoansByClientAsync(clientId);
        return Ok(loans);
    }

    [HttpPost("apply")]
    public async Task<ActionResult<LoanContract>> Apply([FromBody] ApplyLoanDto dto)
    {
        var loan = await _bankingService.ApplyForLoanAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = loan.Id }, loan);
    }

    [HttpPost("{id:int}/approve")]
    public async Task<ActionResult<LoanContract>> Approve(int id)
    {
        var loan = await _bankingService.ApproveLoanAsync(id);
        return Ok(loan);
    }

    [HttpPost("{id:int}/disburse")]
    public async Task<ActionResult<LoanContract>> Disburse(int id)
    {
        var loan = await _bankingService.DisburseLoanAsync(id);
        return Ok(loan);
    }

    [HttpPost("{id:int}/repay")]
    public async Task<ActionResult<RepaymentResult>> Repay(int id, [FromQuery] decimal amount)
    {
        var result = await _bankingService.ProcessRepaymentAsync(id, amount);
        return Ok(result);
    }
}
