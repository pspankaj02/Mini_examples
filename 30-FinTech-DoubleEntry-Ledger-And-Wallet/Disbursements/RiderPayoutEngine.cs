// ==============================================================================================
// KiranaHub FinTech Ledger: Delivery Partner Earnings Accrual & Payout Engine
// [KYA KARTA HAI] Completed orders par rider base fee, distance bonus, aur surge incentive calculate karke double-entry ledger me accrue aur disburse karta hai.
// [KYU ZAROORI HAI] Delivery rider earnings ka 100% financial audit trail banana (Zero unpaid claims or ghost payouts).
// [KAISE CHALTA HAI] Accrual (Expense vs Payable) aur Settlement (Payable vs Bank) legs atomically journal me post karta hai.
// [OUTPUT / SIDE EFFECT] Transparent rider financial accounting.
// [VARIATIONS TO TRY] Deduct TDS tax withholding (1% section 194C) from rider payouts.
// ==============================================================================================

// [KYA KARTA HAI] System namespace import karta hai core types ke liye.
// [KYU ZAROORI HAI] Guid, Math, DateTimeOffset types provide karna.
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

// [KYA KARTA HAI] Concurrent collections import karta hai.
// [KYU ZAROORI HAI] Per-rider payable balance tracking.
// [KAISE CHALTA HAI] ConcurrentDictionary storage.
// [OUTPUT / SIDE EFFECT] Rider sub-ledger.
// [VARIATIONS TO TRY] None.
using System.Collections.Concurrent;

// [KYA KARTA HAI] Ledger model namespace import karta hai.
// [KYU ZAROORI HAI] LedgerEntry and EntryDirection access karna.
// [KAISE CHALTA HAI] References local model namespace.
// [OUTPUT / SIDE EFFECT] Model types accessible.
// [VARIATIONS TO TRY] None.
using KiranaHub.FinTechLedger.Model;

// [KYA KARTA HAI] Journal engine namespace import karta hai.
// [KYU ZAROORI HAI] DoubleEntryJournal access karna.
// [KAISE CHALTA HAI] References local engine namespace.
// [OUTPUT / SIDE EFFECT] Journal engine accessible.
// [VARIATIONS TO TRY] None.
using KiranaHub.FinTechLedger.Engine;

// [KYA KARTA HAI] Namespace declare karta hai payout disbursements ke liye.
// [KYU ZAROORI HAI] Code modularity.
// [KAISE CHALTA HAI] Scoped to KiranaHub.FinTechLedger.Disbursements.
// [OUTPUT / SIDE EFFECT] Namespace bounded.
// [VARIATIONS TO TRY] KiranaHub.FinTech.Payouts.
namespace KiranaHub.FinTechLedger.Disbursements;

// [KYA KARTA HAI] Rider calculated payout breakdown record.
// [KYU ZAROORI HAI] Payout components encapsulate karna (Base, Distance, Surge, Total).
// [KAISE CHALTA HAI] Immutable record with financial breakdown.
// [OUTPUT / SIDE EFFECT] Payout breakdown object.
// [VARIATIONS TO TRY] Fuel surcharge component.
public record RiderPayoutBreakdown(
    string OrderId,
    string RiderId,
    decimal BasePay,
    decimal DistanceBonus,
    decimal SurgeBonus,
    decimal TotalEarnings
);

// [KYA KARTA HAI] Rider Payout & Earnings Engine class.
// [KYU ZAROORI HAI] Delivery fee calculations aur ledger postings execute karna.
// [KAISE CHALTA HAI] Calculates formula earnings, posts accrual transaction, and disburses payouts.
// [OUTPUT / SIDE EFFECT] Rider payout engine instance.
// [VARIATIONS TO TRY] Daily batch auto-disbursement cron.
public class RiderPayoutEngine
{
    // [KYA KARTA HAI] Double entry journal dependency reference.
    // [KYU ZAROORI HAI] Journal postings execute karne ke liye.
    // [KAISE CHALTA HAI] Injected via constructor.
    // [OUTPUT / SIDE EFFECT] Journal handle stored.
    // [VARIATIONS TO TRY] None.
    private readonly DoubleEntryJournal _journal;

    // [KYA KARTA HAI] Rider sub-ledger payable balance map: RiderId -> payable balance.
    // [KYU ZAROORI HAI] Track karna ki company ko kis rider ko kitna paisa dena baaki hai.
    // [KAISE CHALTA HAI] ConcurrentDictionary updates on accruals and payouts.
    // [OUTPUT / SIDE EFFECT] Rider payable balances.
    // [VARIATIONS TO TRY] Sub-ledger database table.
    private readonly ConcurrentDictionary<string, decimal> _riderBalances = new();

    // [KYA KARTA HAI] Rider payout engine constructor initialize karta hai.
    // [KYU ZAROORI HAI] DoubleEntryJournal receive karna.
    // [KAISE CHALTA HAI] Assigns private field.
    // [OUTPUT / SIDE EFFECT] Engine instance ready.
    // [VARIATIONS TO TRY] None.
    public RiderPayoutEngine(DoubleEntryJournal journal)
    {
        _journal = journal;
    }

    // [KYA KARTA HAI] Completed delivery par rider earnings compute karke ledger me accrue karta hai.
    // [KYU ZAROORI HAI] Delivery complete hote hi expense acknowledge karna (Accrual Accounting):
    //   - Debit: Expenses:Rider_Payouts (Company expense increases)
    //   - Credit: Liabilities:Rider_Payables (Company owes payout to rider)
    // [KAISE CHALTA HAI] Formula: Base(₹35) + Distance(₹12/km over 2km) + Surge(₹20 if surge active).
    // [OUTPUT / SIDE EFFECT] Balanced journal transaction posted, rider balance incremented.
    // [VARIATIONS TO TRY] Bad weather incentive bonus.
    public (RiderPayoutBreakdown Breakdown, JournalTransaction Transaction) AccrueEarnings(
        string orderId,
        string riderId,
        double distanceKm,
        bool isSurge)
    {
        const decimal BasePay = 35m;
        decimal distanceBonus = distanceKm > 2.0 ? (decimal)Math.Round((distanceKm - 2.0) * 12.0, 2) : 0m;
        decimal surgeBonus = isSurge ? 20m : 0m;
        decimal totalEarnings = BasePay + distanceBonus + surgeBonus;

        var breakdown = new RiderPayoutBreakdown(orderId, riderId, BasePay, distanceBonus, surgeBonus, totalEarnings);

        var entries = new List<LedgerEntry>
        {
            new LedgerEntry("Expenses:Rider_Payouts", EntryDirection.Debit, totalEarnings),
            new LedgerEntry("Liabilities:Rider_Payables", EntryDirection.Credit, totalEarnings)
        };

        var tx = _journal.PostTransaction(
            transactionId: $"ACCRUE-{orderId}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}",
            description: $"Earnings accrual for rider {riderId} on order {orderId} (Dist: {distanceKm}km, Surge: {isSurge})",
            timestamp: DateTimeOffset.UtcNow,
            entries: entries
        );

        _riderBalances.AddOrUpdate(riderId, totalEarnings, (_, old) => old + totalEarnings);
        return (breakdown, tx);
    }

    // [KYA KARTA HAI] Accrued earnings ko rider ke bank account me transfer (Settlement) mark karta hai.
    // [KYU ZAROORI HAI] Payout settlement entry:
    //   - Debit: Liabilities:Rider_Payables (Liability cleared)
    //   - Credit: Assets:Bank_HDFC (Money leaves company bank account)
    // [KAISE CHALTA HAI] Verifies rider balance >= amount, posts balanced journal transaction, clears sub-ledger.
    // [OUTPUT / SIDE EFFECT] Rider liability cleared, bank account decremented.
    // [VARIATIONS TO TRY] RazorpayX / Cashfree Payouts API webhook integration.
    public JournalTransaction DisbursePayout(string riderId, decimal amount, string bankUtr)
    {
        if (amount <= 0) throw new ArgumentException("Payout amount must be positive!", nameof(amount));

        _riderBalances.TryGetValue(riderId, out var currentPayable);
        if (currentPayable < amount)
            throw new InvalidOperationException($"Cannot disburse ₹{amount}! Accrued payable for rider '{riderId}' is only ₹{currentPayable}.");

        var entries = new List<LedgerEntry>
        {
            new LedgerEntry("Liabilities:Rider_Payables", EntryDirection.Debit, amount),
            new LedgerEntry("Assets:Bank_HDFC", EntryDirection.Credit, amount)
        };

        var tx = _journal.PostTransaction(
            transactionId: $"PAYOUT-{riderId}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}",
            description: $"Bank payout disbursement to rider {riderId} via Bank UTR: {bankUtr}",
            timestamp: DateTimeOffset.UtcNow,
            entries: entries
        );

        _riderBalances.AddOrUpdate(riderId, 0m, (_, old) => old - amount);
        return tx;
    }

    // [KYA KARTA HAI] Specific rider ka pending payable balance return karta hai.
    // [KYU ZAROORI HAI] Rider mobile app me "Earnings Available for Payout" display karna.
    // [KAISE CHALTA HAI] Reads _riderBalances dictionary.
    // [OUTPUT / SIDE EFFECT] Decimal payable balance.
    // [VARIATIONS TO TRY] None.
    public decimal GetRiderPayableBalance(string riderId)
    {
        _riderBalances.TryGetValue(riderId, out var balance);
        return balance;
    }
}
