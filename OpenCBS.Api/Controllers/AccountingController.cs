using Microsoft.AspNetCore.Mvc;
using OpenCBS.Api.Services;
using OpenCBS.Core.Domain.Accounting;

namespace OpenCBS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountingController : ControllerBase
{
    private readonly IBankingService _bankingService;

    public AccountingController(IBankingService bankingService)
    {
        _bankingService = bankingService;
    }

    [HttpGet("trial-balance")]
    public async Task<ActionResult<List<TrialBalanceItem>>> GetTrialBalance()
    {
        var trialBalance = await _bankingService.GetTrialBalanceAsync();
        return Ok(trialBalance);
    }

    [HttpPost("journal-entries")]
    public async Task<ActionResult<JournalEntry>> PostJournalEntry([FromBody] PostJournalEntryDto dto)
    {
        var entry = await _bankingService.PostJournalEntryAsync(dto);
        return Ok(entry);
    }
}
