using Microsoft.EntityFrameworkCore;
using OpenCBS.Core.Domain.Accounting;
using OpenCBS.Core.Domain.Clients;
using OpenCBS.Core.Domain.Loans;
using OpenCBS.Core.Domain.Savings;

namespace OpenCBS.Api.Data;

public class OpenCbsDbContext : DbContext
{
    public OpenCbsDbContext(DbContextOptions<OpenCbsDbContext> options) : base(options) { }

    public DbSet<Client> Clients => Set<Client>();
    public DbSet<LoanContract> LoanContracts => Set<LoanContract>();
    public DbSet<LoanInstallment> LoanInstallments => Set<LoanInstallment>();
    public DbSet<SavingsAccount> SavingsAccounts => Set<SavingsAccount>();
    public DbSet<SavingsTransaction> SavingsTransactions => Set<SavingsTransaction>();
    public DbSet<LedgerAccount> LedgerAccounts => Set<LedgerAccount>();
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
    public DbSet<JournalEntryLine> JournalEntryLines => Set<JournalEntryLine>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure precision
        modelBuilder.Entity<LoanContract>()
            .Property(p => p.PrincipalAmount).HasPrecision(18, 2);
        modelBuilder.Entity<LoanContract>()
            .Property(p => p.AnnualInterestRate).HasPrecision(8, 4);

        modelBuilder.Entity<LoanInstallment>()
            .Property(p => p.PrincipalDue).HasPrecision(18, 2);
        modelBuilder.Entity<LoanInstallment>()
            .Property(p => p.InterestDue).HasPrecision(18, 2);
        modelBuilder.Entity<LoanInstallment>()
            .Property(p => p.TotalDue).HasPrecision(18, 2);
        modelBuilder.Entity<LoanInstallment>()
            .Property(p => p.PaidPrincipal).HasPrecision(18, 2);
        modelBuilder.Entity<LoanInstallment>()
            .Property(p => p.PaidInterest).HasPrecision(18, 2);
        modelBuilder.Entity<LoanInstallment>()
            .Property(p => p.PaidTotal).HasPrecision(18, 2);

        modelBuilder.Entity<SavingsAccount>()
            .Property(p => p.Balance).HasPrecision(18, 2);
        modelBuilder.Entity<SavingsAccount>()
            .Property(p => p.AnnualInterestRate).HasPrecision(8, 4);
        modelBuilder.Entity<SavingsAccount>()
            .Property(p => p.OverdraftLimit).HasPrecision(18, 2);

        modelBuilder.Entity<SavingsTransaction>()
            .Property(p => p.Amount).HasPrecision(18, 2);
        modelBuilder.Entity<SavingsTransaction>()
            .Property(p => p.BalanceAfter).HasPrecision(18, 2);

        modelBuilder.Entity<LedgerAccount>()
            .Property(p => p.Balance).HasPrecision(18, 2);

        modelBuilder.Entity<JournalEntryLine>()
            .Property(p => p.DebitAmount).HasPrecision(18, 2);
        modelBuilder.Entity<JournalEntryLine>()
            .Property(p => p.CreditAmount).HasPrecision(18, 2);

        // Seed Standard Chart of Accounts for Financial Institutions
        modelBuilder.Entity<LedgerAccount>().HasData(
            new LedgerAccount { Id = 1, AccountNumber = "1010", Name = "Cash and Vault", Category = AccountCategory.Asset, Balance = 1_000_000.00m, Description = "Physical cash and bank reserve accounts" },
            new LedgerAccount { Id = 2, AccountNumber = "1200", Name = "Loans Portfolio Receivable", Category = AccountCategory.Asset, Balance = 0.00m, Description = "Active client microfinance loans" },
            new LedgerAccount { Id = 3, AccountNumber = "2100", Name = "Client Savings Deposits", Category = AccountCategory.Liability, Balance = 0.00m, Description = "Customer demand and term deposit balances" },
            new LedgerAccount { Id = 4, AccountNumber = "3000", Name = "Institutional Equity & Reserves", Category = AccountCategory.Equity, Balance = 1_000_000.00m, Description = "Initial capital reserve" },
            new LedgerAccount { Id = 5, AccountNumber = "4100", Name = "Interest Income from Loans", Category = AccountCategory.Revenue, Balance = 0.00m, Description = "Earned interest from loan portfolios" },
            new LedgerAccount { Id = 6, AccountNumber = "5100", Name = "Operating & Administration Expenses", Category = AccountCategory.Expense, Balance = 0.00m, Description = "MFI branch operations and software maintenance" }
        );
    }
}
