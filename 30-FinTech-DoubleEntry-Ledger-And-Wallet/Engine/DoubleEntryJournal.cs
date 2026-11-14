// ==============================================================================================
// KiranaHub FinTech Ledger: Immutable Double-Entry Journal Engine
// [KYA KARTA HAI] Atomic multi-leg journal transactions post karta hai aur mathematical invariant (Debits == Credits) strictly enforce karta hai.
// [KYU ZAROORI HAI] FinTech audit compliance — kisi bhi single transaction me ₹0.01 ka bhi imbalance allow na karna.
// [KAISE CHALTA HAI] Sum of Debits aur Sum of Credits compare karta hai; match hone par hi atomically ledger accounts update karta hai.
// [OUTPUT / SIDE EFFECT] Tamper-proof balanced financial journal transaction.
// [VARIATIONS TO TRY] Cryptographic Merkle tree hashing of journal transactions for blockchain auditability.
// ==============================================================================================

// [KYA KARTA HAI] System namespace import karta hai core types ke liye.
// [KYU ZAROORI HAI] Guid, Exception, DateTimeOffset types provide karna.
// [KAISE CHALTA HAI] Core types loaded.
// [OUTPUT / SIDE EFFECT] Standard utilities available.
// [VARIATIONS TO TRY] None.
using System;

// [KYA KARTA HAI] Collections generic import karta hai.
// [KYU ZAROORI HAI] List, Dictionary, IReadOnlyList provide karna.
// [KAISE CHALTA HAI] Generic collections loaded.
// [OUTPUT / SIDE EFFECT] Collections available.
// [VARIATIONS TO TRY] None.
using System.Collections.Generic;

// [KYA KARTA HAI] Concurrent collections import karta hai.
// [KYU ZAROORI HAI] Multi-threaded accounts and transactions thread-safely maintain karna.
// [KAISE CHALTA HAI] ConcurrentDictionary storage.
// [OUTPUT / SIDE EFFECT] Thread-safe registry.
// [VARIATIONS TO TRY] Partitioned tables.
using System.Collections.Concurrent;

// [KYA KARTA HAI] LINQ queries import karta hai.
// [KYU ZAROORI HAI] Sum and Where queries ke liye.
// [KAISE CHALTA HAI] LINQ library loaded.
// [OUTPUT / SIDE EFFECT] LINQ operators ready.
// [VARIATIONS TO TRY] None.
using System.Linq;

// [KYA KARTA HAI] Ledger model namespace import karta hai.
// [KYU ZAROORI HAI] LedgerAccount, LedgerEntry, AccountType, EntryDirection access karna.
// [KAISE CHALTA HAI] References local model namespace.
// [OUTPUT / SIDE EFFECT] Model types accessible.
// [VARIATIONS TO TRY] None.
using KiranaHub.FinTechLedger.Model;

// [KYA KARTA HAI] Namespace declare karta hai journal engine ke liye.
// [KYU ZAROORI HAI] Code modularity.
// [KAISE CHALTA HAI] Scoped to KiranaHub.FinTechLedger.Engine.
// [OUTPUT / SIDE EFFECT] Namespace bounded.
// [VARIATIONS TO TRY] KiranaHub.FinTech.Core.
namespace KiranaHub.FinTechLedger.Engine;

// [KYA KARTA HAI] Unbalanced transaction exception class.
// [KYU ZAROORI HAI] Jab debits != credits hon toh financial rejection emit karna.
// [KAISE CHALTA HAI] Thrown on transaction validation failure.
// [OUTPUT / SIDE EFFECT] Custom financial exception.
// [VARIATIONS TO TRY] Include unbalanced legs in message.
public class UnbalancedTransactionException : Exception
{
    public decimal TotalDebits { get; }
    public decimal TotalCredits { get; }

    public UnbalancedTransactionException(string transactionId, decimal debits, decimal credits)
        : base($"[FINANCIAL INVARIANT VIOLATION] Transaction '{transactionId}' is unbalanced! Total Debits: ₹{debits}, Total Credits: ₹{credits} (Delta: ₹{Math.Abs(debits - credits)})")
    {
        TotalDebits = debits;
        TotalCredits = credits;
    }
}

// [KYA KARTA HAI] Committed journal transaction record.
// [KYU ZAROORI HAI] Tamper-proof journal audit history maintain karna.
// [KAISE CHALTA HAI] Immutable record of posted transaction with all its debit and credit legs.
// [OUTPUT / SIDE EFFECT] Journal transaction entry.
// [VARIATIONS TO TRY] Add digital signature property.
public record JournalTransaction(
    string TransactionId,
    string Description,
    DateTimeOffset Timestamp,
    IReadOnlyList<LedgerEntry> Entries,
    decimal BalancedAmount
);

// [KYA KARTA HAI] Double-Entry Journal Engine class.
// [KYU ZAROORI HAI] Central bank-grade accounting engine operations provide karna.
// [KAISE CHALTA HAI] Maintains accounts dictionary and executes balanced atomic postings under lock.
// [OUTPUT / SIDE EFFECT] Accounting engine instance.
// [VARIATIONS TO TRY] Distributed journal with two-phase commit (2PC).
public class DoubleEntryJournal
{
    // [KYA KARTA HAI] Chart of accounts dictionary: AccountId -> LedgerAccount.
    // [KYU ZAROORI HAI] Registered accounts access karna.
    // [KAISE CHALTA HAI] ConcurrentDictionary.
    // [OUTPUT / SIDE EFFECT] Accounts registry.
    // [VARIATIONS TO TRY] Read-only map after initialization.
    private readonly ConcurrentDictionary<string, LedgerAccount> _accounts = new();

    // [KYA KARTA HAI] Posted journal transactions audit log list.
    // [KYU ZAROORI HAI] Immutable chronological financial audit trail.
    // [KAISE CHALTA HAI] List of JournalTransaction.
    // [OUTPUT / SIDE EFFECT] Journal audit log.
    // [VARIATIONS TO TRY] Append-only file.
    private readonly List<JournalTransaction> _journalLog = new();

    // [KYA KARTA HAI] Engine synchronization lock object.
    // [KYU ZAROORI HAI] Multi-leg postings ko atomically execute karna.
    // [KAISE CHALTA HAI] Standard lock object.
    // [OUTPUT / SIDE EFFECT] Concurrency lock.
    // [VARIATIONS TO TRY] ReaderWriterLockSlim.
    private readonly object _lock = new();

    // [KYA KARTA HAI] Naya account create/register karta hai chart of accounts me.
    // [KYU ZAROORI HAI] Financial ledger accounts initialize karna.
    // [KAISE CHALTA HAI] Adds LedgerAccount to _accounts.
    // [OUTPUT / SIDE EFFECT] Registered account.
    // [VARIATIONS TO TRY] Throw if already exists.
    public LedgerAccount CreateAccount(string accountId, string name, AccountType type)
    {
        var account = new LedgerAccount(accountId, name, type);
        _accounts[accountId] = account;
        return account;
    }

    // [KYA KARTA HAI] Account lookup karta hai by ID.
    // [KYU ZAROORI HAI] Balance inspect karna.
    // [KAISE CHALTA HAI] Dictionary TryGetValue.
    // [OUTPUT / SIDE EFFECT] LedgerAccount or null.
    // [VARIATIONS TO TRY] Throw KeyNotFoundException if missing.
    public LedgerAccount? GetAccount(string accountId)
    {
        _accounts.TryGetValue(accountId, out var account);
        return account;
    }

    // [KYA KARTA HAI] Multi-leg double-entry transaction atomically post karta hai.
    // [KYU ZAROORI HAI] Enforce fundamental rule: Sum(Debits) == Sum(Credits).
    // [KAISE CHALTA HAI] Validates balance, verifies accounts exist, applies legs, records in journal.
    // [OUTPUT / SIDE EFFECT] Balanced committed JournalTransaction.
    // [VARIATIONS TO TRY] Add idempotency key check.
    public JournalTransaction PostTransaction(
        string transactionId,
        string description,
        DateTimeOffset timestamp,
        IReadOnlyList<LedgerEntry> entries)
    {
        if (entries == null || entries.Count < 2)
            throw new ArgumentException("A double-entry transaction must have at least 2 legs (one debit, one credit)!", nameof(entries));

        lock (_lock)
        {
            // [KYA KARTA HAI] Sum of Debits and Credits calculate karta hai.
            // [KYU ZAROORI HAI] Accounting balance equation verify karna.
            // [KAISE CHALTA HAI] LINQ Sum filtering by Direction.
            // [OUTPUT / SIDE EFFECT] Sums computed.
            // [VARIATIONS TO TRY] Explicit loop for micro-optimization.
            var totalDebits = entries.Where(e => e.Direction == EntryDirection.Debit).Sum(e => e.Amount);
            var totalCredits = entries.Where(e => e.Direction == EntryDirection.Credit).Sum(e => e.Amount);

            // [KYA KARTA HAI] Mathematical Balance Invariant Check!
            // [KYU ZAROORI HAI] Imbalanced entry (Debits != Credits) reject karna before state mutation.
            // [KAISE CHALTA HAI] Compares decimal equality.
            // [OUTPUT / SIDE EFFECT] Throws UnbalancedTransactionException on imbalance.
            // [VARIATIONS TO TRY] Epsilon tolerance (never in real banking!).
            if (totalDebits != totalCredits)
            {
                throw new UnbalancedTransactionException(transactionId, totalDebits, totalCredits);
            }

            // [KYA KARTA HAI] Verify karta hai ki saare referenced accounts exist karte hain.
            // [KYU ZAROORI HAI] Ghost accounts par postings rokhna.
            // [KAISE CHALTA HAI] Checks _accounts.ContainsKey for each leg.
            // [OUTPUT / SIDE EFFECT] Throws InvalidOperationException if missing.
            // [VARIATIONS TO TRY] Auto-create sub-ledger accounts.
            foreach (var entry in entries)
            {
                if (!_accounts.ContainsKey(entry.AccountId))
                {
                    throw new InvalidOperationException($"Account '{entry.AccountId}' does not exist in Chart of Accounts!");
                }
            }

            // [KYA KARTA HAI] Atomic Application Phase: apply entries to accounts.
            // [KYU ZAROORI HAI] Certified state updates.
            // [KAISE CHALTA HAI] Invokes internal account.Apply().
            // [OUTPUT / SIDE EFFECT] Account balances updated.
            // [VARIATIONS TO TRY] Reversible staging log.
            foreach (var entry in entries)
            {
                var account = _accounts[entry.AccountId];
                account.Apply(entry.Direction, entry.Amount);
            }

            // [KYA KARTA HAI] Journal transaction record append karta hai audit log me.
            // [KYU ZAROORI HAI] Permanent financial history maintain karna.
            // [KAISE CHALTA HAI] Adds JournalTransaction to _journalLog.
            // [OUTPUT / SIDE EFFECT] Audit entry committed.
            // [VARIATIONS TO TRY] Write to append-only database table.
            var tx = new JournalTransaction(transactionId, description, timestamp, entries, totalDebits);
            _journalLog.Add(tx);
            return tx;
        }
    }

    // [KYA KARTA HAI] Global Trial Balance verification execute karta hai across all accounts.
    // [KYU ZAROORI HAI] Confirm karna ki Total All Debits == Total All Credits globally (System Integrity Check).
    // [KAISE CHALTA HAI] Sums TotalDebits and TotalCredits across all registered accounts.
    // [OUTPUT / SIDE EFFECT] Returns true if globally balanced, throws if corrupted.
    // [VARIATIONS TO TRY] Generate formatted Trial Balance financial sheet.
    public bool AssertTrialBalance()
    {
        lock (_lock)
        {
            var grandTotalDebits = _accounts.Values.Sum(a => a.TotalDebits);
            var grandTotalCredits = _accounts.Values.Sum(a => a.TotalCredits);

            if (grandTotalDebits != grandTotalCredits)
            {
                throw new Exception($"[LEDGER CORRUPTION] Global Trial Balance out of balance! Grand Debits: ₹{grandTotalDebits}, Grand Credits: ₹{grandTotalCredits}");
            }
            return true;
        }
    }

    // [KYA KARTA HAI] Saare registered accounts return karta hai.
    // [KYU ZAROORI HAI] Reporting and balance sheet generation.
    // [KAISE CHALTA HAI] Returns snapshot of _accounts.Values.
    // [OUTPUT / SIDE EFFECT] List of accounts.
    // [VARIATIONS TO TRY] Filter by AccountType.
    public IReadOnlyList<LedgerAccount> GetAllAccounts()
    {
        lock (_lock)
        {
            return _accounts.Values.ToList();
        }
    }
}
