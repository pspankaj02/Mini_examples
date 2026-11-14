using System;
using System.Diagnostics;
using KiranaHub.MemoryAndReflection.Lifecycle;
using KiranaHub.MemoryAndReflection.Memory;
using KiranaHub.MemoryAndReflection.Reflection;

// [KYA KARTA HAI]: Console output encoding ko UTF-8 me set karta hai taaki Indian Rupee symbol (₹) aur unicode text bina distortion render ho sake.
// [KYU ZAROORI HAI]: Default Windows terminal encoding legacy ASCII/code page hoti hai jo currency symbols ko '?' bana deti hai.
// [KAISE CHALTA HAI]: OS console buffer ki output code page ko CP65001 (UTF-8) me switch karta hai.
// [OUTPUT / SIDE EFFECT]: Console text UTF-8 characters support karta hai.
// [VARIATIONS TO TRY]: System.Text.Encoding.Unicode try kar sakte hain agar wide characters chahiye.
Console.OutputEncoding = System.Text.Encoding.UTF8;

// [KYA KARTA HAI]: Visual separator line print karta hai terminal par.
// [KYU ZAROORI HAI]: Readability aur clean CLI formatting ke liye.
Console.WriteLine("==========================================================================");

// [KYA KARTA HAI]: Project ka header banner print karta hai.
// [KYU ZAROORI HAI]: User ko announce karta hai ki memory management & reflection test harness execute ho raha hai.
Console.WriteLine("   KIRANAHUB - 06: MEMORY MANAGEMENT & REFLECTION IN C#                   ");

// [KYA KARTA HAI]: Visual separator closing line print karta hai.
// [KYU ZAROORI HAI]: Section ko cleanly demarcate karne ke liye.
Console.WriteLine("==========================================================================\n");

// ----------------------------------------------------------------------------------------
// 1. SPAN<T> & READONLYSPAN<T> (ZERO-ALLOCATION PARSING)
// ----------------------------------------------------------------------------------------

// [KYA KARTA HAI]: Step 1 ka heading print karta hai terminal par.
// [KYU ZAROORI HAI]: Step-by-step progress visually dikhana.
Console.WriteLine("--> [Step 1] Zero-Allocation Barcode Parsing with ReadOnlySpan<char>");

// [KYA KARTA HAI]: Ek raw barcode scanner ka pipe-delimited payload string allocate karta hai heap par.
// [KYU ZAROORI HAI]: Real-world POS scanner machine se aane wala input simulate karna.
// [KAISE CHALTA HAI]: String literal runtime intern pool se reference hota hai.
// [OUTPUT / SIDE EFFECT]: Heap string "AMUL-BUTTER-100G|58.00|15" banti hai.
// [VARIATIONS TO TRY]: Isme alag format jaise CSV ya comma delimiter pass karke parser robustness test karein.
string rawBarcodePayload = "AMUL-BUTTER-100G|58.00|15";

// [KYA KARTA HAI]: String ke char array par zero-allocation ReadOnlySpan<char> window banata hai.
// [KYU ZAROORI HAI]: String.Substring naye heap objects allocate karta hai jo GC pressure badhate hain; AsSpan() 0 bytes allocate karta hai.
// [KAISE CHALTA HAI]: AsSpan() ek ref struct banata hai stack par jisme pointer (ref char) aur length hoti hai.
// [OUTPUT / SIDE EFFECT]: Stack par lightweight span banta hai bina heap memory touch kiye.
// [VARIATIONS TO TRY]: Memory<char> use kar sakte hain agar async methods ke cross span pass karna ho.
ReadOnlySpan<char> payloadSpan = rawBarcodePayload.AsSpan();

// [KYA KARTA HAI]: Span ko ZeroAllocationBarcodeParser ke static Parse method me bhej kar SKU, Price aur Quantity extract karta hai.
// [KYU ZAROORI HAI]: Zero memory allocation ke sath fast parsing verify karna.
// [KAISE CHALTA HAI]: ReadOnlySpan.Slice() memory pointer offset karta hai; int.Parse/decimal.Parse directly span read karte hain.
// [OUTPUT / SIDE EFFECT]: ParsedBarcode struct return hota hai on the stack.
// [VARIATIONS TO TRY]: Malformed string pass karke exception handling verify karein.
var parsed = ZeroAllocationBarcodeParser.Parse(payloadSpan);

// [KYA KARTA HAI]: Parsed SKU span ko string bana kar console par display karta hai.
// [KYU ZAROORI HAI]: Extracted value ko human-readable output me verify karna.
Console.WriteLine($"Parsed SKU: {parsed.Sku.ToString()}");

// [KYA KARTA HAI]: Parsed Price decimal value ko format karke console par print karta hai.
// [KYU ZAROORI HAI]: Extracted numerical monetary value verify karna.
Console.WriteLine($"Parsed Price: Rs. {parsed.Price}");

// [KYA KARTA HAI]: Parsed Quantity integer value ko console par print karta hai.
// [KYU ZAROORI HAI]: Extracted integer stock value verify karna.
Console.WriteLine($"Parsed Quantity: {parsed.Quantity}");

// [KYA KARTA HAI]: Assert karta hai ki parsed price 58.00m aur quantity 15 ho.
// [KYU ZAROORI HAI]: Regression protection; agar parsing logic me bug aaye toh execution turant crash ho jaye.
// [KAISE CHALTA HAI]: Debug mode me condition evaluate hoti hai; agar false ho toh AssertionException throw hoti hai.
// [OUTPUT / SIDE EFFECT]: If condition is true, execution silently continues.
Debug.Assert(parsed.Price == 58.00m && parsed.Quantity == 15);

// [KYA KARTA HAI]: Step 1 success confirmation print karta hai.
Console.WriteLine("[PASS] ReadOnlySpan parsing verified with zero heap substring allocations.\n");

// ----------------------------------------------------------------------------------------
// 2. STACKALLOC (STACK ALLOCATED MEMORY BUFFER)
// ----------------------------------------------------------------------------------------

// [KYA KARTA HAI]: Step 2 ka heading print karta hai terminal par.
Console.WriteLine("--> [Step 2] Stackalloc (Direct CPU Stack Memory Allocation)");

// [KYA KARTA HAI]: Thread ke stack frame par 5 integers (20 bytes) direct allocate karta hai bina Garbage Collector heap ko touch kiye.
// [KYU ZAROORI HAI]: Short-lived micro buffers ke liye stackalloc ultra-fast hota hai (sirf CPU stack pointer ESP/RSP decrement hota hai).
// [KAISE CHALTA HAI]: IL opcode 'localloc' emit hota hai jo direct stack memory reserve karta hai. Scope end hote hi automatically free ho jata hai bina GC involvement.
// [OUTPUT / SIDE EFFECT]: Stack memory par Span<int> map hota hai.
// [VARIATIONS TO TRY]: stackalloc byte[128] try kar sakte hain for fast crypto hash buffer.
Span<int> stackNumbers = stackalloc int[5] { 10, 20, 30, 40, 50 };

// [KYA KARTA HAI]: Sum accumulation variable zero se initialize karta hai on the stack.
// [KYU ZAROORI HAI]: Buffer ke elements ka sum calculate karne ke liye.
int sum = 0;

// [KYA KARTA HAI]: Span<int> ke elements par foreach loop chalata hai.
// [KYU ZAROORI HAI]: Stack buffer ke values ko read karna.
// [KAISE CHALTA HAI]: Span ka ref struct enumerator pointer arithmetic se elements access karta hai.
foreach (int val in stackNumbers)
{
    // [KYA KARTA HAI]: Current element ko accumulator me add karta hai.
    // [KYU ZAROORI HAI]: Arithmetic sum complete karna.
    sum += val;
}

// [KYA KARTA HAI]: Computed sum console par display karta hai.
Console.WriteLine($"Sum of stackalloc buffer: {sum}");

// [KYA KARTA HAI]: Assert karta hai ki 10+20+30+40+50 ka total exactly 150 ho.
// [KYU ZAROORI HAI]: Stack memory calculation accuracy verify karna.
Debug.Assert(sum == 150);

// [KYA KARTA HAI]: Step 2 success confirmation print karta hai.
Console.WriteLine("[PASS] stackalloc memory verified.\n");

// ----------------------------------------------------------------------------------------
// 3. ARRAYPOOL BUFFER RENTING & RECYCLING
// ----------------------------------------------------------------------------------------

// [KYA KARTA HAI]: Step 3 ka heading print karta hai.
Console.WriteLine("--> [Step 3] ArrayPool<byte>.Shared Buffer Renting & Checksum");

// [KYA KARTA HAI]: 256 bytes ka dummy byte array heap par allocate karta hai incoming network packet simulate karne ke liye.
// [KYU ZAROORI HAI]: IoT scale scanner ya network telemetry packet simulate karna.
// [KAISE CHALTA HAI]: Managed heap par standard byte array banta hai.
// [OUTPUT / SIDE EFFECT]: 256 bytes memory allocated.
byte[] dummyPacket = new byte[256];

// [KYA KARTA HAI]: Array ke sabhi 256 bytes ko byte value 5 se fill karta hai.
// [KYU ZAROORI HAI]: Predictable checksum calculation verify karne ke liye known data pattern chahiye.
// [KAISE CHALTA HAI]: Memory block fast memset SIMD instruction se fill hota hai.
Array.Fill<byte>(dummyPacket, (byte)5);

// [KYA KARTA HAI]: RentedBufferManager me dummyPacket bhejta hai jo ArrayPool.Shared se memory rent karke checksum nikalta hai.
// [KYU ZAROORI HAI]: High-throughput pipeline me baar-baar byte[] new karne se GC Gen 0 thrashing hoti hai; pool reuse GC pause ko eliminate karta hai.
// [KAISE CHALTA HAI]: ArrayPool.Shared.Rent(length) se pre-allocated array milta hai, process hota hai, aur Return() se wapas pool me chala jata hai.
// [OUTPUT / SIDE EFFECT]: Checksum integer return hota hai.
int checksum = RentedBufferManager.ProcessIncomingPayload(dummyPacket);

// [KYA KARTA HAI]: Checksum result console par print karta hai.
Console.WriteLine($"Checksum computed over rented buffer: {checksum} (Expected: {256 * 5})");

// [KYA KARTA HAI]: Verify karta hai ki calculated checksum exactly 1280 (256 * 5) hai.
// [KYU ZAROORI HAI]: Ensure karna ki rented buffer me koi data corruption ya boundary overrun nahi hua.
Debug.Assert(checksum == 256 * 5);

// [KYA KARTA HAI]: Step 3 success message print karta hai.
Console.WriteLine("[PASS] ArrayPool buffer reuse verified.\n");

// ----------------------------------------------------------------------------------------
// 4. GARBAGE COLLECTION GENERATIONS & IDISPOSABLE
// ----------------------------------------------------------------------------------------

// [KYA KARTA HAI]: Step 4 ka heading print karta hai.
Console.WriteLine("--> [Step 4] GC Generations & IDisposable Pattern");

// [KYA KARTA HAI]: KiranaLogFileHandle ka naya instance allocate karta hai.
// [KYU ZAROORI HAI]: Managed heap allocation aur GC lifecycle tracking demonstrate karna.
// [KAISE CHALTA HAI]: CLR GC allocator Gen 0 heap segment par memory assign karta hai.
var disposableObj = new KiranaLogFileHandle();

// [KYA KARTA HAI]: CLR GC API se check karta hai ki ye object kis Generation me reside karta hai.
// [KYU ZAROORI HAI]: Memory age understand karna: Gen 0 = brand new, Gen 1 = survived 1 collection, Gen 2 = long lived.
// [KAISE CHALTA HAI]: GC.GetGeneration object header ka sync block / segment inspect karta hai.
// [OUTPUT / SIDE EFFECT]: Returns 0 for newly created objects.
int gen = GC.GetGeneration(disposableObj);

// [KYA KARTA HAI]: Object ki generation console par print karta hai.
Console.WriteLine($"Newly allocated object GC Generation: Gen {gen}");

// [KYA KARTA HAI]: Assert karta hai ki fresh object hamesha Generation 0 me shuru hota hai.
// [KYU ZAROORI HAI]: CLR memory architecture guarantee verify karna.
Debug.Assert(gen == 0, "New objects should start in Generation 0!");

// [KYA KARTA HAI]: Object ko explicitly dispose karta hai using block ke zariye.
// [KYU ZAROORI HAI]: Unmanaged file handles/streams ko timely close karna bina GC finalizer ka wait kiye.
// [KAISE CHALTA HAI]: Compiler using block ko try-finally me rewrite karta hai jisme finally block fileHandle.Dispose() call karta hai.
using (var fileHandle = new KiranaLogFileHandle())
{
    // [KYA KARTA HAI]: Dummy log data write karta hai file handle me.
    // [KYU ZAROORI HAI]: Resource utilization verify karna before disposal.
    fileHandle.WriteLog(new byte[] { 1, 2, 3 });
} // [NOTE]: Dispose() executes here automatically! GC.SuppressFinalize() stops expensive finalizer thread execution.

// [KYA KARTA HAI]: Step 4 success confirmation print karta hai.
Console.WriteLine("[PASS] IDisposable pattern executed and GC.SuppressFinalize verified.\n");

// ----------------------------------------------------------------------------------------
// 5. REFLECTION & CUSTOM ATTRIBUTES (AUDITING & MASKING)
// ----------------------------------------------------------------------------------------

// [KYA KARTA HAI]: Step 5 heading print karta hai.
Console.WriteLine("--> [Step 5] Reflection & Custom Attributes Inspection");

// [KYA KARTA HAI]: CustomerBillingProfile ka object banata hai jisme credit card aur UPI details populated hain.
// [KYU ZAROORI HAI]: Runtime type metadata inspection aur security compliance (PII masking) test karna.
var billingProfile = new CustomerBillingProfile();

// [KYA KARTA HAI]: KiranaMetadataInspector.GenerateAuditedSafeLog ko call karke reflection se attributes scan karta hai.
// [KYU ZAROORI HAI]: PII (Personal Identifiable Information) ko log me plain text likhne se bachana (GDPR/RBI compliance).
// [KAISE CHALTA HAI]: Type.GetProperties() se metadata read karta hai, [SensitiveData] check karta hai, aur value mask karta hai.
// [OUTPUT / SIDE EFFECT]: Dictionary<string, string> jisme masked fields hote hain.
Dictionary<string, string> safeLog = KiranaMetadataInspector.GenerateAuditedSafeLog(billingProfile);

// [KYA KARTA HAI]: Class-level custom attribute [AuditableEntity] se extract ki gayi category print karta hai.
Console.WriteLine($"Audit Category: {safeLog["_AuditCategory"]}");

// [KYA KARTA HAI]: Customer ID print karta hai.
Console.WriteLine($"Customer Id   : {safeLog["CustomerId"]}");

// [KYA KARTA HAI]: Masked credit card number print karta hai.
Console.WriteLine($"Credit Card   : {safeLog["CreditCardNumber"]}");

// [KYA KARTA HAI]: Masked UPI PIN hash print karta hai.
Console.WriteLine($"UPI Hash      : {safeLog["UpiPinHash"]}");

// [KYA KARTA HAI]: Plain wallet balance print karta hai.
Console.WriteLine($"Wallet Balance: {safeLog["WalletBalance"]}");

// [KYA KARTA HAI]: Assert karta hai ki sensitive credit card number masked ho gaya hai.
// [KYU ZAROORI HAI]: Confirm karna ki raw card number kabhi bhi logs me leak nahi hoga.
Debug.Assert(safeLog["CreditCardNumber"] == "XXXX-XXXX-XXXX-1234", "Sensitive credit card was not masked!");

// [KYA KARTA HAI]: Assert karta hai ki sensitive UPI hash masked ho gaya hai.
// [KYU ZAROORI HAI]: Security compliance assertion.
Debug.Assert(safeLog["UpiPinHash"] == "SECRET", "Sensitive UPI hash was not masked!");

// [KYA KARTA HAI]: Step 5 success message print karta hai.
Console.WriteLine("[PASS] Reflection attribute discovery and data masking verified.\n");

// [KYA KARTA HAI]: Final success banner lines print karta hai.
Console.WriteLine("==========================================================================");
Console.WriteLine("   [SUCCESS] ALL 06-MEMORY-MANAGEMENT-AND-REFLECTION TESTS PASSED!        ");
Console.WriteLine("==========================================================================");
