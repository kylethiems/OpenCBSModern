using Microsoft.EntityFrameworkCore;
using OpenCBS.Api.Data;
using OpenCBS.Api.Services;
using OpenCBS.Core.Domain.Accounting;
using OpenCBS.Core.Exceptions;

namespace OpenCBS.Tests;

public class GeneralLedgerTests
{
    private OpenCbsDbContext CreateInMemoryDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<OpenCbsDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        var db = new OpenCbsDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    [Fact]
    public async Task BalancedJournalEntry_SuccessfullyUpdatesBalances()
    {
        var db = CreateInMemoryDb(nameof(BalancedJournalEntry_SuccessfullyUpdatesBalances));
        var service = new BankingService(db);

        // Vault cash before
        var vault = await db.LedgerAccounts.FirstAsync(a => a.AccountNumber == "1010");
        decimal initialVaultBalance = vault.Balance;

        // Post balanced transaction: Debit 5100 Operating Expense $500, Credit 1010 Cash $500
        var dto = new PostJournalEntryDto(
            "Office supplies purchase",
            new List<JournalLineDto>
            {
                new("5100", 500.00m, 0.00m, "Office paper"),
                new("1010", 0.00m, 500.00m, "Paid from cash vault")
            });

        var entry = await service.PostJournalEntryAsync(dto);
        await db.SaveChangesAsync();

        Assert.NotNull(entry);
        Assert.True(entry.IsBalanced);
        Assert.Equal(500.00m, entry.TotalDebits);
        Assert.Equal(500.00m, entry.TotalCredits);

        var expenseAcc = await db.LedgerAccounts.FirstAsync(a => a.AccountNumber == "5100");
        Assert.Equal(500.00m, expenseAcc.Balance);

        var updatedVault = await db.LedgerAccounts.FirstAsync(a => a.AccountNumber == "1010");
        Assert.Equal(initialVaultBalance - 500.00m, updatedVault.Balance);
    }

    [Fact]
    public async Task UnbalancedJournalEntry_ThrowsUnbalancedJournalException()
    {
        var db = CreateInMemoryDb(nameof(UnbalancedJournalEntry_ThrowsUnbalancedJournalException));
        var service = new BankingService(db);

        var unbalancedDto = new PostJournalEntryDto(
            "Fraudulent unbalanced transaction",
            new List<JournalLineDto>
            {
                new("5100", 1_000.00m, 0.00m, "Debit 1000"),
                new("1010", 0.00m, 800.00m, "Credit 800 only")
            });

        await Assert.ThrowsAsync<UnbalancedJournalException>(async () =>
        {
            await service.PostJournalEntryAsync(unbalancedDto);
        });
    }

    [Fact]
    public async Task TrialBalance_ReflectsSeededAccounts()
    {
        var db = CreateInMemoryDb(nameof(TrialBalance_ReflectsSeededAccounts));
        var service = new BankingService(db);

        var trialBalance = await service.GetTrialBalanceAsync();

        Assert.NotEmpty(trialBalance);
        Assert.Contains(trialBalance, a => a.AccountNumber == "1010" && a.Category == AccountCategory.Asset);
        Assert.Contains(trialBalance, a => a.AccountNumber == "1200" && a.Category == AccountCategory.Asset);
        Assert.Contains(trialBalance, a => a.AccountNumber == "2100" && a.Category == AccountCategory.Liability);
    }
}
