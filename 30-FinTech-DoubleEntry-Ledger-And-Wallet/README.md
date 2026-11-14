# Project 30: FinTech Double-Entry Ledger, Multi-Party Wallets & Rider Earnings Accrual

> **Quick-Commerce Domain:** KiranaHub Bank-Grade Financial Accounting & Instant Settlement Engine  
> **Tech Stack:** C# 13, .NET 10, Double-Entry Bookkeeping ($\sum \text{Debits} \equiv \sum \text{Credits}$), Chart of Accounts, Normal Balance Evaluation, Prepaid Customer Wallets, Instant Out-of-Stock Item Refunds, Rider Delivery Payables & Bank Disbursements.

---

## 1. Iss Project Ko Scratch Se Kaise Padhein? (Roadmap)

Software development me junior engineers customer wallet ko ek single SQL column samajhte hain: `UPDATE Users SET WalletBalance = WalletBalance + 500`. Lekin real FinTech aur commercial platforms me aisi direct balance mutation **strictly illegal aur audit non-compliant** hoti hai. Agar kisi hacker ya glitch ne database me balance badha diya, toh company ko pata hi nahi chalega ki paisa kahan se aaya.

Double-entry bookkeeping me paisa hawa se create nahi ho sakta: har debit ke liye ek matching credit hona mandatory hai.

Aapko yeh code iss sequence me padhna hai:
1. **`Model/LedgerAccounts.cs`**:
   - Dekhiye Standard Chart of Accounts:
     - **Assets** (Bank Account) -> Debit Normal
     - **Liabilities** (Customer Wallets, Rider Payables) -> Credit Normal
     - **Revenue** (Platform Commission) -> Credit Normal
     - **Expenses** (Rider Remuneration) -> Debit Normal
2. **`Engine/DoubleEntryJournal.cs`**:
   - Samajhiye Double-Entry Invariant Enforcement:
     - Har journal transaction me multi-leg entries hoti hain.
     - Agar $\sum \text{Debits} \neq \sum \text{Credits}$ (chahe ₹0.01 ka difference ho), engine turant `UnbalancedTransactionException` throw karke transaction reject kar deta hai.
     - `AssertTrialBalance()` poore ledger me zero-sum balance verify karta hai.
3. **`Wallets/CustomerWalletService.cs`**:
   - Dekhiye real-world consumer transactions:
     - **Top-Up**: Debit Bank / Credit Customer_Wallets Liability.
     - **Checkout**: Debit Customer_Wallets Liability / Credit Revenue.
     - **Instant Refund**: Debit Revenue (Contra-revenue) / Credit Customer_Wallets Liability. Customer ko bina bank delay ke instant refund mil jata hai!
4. **`Disbursements/RiderPayoutEngine.cs`**:
   - Dekhiye gig-worker delivery partner accounting:
     - Delivery complete hote hi **Accrual**: Debit Expenses / Credit Rider_Payables.
     - Bank transfer hote hi **Settlement**: Debit Rider_Payables / Credit Bank.
5. **`Program.cs`**:
   - Automated benchmark test jo balance sheet equation satisfy karke dikhata hai:
     $$\text{Assets} \equiv \text{Liabilities} + \text{Net Income}$$

---

## 2. Kya Pehle Bna, Kaise Bna? (Foundations)

### Foundation: Luca Pacioli's Double-Entry Accounting Invariant (1494)
- Kisi bhi business me assets sirf do sources se aate hain:
  1. Ya toh creditors/customers se borrow kiye gaye hain (**Liabilities**).
  2. Ya business ne profit earn kiya hai (**Equity & Net Income**).
- Isliye fundamental equation hamesha true rehti hai:
  $$\text{Assets} \equiv \text{Liabilities} + \text{Equity} + (\text{Revenue} - \text{Expenses})$$
- Har transaction me Debit side aur Credit side ek doosre ko perfectly balance karte hain:
  $$\sum \text{All Debits} - \sum \text{All Credits} \equiv 0.00$$

```
Customer Tops Up ₹2,000 via UPI
                 │
                 ▼ [DoubleEntryJournal: PostTransaction]
┌────────────────────────────────────────────────────────┐
│ Leg 1: Debit  Assets:Bank_HDFC              ₹2,000     │
│ Leg 2: Credit Liabilities:Customer_Wallets  ₹2,000     │
│ (Sum Debits ₹2000 == Sum Credits ₹2000 -> BALANCED)    │
└────────────────────────────────────────────────────────┘
                 │
                 ▼ [Customer Orders ₹750 Grocery]
┌────────────────────────────────────────────────────────┐
│ Leg 1: Debit  Liabilities:Customer_Wallets  ₹750       │
│ Leg 2: Credit Revenue:Grocery_Sales         ₹750       │
└────────────────────────────────────────────────────────┘
                 │
                 ▼ [Damaged Paneer Instant Refund ₹150]
┌────────────────────────────────────────────────────────┐
│ Leg 1: Debit  Revenue:Grocery_Sales         ₹150       │
│ Leg 2: Credit Liabilities:Customer_Wallets  ₹150       │
└────────────────────────────────────────────────────────┘
```

---

## 3. Phir Kya Aaya, Kyu Aaya, Kaise Aaya, Kya Karta Hai?

### Step 1: `LedgerAccounts.cs` (Normal Balance Rules)
- **Kyu Aaya?** Floating point numbers (`double`/`float`) me rounding errors hote hain (e.g. `0.1 + 0.2 = 0.30000000000000004`). FinTech me strict `decimal` use hona mandatory hai.
- **Kaise Kaam Karta Hai?**
  - Account types ke according balance calculate hota hai.
  - Assets ke liye `Debits - Credits`, Liabilities ke liye `Credits - Debits`.

### Step 2: `DoubleEntryJournal.cs` (Atomic Balancing Engine)
- **Kyu Aaya?** Agar server crash ho jaye jab customer ke wallet se paise cut gaye par merchant ko credit nahi hue, toh paisa gayab ho jata hai (Ghost Disappearance).
- **Kaise Kaam Karta Hai?**
  - Saare legs ek single atomic lock ke andar evaluate hote hain.
  - Imbalanced entry ko commit se pehle hi reject kar diya jata hai.

### Step 3: `CustomerWalletService.cs` (Prepaid Sub-Ledger)
- **Kyu Aaya?** Quick-commerce me customer ko har ₹20 ke packet ke liye bar-bar UPI OTP daalna pasand nahi hota.
- **Kaise Kaam Karta Hai?**
  - General Ledger ke andar individual customer sub-ledger maintain hota hai.
  - Item out of stock hone par 1 millisecond me instant reverse ledger entry post hoti hai.

### Step 4: `RiderPayoutEngine.cs` (Accrual & Settlement Engine)
- **Kyu Aaya?** Delivery riders ki earnings transparently track karna aur bank settlement execute karna.
- **Kaise Kaam Karta Hai?**
  - Base pay, distance incentive (>2km), aur rain/surge bonus dynamically compute hote hain.
  - Accrual entry se liability create hoti hai aur IMPS/UPI disbursement se liability discharge hoti hai.

---

## 4. ⚠️ Uske Na Hone Se Kya Hoga? (Production Catastrophes)

| Component Missing | Real-World Production Catastrophe |
|---|---|
| **Without Double-Entry Invariant (Single column updates)** | Database bug ya rogue internal employee customer wallet me ₹10,00,000 edit kar dega aur withdraw kar lega. Audit trail na hone ki wajah se auditor fraud track nahi kar payenge. |
| **Without Strict `decimal` Arithmetic (using `float`/`double`)** | Millions of transactions process hone ke baad floating point drift ki wajah se bank account balance aur ledger balance me hazaron rupaye ka mismatch aayega (Audit Discrepancy & Regulatory Sanctions). |
| **Without Accrual Accounting for Riders** | Rider ko deliver karte waqt lagta hai ki company ne payment record kar li, par settlement server crash hone par record gayab ho jata hai. Riders protest karenge aur dark store operations band kar denge. |
| **Without Instant Reverse Refund Ledger Leg** | Customer ka damaged item refund hone me 5 banking days lagenge. Customer gusse me order cancel karke competitor app par switch ho jayega. |
| **Without Global Trial Balance Assertion** | Silent bugs ki wajah se partial entries leak hoti rahengi aur saal ke end me company ki balance sheet tally nahi hogi, resulting in loss of FinTech license. |

---

## 5. How to Run & Verify

Run from PowerShell:
```powershell
dotnet run --project d:\ToLearn_Antigravity\30-FinTech-DoubleEntry-Ledger-And-Wallet\KiranaHub.FinTechLedger.csproj
```

### Expected Output:
```text
====================================================================
  KiranaHub Project 30: FinTech Double-Entry Ledger & Wallet Suite  
====================================================================

[TEST 1] Testing Accounting Invariant Enforcement (Debits == Credits)...
  -> Caught expected Invariant Rejection: [FINANCIAL INVARIANT VIOLATION] Transaction 'TXN-TEST-IMBALANCED' is unbalanced! Total Debits: ₹500, Total Credits: ₹450 (Delta: ₹50): PASSED

[TEST 2] Testing Customer Wallet Top-Up, Checkout Debit & Instant Refund...
  -> Wallet Top-Up Successful: Customer Balance = ₹2000: PASSED
  -> Order Checkout Debit Successful: Remaining Balance = ₹1250: PASSED
  -> Instant Refund Successful: Restored Balance = ₹1400: PASSED

[TEST 3] Testing Delivery Partner Earnings Accrual & Bank Settlement...
  -> Earnings Accrued: Base=₹35, Distance=₹18, Surge=₹20 -> Total: ₹73: PASSED
  -> Bank Settlement Disbursed: Rider Payable cleared to ₹0: PASSED

[TEST 4] Testing Global Trial Balance & Financial Balance Sheet Integrity...
  -> Chart of Accounts Net Balances:
     * [ASSET]     Bank Account (HDFC)      : ₹1927 (Debit)
     * [LIABILITY] Customer Wallets         : ₹1400 (Credit)
     * [LIABILITY] Rider Payables           : ₹0 (Credit)
     * [REVENUE]   Platform Commission      : ₹600 (Credit)
     * [EXPENSE]   Rider Delivery Remun.    : ₹73 (Debit)
  -> Fundamental Accounting Equation Satisfied: Assets (₹1927) == Liabilities + Net Income (₹1927): PASSED

====================================================================
 [SUCCESS] All FinTech Double-Entry & Wallet checks PASSED!         
====================================================================
```
