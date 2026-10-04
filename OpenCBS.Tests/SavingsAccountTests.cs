using Microsoft.EntityFrameworkCore;
using OpenCBS.Api.Data;
using OpenCBS.Api.Services;
using OpenCBS.Core.Domain.Clients;
using OpenCBS.Core.Domain.Savings;
using OpenCBS.Core.Exceptions;

namespace OpenCBS.Tests;

public class SavingsAccountTests
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
    public async Task Deposit_IncreasesBalance_AndPostsLedgerTransaction()
    {
        var db = CreateInMemoryDb(nameof(Deposit_IncreasesBalance_AndPostsLedgerTransaction));
        var service = new BankingService(db);

        var client = await service.CreateClientAsync(new CreateClientDto(
            "ID-3003", "John", "Doe", "", ClientType.Individual, "john@example.com", "555-0404", "Market St"));
        await service.UpdateKycStatusAsync(client.Id, ClientStatus.Active);

        var account = await service.OpenSavingsAccountAsync(
            client.Id, SavingsAccountType.DemandDeposit, initialDeposit: 200.00m);

        Assert.Equal(200.00m, account.Balance);
        Assert.Single(account.Transactions);

        // Additional deposit
        var updated = await service.DepositAsync(account.Id, 300.00m, "Payroll deposit");
        Assert.Equal(500.00m, updated.Balance);
        Assert.Equal(2, updated.Transactions.Count);

        // Verify General Ledger: Customer Deposits (2100) credited by 500 total
        var depositLiab = await db.LedgerAccounts.FirstAsync(a => a.AccountNumber == "2100");
        Assert.Equal(500.00m, depositLiab.Balance);
    }

    [Fact]
    public async Task Withdrawal_WithinBalance_Succeeds()
    {
        var db = CreateInMemoryDb(nameof(Withdrawal_WithinBalance_Succeeds));
        var service = new BankingService(db);

        var client = await service.CreateClientAsync(new CreateClientDto(
            "ID-4004", "Jane", "Smith", "", ClientType.Individual, "jane@example.com", "555-0505", "Broad St"));
        await service.UpdateKycStatusAsync(client.Id, ClientStatus.Active);

        var account = await service.OpenSavingsAccountAsync(client.Id, SavingsAccountType.DemandDeposit, 1_000.00m);

        var withdrawn = await service.WithdrawAsync(account.Id, 400.00m, "ATM withdrawal");
        Assert.Equal(600.00m, withdrawn.Balance);
    }

    [Fact]
    public async Task Withdrawal_ExceedingAvailableFunds_ThrowsInsufficientFunds()
    {
        var db = CreateInMemoryDb(nameof(Withdrawal_ExceedingAvailableFunds_ThrowsInsufficientFunds));
        var service = new BankingService(db);

        var client = await service.CreateClientAsync(new CreateClientDto(
            "ID-5005", "Carlos", "Silva", "", ClientType.Individual, "carlos@example.com", "555-0606", "Avenida Paulista"));
        await service.UpdateKycStatusAsync(client.Id, ClientStatus.Active);

        // Account with 100 balance and NO overdraft
        var account = await service.OpenSavingsAccountAsync(client.Id, SavingsAccountType.DemandDeposit, 100.00m, overdraftLimit: 0m);

        await Assert.ThrowsAsync<InsufficientFundsException>(async () =>
        {
            await service.WithdrawAsync(account.Id, 150.00m);
        });

        // Account WITH 200 overdraft limit allows 150 withdrawal on 100 balance (net -50 balance)
        var overdraftAcc = await service.OpenSavingsAccountAsync(client.Id, SavingsAccountType.DemandDeposit, 100.00m, overdraftLimit: 200.00m);
        var successWithdraw = await service.WithdrawAsync(overdraftAcc.Id, 150.00m, "Authorized Overdraft");

        Assert.Equal(-50.00m, successWithdraw.Balance);
        Assert.Equal(150.00m, successWithdraw.AvailableFunds);
    }
}
