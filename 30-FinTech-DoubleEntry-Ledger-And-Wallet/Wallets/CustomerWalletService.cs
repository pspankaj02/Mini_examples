// ==============================================================================================
// KiranaHub FinTech Ledger: Customer Prepaid Wallet & Instant Refund Service
// [KYA KARTA HAI] Customer prepaid wallet top-ups, checkout debits, aur instant cancellations refunds ko double-entry transactions me post karta hai.
// [KYU ZAROORI HAI] Quick-commerce me cancelled item ka refund customer ko 3-5 bank days wait karaye bina 1 second me wallet me credit karna with financial balance.
// [KAISE CHALTA HAI] DoubleEntryJournal ke accounts (Bank, Customer_Wallets, Sales_Revenue) me balanced multi-leg postings execute karta hai.
// [OUTPUT / SIDE EFFECT] Balanced wallet balance updates with permanent journal audit trail.
// [VARIATIONS TO TRY] Auto-top-up via UPI e-Mandate integration.
// ==============================================================================================

// [KYA KARTA HAI] System namespace import karta hai core types ke liye.
// [KYU ZAROORI HAI] Guid, DateTimeOffset types provide karna.
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
// [KYU ZAROORI HAI] Per-customer sub-ledger wallet tracking.
// [KAISE CHALTA HAI] ConcurrentDictionary storage.
// [OUTPUT / SIDE EFFECT] Sub-ledger registry.
// [VARIATIONS TO TRY] Dedicated sub-ledger table in DB.
using System.Collections.Concurrent;

// [KYA KARTA HAI] Ledger model namespace import karta hai.
// [KYU ZAROORI HAI] LedgerEntry, EntryDirection access karna.
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

// [KYA KARTA HAI] Namespace declare karta hai wallet services ke liye.
// [KYU ZAROORI HAI] Code modularity.
// [KAISE CHALTA HAI] Scoped to KiranaHub.FinTechLedger.Wallets.
// [OUTPUT / SIDE EFFECT] Namespace bounded.
// [VARIATIONS TO TRY] KiranaHub.FinTech.Wallets.
namespace KiranaHub.FinTechLedger.Wallets;

// [KYA KARTA HAI] Customer Wallet Service class.
// [KYU ZAROORI HAI] High-level wallet operations ko double-entry journal postings me translate karna.
// [KAISE CHALTA HAI] Invokes journal.PostTransaction() for top-ups, debits, and refunds.
// [OUTPUT / SIDE EFFECT] Wallet service instance.
// [VARIATIONS TO TRY] Multi-currency wallet support.
public class CustomerWalletService
{
    // [KYA KARTA HAI] Double entry journal dependency reference.
    // [KYU ZAROORI HAI] Financial transactions post karne ke liye.
    // [KAISE CHALTA HAI] Injected via constructor.
    // [OUTPUT / SIDE EFFECT] Journal reference stored.
    // [VARIATIONS TO TRY] None.
    private readonly DoubleEntryJournal _journal;

    // [KYA KARTA HAI] Sub-ledger customer specific balance map: CustomerId -> balance.
    // [KYU ZAROORI HAI] General Ledger (GL) account `Liabilities:Customer_Wallets` ke breakdown me individual customer balance track karna.
    // [KAISE CHALTA HAI] ConcurrentDictionary maintains individual balances.
    // [OUTPUT / SIDE EFFECT] Sub-ledger balances.
    // [VARIATIONS TO TRY] Stored in relational sub-ledger table.
    private readonly ConcurrentDictionary<string, decimal> _customerBalances = new();

    // [KYA KARTA HAI] Customer wallet service constructor initialize karta hai.
    // [KYU ZAROORI HAI] DoubleEntryJournal receive karna.
    // [KAISE CHALTA HAI] Assigns private field.
    // [OUTPUT / SIDE EFFECT] Service instance ready.
    // [VARIATIONS TO TRY] None.
    public CustomerWalletService(DoubleEntryJournal journal)
    {
        _journal = journal;
    }

    // [KYA KARTA HAI] Customer wallet me paise add (Top-Up) karta hai.
    // [KYU ZAROORI HAI] Prepaid wallet balance load karna:
    //   - Debit: Assets:Bank_HDFC (Company bank receives cash)
    //   - Credit: Liabilities:Customer_Wallets (Company owes this money to customer)
    // [KAISE CHALTA HAI] Posts 2-leg balanced transaction and updates sub-ledger.
    // [OUTPUT / SIDE EFFECT] Journal transaction posted, wallet balance incremented.
    // [VARIATIONS TO TRY] Add payment gateway transaction ID.
    public JournalTransaction TopUp(string customerId, decimal amount, string bankRef)
    {
        if (amount <= 0) throw new ArgumentException("Top-up amount must be positive!", nameof(amount));

        var entries = new List<LedgerEntry>
        {
            new LedgerEntry("Assets:Bank_HDFC", EntryDirection.Debit, amount),
            new LedgerEntry("Liabilities:Customer_Wallets", EntryDirection.Credit, amount)
        };

        var tx = _journal.PostTransaction(
            transactionId: $"TOPUP-{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
            description: $"Wallet top-up for {customerId} via Bank Ref: {bankRef}",
            timestamp: DateTimeOffset.UtcNow,
            entries: entries
        );

        _customerBalances.AddOrUpdate(customerId, amount, (_, old) => old + amount);
        return tx;
    }

    // [KYA KARTA HAI] Grocery order checkout ke liye wallet se paise deduct karta hai.
    // [KYU ZAROORI HAI] Customer payment process karna:
    //   - Debit: Liabilities:Customer_Wallets (Company liability decreases)
    //   - Credit: Revenue:Grocery_Sales_Commission (Company earns revenue)
    // [KAISE CHALTA HAI] Verifies sufficient balance, posts balanced journal transaction, updates sub-ledger.
    // [OUTPUT / SIDE EFFECT] Journal transaction posted, wallet balance decremented.
    // [VARIATIONS TO TRY] Split revenue into vendor payout and platform fee.
    public JournalTransaction DeductForOrder(string customerId, string orderId, decimal amount)
    {
        if (amount <= 0) throw new ArgumentException("Deduction amount must be positive!", nameof(amount));

        var currentBal = GetCustomerBalance(customerId);
        if (currentBal < amount)
            throw new InvalidOperationException($"Insufficient wallet balance for '{customerId}'! Available: ₹{currentBal}, Required: ₹{amount}");

        var entries = new List<LedgerEntry>
        {
            new LedgerEntry("Liabilities:Customer_Wallets", EntryDirection.Debit, amount),
            new LedgerEntry("Revenue:Grocery_Sales_Commission", EntryDirection.Credit, amount)
        };

        var tx = _journal.PostTransaction(
            transactionId: $"PAY-{orderId}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}",
            description: $"Order payment from wallet for {orderId} (Customer: {customerId})",
            timestamp: DateTimeOffset.UtcNow,
            entries: entries
        );

        _customerBalances.AddOrUpdate(customerId, 0m, (_, old) => old - amount);
        return tx;
    }

    // [KYA KARTA HAI] Cancelled item ka instant refund customer wallet me credit karta hai.
    // [KYU ZAROORI HAI] Reverse accounting entry:
    //   - Debit: Revenue:Grocery_Sales_Commission (Contra-revenue decreases sales)
    //   - Credit: Liabilities:Customer_Wallets (Customer wallet liability increases back)
    // [KAISE CHALTA HAI] Posts balanced reverse journal transaction and restores sub-ledger balance.
    // [OUTPUT / SIDE EFFECT] Customer wallet instantly refunded.
    // [VARIATIONS TO TRY] Include cancellation reason in metadata.
    public JournalTransaction RefundCancelledItem(string customerId, string orderId, decimal refundAmount, string reason)
    {
        if (refundAmount <= 0) throw new ArgumentException("Refund amount must be positive!", nameof(refundAmount));

        var entries = new List<LedgerEntry>
        {
            new LedgerEntry("Revenue:Grocery_Sales_Commission", EntryDirection.Debit, refundAmount),
            new LedgerEntry("Liabilities:Customer_Wallets", EntryDirection.Credit, refundAmount)
        };

        var tx = _journal.PostTransaction(
            transactionId: $"REFUND-{orderId}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}",
            description: $"Instant refund for {orderId} to {customerId}. Reason: {reason}",
            timestamp: DateTimeOffset.UtcNow,
            entries: entries
        );

        _customerBalances.AddOrUpdate(customerId, refundAmount, (_, old) => old + refundAmount);
        return tx;
    }

    // [KYA KARTA HAI] Specific customer ka current wallet sub-ledger balance return karta hai.
    // [KYU ZAROORI HAI] Customer app UI me live wallet balance display karna.
    // [KAISE CHALTA HAI] Reads _customerBalances dictionary.
    // [OUTPUT / SIDE EFFECT] Decimal wallet balance.
    // [VARIATIONS TO TRY] Reconcile with GL account balance.
    public decimal GetCustomerBalance(string customerId)
    {
        _customerBalances.TryGetValue(customerId, out var balance);
        return balance;
    }
}
