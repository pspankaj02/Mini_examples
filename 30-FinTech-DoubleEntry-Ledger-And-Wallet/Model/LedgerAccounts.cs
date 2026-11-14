// ==============================================================================================
// KiranaHub FinTech Ledger: Standard Chart of Accounts & Normal Balance Definitions
// [KYA KARTA HAI] Bank-grade double-entry accounting Chart of Accounts (Assets, Liabilities, Equity, Revenue, Expenses) define karta hai.
// [KYU ZAROORI HAI] Floating point arithmetic ya random balance column update se bach kar financial math balance sheet equation satisfy karna.
// [KAISE CHALTA HAI] Normal balance orientation (Debit normal for Assets/Expenses, Credit normal for Liabilities/Revenue) maintain karta hai.
// [OUTPUT / SIDE EFFECT] Type-safe financial ledger account models.
// [VARIATIONS TO TRY] Multi-currency accounting with ISO-4217 currency codes (INR, USD).
// ==============================================================================================

// [KYA KARTA HAI] System namespace import karta hai core types ke liye.
// [KYU ZAROORI HAI] Guid, ArgumentException, Math types provide karna.
// [KAISE CHALTA HAI] Core types loaded.
// [OUTPUT / SIDE EFFECT] Standard utilities available.
// [VARIATIONS TO TRY] None.
using System;

// [KYA KARTA HAI] Collections generic import karta hai.
// [KYU ZAROORI HAI] List collections provide karna.
// [KAISE CHALTA HAI] Generic collections loaded.
// [OUTPUT / SIDE EFFECT] Collections available.
// [VARIATIONS TO TRY] None.
using System.Collections.Generic;

// [KYA KARTA HAI] Namespace declare karta hai ledger models ke liye.
// [KYU ZAROORI HAI] Code modularity.
// [KAISE CHALTA HAI] Scoped to KiranaHub.FinTechLedger.Model.
// [OUTPUT / SIDE EFFECT] Namespace bounded.
// [VARIATIONS TO TRY] KiranaHub.FinTech.Accounting.
namespace KiranaHub.FinTechLedger.Model;

// [KYA KARTA HAI] Fundamental 5 accounting account types enumeration.
// [KYU ZAROORI HAI] Financial statement categorizations enforce karna.
// [KAISE CHALTA HAI] Enum values: Asset, Liability, Equity, Revenue, Expense.
// [OUTPUT / SIDE EFFECT] Account type classification.
// [VARIATIONS TO TRY] Sub-types (Current Asset vs Non-Current Asset).
public enum AccountType
{
    Asset,
    Liability,
    Equity,
    Revenue,
    Expense
}

// [KYA KARTA HAI] Debit or Credit transaction entry direction enum.
// [KYU ZAROORI HAI] Double-entry posting leg identify karna.
// [KAISE CHALTA HAI] Debit or Credit.
// [OUTPUT / SIDE EFFECT] Direction flag.
// [VARIATIONS TO TRY] None.
public enum EntryDirection
{
    Debit,
    Credit
}

// [KYA KARTA HAI] Single line entry in a double-entry transaction record.
// [KYU ZAROORI HAI] Ek specific account par debit ya credit amount represent karna.
// [KAISE CHALTA HAI] Immutable record with AccountId, Direction, and Amount.
// [OUTPUT / SIDE EFFECT] Journal entry leg.
// [VARIATIONS TO TRY] Include description per line.
public record LedgerEntry(
    string AccountId,
    EntryDirection Direction,
    decimal Amount
);

// [KYA KARTA HAI] Financial Ledger Account state class.
// [KYU ZAROORI HAI] Har account ka running debit, credit, aur computed net balance track karna.
// [KAISE CHALTA HAI] AccountType ke normal balance rule ke mutabiq Net Balance calculate karta hai.
// [OUTPUT / SIDE EFFECT] Account instance.
// [VARIATIONS TO TRY] Snapshot account balances daily.
public class LedgerAccount
{
    // [KYA KARTA HAI] Unique account code identifier (e.g. "1010-ASSETS-CASH").
    // [KYU ZAROORI HAI] Chart of accounts address.
    // [KAISE CHALTA HAI] String property.
    // [OUTPUT / SIDE EFFECT] Account ID stored.
    // [VARIATIONS TO TRY] None.
    public string AccountId { get; }

    // [KYA KARTA HAI] Human-readable account title.
    // [KYU ZAROORI HAI] Balance sheet presentation.
    // [KAISE CHALTA HAI] String property.
    // [OUTPUT / SIDE EFFECT] Account name.
    // [VARIATIONS TO TRY] None.
    public string Name { get; }

    // [KYA KARTA HAI] Account type classification.
    // [KYU ZAROORI HAI] Normal balance calculation rule apply karna.
    // [KAISE CHALTA HAI] AccountType enum.
    // [OUTPUT / SIDE EFFECT] Account type stored.
    // [VARIATIONS TO TRY] None.
    public AccountType Type { get; }

    // [KYA KARTA HAI] Accumulated total debit postings sum.
    // [KYU ZAROORI HAI] Audit debit history.
    // [KAISE CHALTA HAI] Decimal accumulator.
    // [OUTPUT / SIDE EFFECT] Total debits.
    // [VARIATIONS TO TRY] Decimal with 4 decimal places for crypto.
    public decimal TotalDebits { get; private set; }

    // [KYA KARTA HAI] Accumulated total credit postings sum.
    // [KYU ZAROORI HAI] Audit credit history.
    // [KAISE CHALTA HAI] Decimal accumulator.
    // [OUTPUT / SIDE EFFECT] Total credits.
    // [VARIATIONS TO TRY] Decimal.
    public decimal TotalCredits { get; private set; }

    // [KYA KARTA HAI] Ledger account constructor initialize karta hai.
    // [KYU ZAROORI HAI] Account code, name aur classification set karna.
    // [KAISE CHALTA HAI] Assigns properties and zeroes accumulators.
    // [OUTPUT / SIDE EFFECT] Account instance ready.
    // [VARIATIONS TO TRY] None.
    public LedgerAccount(string accountId, string name, AccountType type)
    {
        AccountId = accountId;
        Name = name;
        Type = type;
        TotalDebits = 0m;
        TotalCredits = 0m;
    }

    // [KYA KARTA HAI] Certified net balance compute karta hai according to normal balance rules.
    // [KYU ZAROORI HAI] Assets/Expenses debit-normal hote hain (Debits - Credits), jabki Liabilities/Revenue credit-normal hote hain (Credits - Debits).
    // [KAISE CHALTA HAI] Conditional logic based on AccountType.
    // [OUTPUT / SIDE EFFECT] Certified net balance.
    // [VARIATIONS TO TRY] Signed balances where debit is positive and credit is negative.
    public decimal Balance
    {
        get
        {
            return Type switch
            {
                AccountType.Asset => TotalDebits - TotalCredits,
                AccountType.Expense => TotalDebits - TotalCredits,
                AccountType.Liability => TotalCredits - TotalDebits,
                AccountType.Equity => TotalCredits - TotalDebits,
                AccountType.Revenue => TotalCredits - TotalDebits,
                _ => TotalDebits - TotalCredits
            };
        }
    }

    // [KYA KARTA HAI] Posting leg apply karta hai account par.
    // [KYU ZAROORI HAI] Internal accumulators update karna strictly double-entry validation ke baad.
    // [KAISE CHALTA HAI] Increments TotalDebits on Debit, or TotalCredits on Credit.
    // [OUTPUT / SIDE EFFECT] Accumulators updated.
    // [VARIATIONS TO TRY] Audit sequence ID check.
    internal void Apply(EntryDirection direction, decimal amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), "Posting amount cannot be negative!");
        if (direction == EntryDirection.Debit)
            TotalDebits += amount;
        else
            TotalCredits += amount;
    }
}
