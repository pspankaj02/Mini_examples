// ==============================================================================================
// KiranaHub FinTech Ledger: Automated Financial Verification & Ledger Integrity Suite
// [KYA KARTA HAI] Bank-grade double-entry journal, customer wallet operations, instant refunds, rider payouts, aur global trial balance verify karta hai.
// [KYU ZAROORI HAI] Confirm karna ki financial invariants strictly enforce hote hain, unbalanced transactions reject hoti hain, aur audit trails 100% balance rehti hain.
// [KAISE CHALTA HAI] Program Main executes 4 financial tests: Invariant Enforcement, Wallet & Refunds, Rider Payouts, and Global Trial Balance.
// [OUTPUT / SIDE EFFECT] Detailed console financial balances and clean exit code 0.
// [VARIATIONS TO TRY] Generate balance sheet and profit & loss (P&L) statements.
// ==============================================================================================

// [KYA KARTA HAI] System namespace import karta hai core types ke liye.
// [KYU ZAROORI HAI] Console, Exception, List types provide karna.
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

// [KYA KARTA HAI] LINQ queries import karta hai.
// [KYU ZAROORI HAI] Sum and formatting queries.
// [KAISE CHALTA HAI] LINQ library loaded.
// [OUTPUT / SIDE EFFECT] LINQ operators ready.
// [VARIATIONS TO TRY] None.
using System.Linq;

// [KYA KARTA HAI] Ledger model namespace import karta hai.
// [KYU ZAROORI HAI] LedgerEntry, AccountType, EntryDirection access karna.
// [KAISE CHALTA HAI] References local model namespace.
// [OUTPUT / SIDE EFFECT] Model types accessible.
// [VARIATIONS TO TRY] None.
using KiranaHub.FinTechLedger.Model;

// [KYA KARTA HAI] Journal engine namespace import karta hai.
// [KYU ZAROORI HAI] DoubleEntryJournal and UnbalancedTransactionException access karna.
// [KAISE CHALTA HAI] References local engine namespace.
// [OUTPUT / SIDE EFFECT] Engine types accessible.
// [VARIATIONS TO TRY] None.
using KiranaHub.FinTechLedger.Engine;

// [KYA KARTA HAI] Wallet namespace import karta hai.
// [KYU ZAROORI HAI] CustomerWalletService access karna.
// [KAISE CHALTA HAI] References local wallet namespace.
// [OUTPUT / SIDE EFFECT] Wallet types accessible.
// [VARIATIONS TO TRY] None.
using KiranaHub.FinTechLedger.Wallets;

// [KYA KARTA HAI] Disbursements namespace import karta hai.
// [KYU ZAROORI HAI] RiderPayoutEngine access karna.
// [KAISE CHALTA HAI] References local disbursements namespace.
// [OUTPUT / SIDE EFFECT] Payout types accessible.
// [VARIATIONS TO TRY] None.
using KiranaHub.FinTechLedger.Disbursements;

// [KYA KARTA HAI] Program entry point class define karta hai.
// [KYU ZAROORI HAI] Executable console application container.
// [KAISE CHALTA HAI] C# 13 top-level application wrapper.
// [OUTPUT / SIDE EFFECT] Entry class.
// [VARIATIONS TO TRY] Top-level statements.
public class Program
{
    // [KYA KARTA HAI] Application main entrypoint.
    // [KYU ZAROORI HAI] Financial tests execute karna.
    // [KAISE CHALTA HAI] CLR executes Main method.
    // [OUTPUT / SIDE EFFECT] Financial audit reports.
    // [VARIATIONS TO TRY] None.
    public static void Main()
    {
        Console.WriteLine("====================================================================");
        Console.WriteLine("  KiranaHub Project 30: FinTech Double-Entry Ledger & Wallet Suite  ");
        Console.WriteLine("====================================================================\n");

        var journal = new DoubleEntryJournal();

        // -------------------------------------------------------------
        // Setup Standard Chart of Accounts
        // -------------------------------------------------------------
        journal.CreateAccount("Assets:Bank_HDFC", "HDFC Corporate Bank Account", AccountType.Asset);
        journal.CreateAccount("Liabilities:Customer_Wallets", "Customer Prepaid Wallet Liabilities", AccountType.Liability);
        journal.CreateAccount("Liabilities:Rider_Payables", "Delivery Partner Accrued Payables", AccountType.Liability);
        journal.CreateAccount("Revenue:Grocery_Sales_Commission", "Grocery Platform Commission Revenue", AccountType.Revenue);
        journal.CreateAccount("Expenses:Rider_Payouts", "Rider Delivery Remuneration Expense", AccountType.Expense);

        var walletService = new CustomerWalletService(journal);
        var payoutEngine = new RiderPayoutEngine(journal);

        // [TEST 1] Testing Double-Entry Invariant & Unbalanced Rejection.
        Console.WriteLine("[TEST 1] Testing Accounting Invariant Enforcement (Debits == Credits)...");
        bool rejectedUnbalanced = false;

        try
        {
            // Intentionally unbalanced transaction: Debit ₹500, Credit ₹450 (Delta ₹50)
            journal.PostTransaction(
                transactionId: "TXN-TEST-IMBALANCED",
                description: "Corrupt transaction test",
                timestamp: DateTimeOffset.UtcNow,
                entries: new List<LedgerEntry>
                {
                    new LedgerEntry("Assets:Bank_HDFC", EntryDirection.Debit, 500m),
                    new LedgerEntry("Liabilities:Customer_Wallets", EntryDirection.Credit, 450m)
                }
            );
        }
        catch (UnbalancedTransactionException ex)
        {
            rejectedUnbalanced = true;
            Console.WriteLine($"  -> Caught expected Invariant Rejection: {ex.Message}: PASSED");
        }

        if (!rejectedUnbalanced)
            throw new Exception("Double-entry engine allowed an unbalanced transaction to be committed!");

        // [TEST 2] Testing Customer Wallet Lifecycle & Instant Refund.
        Console.WriteLine("\n[TEST 2] Testing Customer Wallet Top-Up, Checkout Debit & Instant Refund...");
        const string customerId = "CUST-ANITA-88";

        // Step 1: Top-up ₹2,000
        walletService.TopUp(customerId, 2000m, "UPI-REF-99214");
        var bal1 = walletService.GetCustomerBalance(customerId);
        if (bal1 != 2000m)
            throw new Exception($"Expected wallet balance ₹2000, but got ₹{bal1}!");

        Console.WriteLine($"  -> Wallet Top-Up Successful: Customer Balance = ₹{bal1}: PASSED");

        // Step 2: Checkout debit ₹750
        walletService.DeductForOrder(customerId, "ORD-7001", 750m);
        var bal2 = walletService.GetCustomerBalance(customerId);
        if (bal2 != 1250m)
            throw new Exception($"Expected wallet balance ₹1250 after ₹750 debit, but got ₹{bal2}!");

        Console.WriteLine($"  -> Order Checkout Debit Successful: Remaining Balance = ₹{bal2}: PASSED");

        // Step 3: Instant cancellation refund of ₹150 (out-of-stock butter)
        walletService.RefundCancelledItem(customerId, "ORD-7001", 150m, "Item damaged in transit");
        var bal3 = walletService.GetCustomerBalance(customerId);
        if (bal3 != 1400m)
            throw new Exception($"Expected wallet balance ₹1400 after ₹150 refund, but got ₹{bal3}!");

        Console.WriteLine($"  -> Instant Refund Successful: Restored Balance = ₹{bal3}: PASSED");

        // [TEST 3] Testing Rider Earnings Accrual, Rain Surge Bonus & Bank Settlement.
        Console.WriteLine("\n[TEST 3] Testing Delivery Partner Earnings Accrual & Bank Settlement...");
        const string riderId = "RIDER-DEEPAK-42";

        // Accrue earnings for 3.5 km delivery under rain surge
        // Formula: Base ₹35 + Distance((3.5 - 2.0) * 12 = ₹18) + Surge ₹20 = ₹73
        var (breakdown, accrueTx) = payoutEngine.AccrueEarnings("ORD-7001", riderId, distanceKm: 3.5, isSurge: true);
        if (breakdown.TotalEarnings != 73m)
            throw new Exception($"Expected rider earnings ₹73, but computed ₹{breakdown.TotalEarnings}!");

        var riderPayable = payoutEngine.GetRiderPayableBalance(riderId);
        if (riderPayable != 73m)
            throw new Exception($"Expected rider payable ₹73, but got ₹{riderPayable}!");

        Console.WriteLine($"  -> Earnings Accrued: Base=₹{breakdown.BasePay}, Distance=₹{breakdown.DistanceBonus}, Surge=₹{breakdown.SurgeBonus} -> Total: ₹{breakdown.TotalEarnings}: PASSED");

        // Disburse payout via bank transfer
        payoutEngine.DisbursePayout(riderId, 73m, "IMPS-UTR-782194");
        var riderPayableAfter = payoutEngine.GetRiderPayableBalance(riderId);
        if (riderPayableAfter != 0m)
            throw new Exception($"Expected rider payable ₹0 after payout, but got ₹{riderPayableAfter}!");

        Console.WriteLine($"  -> Bank Settlement Disbursed: Rider Payable cleared to ₹{riderPayableAfter}: PASSED");

        // [TEST 4] Testing Global Trial Balance & Accounting Equation Verification.
        Console.WriteLine("\n[TEST 4] Testing Global Trial Balance & Financial Balance Sheet Integrity...");

        journal.AssertTrialBalance();

        var bankAccount = journal.GetAccount("Assets:Bank_HDFC")!;
        var walletLiability = journal.GetAccount("Liabilities:Customer_Wallets")!;
        var riderPayableAccount = journal.GetAccount("Liabilities:Rider_Payables")!;
        var revenueAccount = journal.GetAccount("Revenue:Grocery_Sales_Commission")!;
        var expenseAccount = journal.GetAccount("Expenses:Rider_Payouts")!;

        Console.WriteLine("  -> Chart of Accounts Net Balances:");
        Console.WriteLine($"     * [ASSET]     Bank Account (HDFC)      : ₹{bankAccount.Balance} (Debit)");
        Console.WriteLine($"     * [LIABILITY] Customer Wallets         : ₹{walletLiability.Balance} (Credit)");
        Console.WriteLine($"     * [LIABILITY] Rider Payables           : ₹{riderPayableAccount.Balance} (Credit)");
        Console.WriteLine($"     * [REVENUE]   Platform Commission      : ₹{revenueAccount.Balance} (Credit)");
        Console.WriteLine($"     * [EXPENSE]   Rider Delivery Remun.    : ₹{expenseAccount.Balance} (Debit)");

        // Verify fundamental Accounting Equation: Assets = Liabilities + (Revenue - Expenses)
        // Bank (₹1927) == CustomerWallets (₹1400) + RiderPayables (₹0) + NetIncome (₹600 - ₹73 = ₹527)
        var netIncome = revenueAccount.Balance - expenseAccount.Balance;
        var totalLiabilitiesAndEquity = walletLiability.Balance + riderPayableAccount.Balance + netIncome;

        if (bankAccount.Balance != totalLiabilitiesAndEquity)
        {
            throw new Exception($"Accounting Equation Mismatch! Assets (₹{bankAccount.Balance}) != Liabilities + Equity (₹{totalLiabilitiesAndEquity})");
        }

        Console.WriteLine($"  -> Fundamental Accounting Equation Satisfied: Assets (₹{bankAccount.Balance}) == Liabilities + Net Income (₹{totalLiabilitiesAndEquity}): PASSED");

        Console.WriteLine("\n====================================================================");
        Console.WriteLine(" [SUCCESS] All FinTech Double-Entry & Wallet checks PASSED!         ");
        Console.WriteLine("====================================================================\n");
    }
}
