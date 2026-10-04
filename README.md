# OpenCBS Modern — Cloud-Native Core Banking Platform

[![.NET 8 CI Pipeline](https://github.com/kylethiems/OpenCBSModern/actions/workflows/ci.yml/badge.svg)](https://github.com/kylethiems/OpenCBSModern/actions/workflows/ci.yml)
[![.NET Core](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![xUnit](https://img.shields.io/badge/tests-14%20passed-brightgreen.svg)](https://github.com/kylethiems/OpenCBSModern)
[![Architecture](https://img.shields.io/badge/Architecture-Clean%20%2F%20Cloud--Native-success.svg)](https://github.com/kylethiems/OpenCBSModern)

**OpenCBS Modern** is an automated, cloud-native modernization of the legacy **[OpenCBS Desktop](https://github.com/OpenCBS/OpenCBS-Desktop)** core banking system (originally forked from Octopus Microfinance Suite). 

It transitions a legacy Windows Forms / .NET Framework 3.5 desktop monolith into a high-throughput, cross-platform **ASP.NET Core 8 WebAPI & Entity Framework Core 8** core banking platform designed for microfinance institutions (MFIs), credit unions, and community financial institutions worldwide.

---

## 🏛️ Domain Architecture & Modern Capabilities

```
                  ┌─────────────────────────────────────────┐
                  │         OpenCBS WebAPI Gateway          │
                  │   ASP.NET Core 8 / Swagger / OpenAPI    │
                  └──────┬────────────┬─────────────┬───────┘
                         │            │             │
        ┌────────────────▼──┐  ┌──────▼──────┐  ┌───▼─────────────────┐
        │ Clients & KYC     │  │ Loans Core  │  │ Savings & Deposits  │
        │ - Individual/Corp │  │ - Annuity   │  │ - Demand / Term     │
        │ - Lifecycle State │  │ - Declining │  │ - Overdraft Engine  │
        │ - Compliance Audit│  │ - Flat      │  │ - Tx Ledger History │
        └───────────────────┘  └──────┬──────┘  └───┬─────────────────┘
                                      │             │
                               ┌──────▼─────────────▼──────┐
                               │   General Ledger (GL)     │
                               │ - Double-Entry Bookkeeping│
                               │ - Invariant: Debit == Cred│
                               │ - Real-Time Trial Balance │
                               └───────────────────────────┘
```

### 1. Loan Amortization & Repayment Engine (`OpenCBS.Core.Engine`)
* **Mathematical Precision:** Supports Annuity (PMT calculation), Declining Balance (constant principal), and Flat interest models with exact penny-rounding absorption on final maturities.
* **Grace Periods:** Configurable interest-only grace period durations with automatic schedule recalculation.
* **Repayment Waterfall:** Allocates repayments strictly to accrued interest obligations first before amortizing principal balances.

### 2. Double-Entry General Ledger (`OpenCBS.Core.Domain.Accounting`)
* **Strict Accounting Invariant:** Enforces $\sum \text{Debits} \equiv \sum \text{Credits}$ at the transaction domain boundary before any ledger mutations occur.
* **Standard Financial Chart of Accounts:** Preconfigured asset, liability, equity, revenue, and expense accounts:
  * `1010` — Cash and Vault (Asset)
  * `1200` — Loans Portfolio Receivable (Asset)
  * `2100` — Client Savings Deposits (Liability)
  * `3000` — Institutional Equity & Capital Reserves (Equity)
  * `4100` — Interest Income from Loans (Revenue)
  * `5100` — Operating & Administration Expenses (Expense)

### 3. Client KYC & Lifecycle Management (`OpenCBS.Core.Domain.Clients`)
* **KYC Gating:** Loan origination and account creation require verified `Active` KYC status, preventing non-compliant credit exposure.
* **Client Types:** Full entity support for both Individual borrowers and Corporate microenterprises.

### 4. Savings & Overdraft Engine (`OpenCBS.Core.Domain.Savings`)
* **Deposit Accounts:** Demand deposits and term savings accounts with continuous balance auditing.
* **Overdraft Controls:** Configurable overdraft limits with instant validation preventing uncollateralized deficits.

---

## 🚀 Getting Started

### Prerequisites
* [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
* Git

### Build & Run
```bash
# Clone the repository
git clone https://github.com/kylethiems/OpenCBSModern.git
cd OpenCBSModern

# Restore dependencies
dotnet restore

# Build solution in Release mode
dotnet build --configuration Release --no-restore

# Run automated test suite
dotnet test --configuration Release --verbosity normal

# Launch WebAPI
dotnet run --project OpenCBS.Api
```

Interactive Swagger documentation is available locally at: `http://localhost:5000/swagger`

---

## 🧪 Automated Test Verification

The modernized solution includes comprehensive xUnit test coverage across core banking domains:

```
Starting test execution, please wait...
A total of 1 test files matched the specified pattern.
[xUnit.net 00:00:00.16]   Starting:    OpenCBS.Tests
  Passed OpenCBS.Tests.ScheduleBuilderTests.FlatSchedule_EqualInterestDistribution
  Passed OpenCBS.Tests.ScheduleBuilderTests.AnnuitySchedule_PrincipalSum_MatchesOriginalPrincipalExactly
  Passed OpenCBS.Tests.ScheduleBuilderTests.DecliningBalance_ConstantPrincipal_AndDecreasingInterest
  Passed OpenCBS.Tests.ScheduleBuilderTests.AnnuitySchedule_WithGracePeriod_HasZeroPrincipalDuringGrace
  Passed OpenCBS.Tests.ScheduleBuilderTests.InvalidArguments_ThrowArgumentException
  Passed OpenCBS.Tests.GeneralLedgerTests.TrialBalance_ReflectsSeededAccounts
  Passed OpenCBS.Tests.LoanLifecycleTests.LoanApplication_RequiresActiveKycStatus
  Passed OpenCBS.Tests.GeneralLedgerTests.UnbalancedJournalEntry_ThrowsUnbalancedJournalException
  Passed OpenCBS.Tests.GeneralLedgerTests.BalancedJournalEntry_SuccessfullyUpdatesBalances
  Passed OpenCBS.Tests.SavingsAccountTests.Withdrawal_ExceedingAvailableFunds_ThrowsInsufficientFunds
  Passed OpenCBS.Tests.SavingsAccountTests.Deposit_IncreasesBalance_AndPostsLedgerTransaction
  Passed OpenCBS.Tests.SavingsAccountTests.Withdrawal_WithinBalance_Succeeds
  Passed OpenCBS.Tests.LoanLifecycleTests.Repayment_AllocatesToInterestThenPrincipal_AndClosesLoanWhenFullyPaid
  Passed OpenCBS.Tests.LoanLifecycleTests.Disbursement_UpdatesStatusAndPostsLedgerEntry

Test Run Successful.
Total tests: 14 | Passed: 14 | Failed: 0
```

---

## 📄 License
This modernized codebase is open source under the MIT License, honoring OpenCBS's original mission of expanding global financial inclusion through open software.
