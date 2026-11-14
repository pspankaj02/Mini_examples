namespace KiranaHub.MemoryAndReflection.Memory;

// =========================================================================================
// [CLASS PURPOSE]:
// High-performance, zero-allocation barcode and CSV payload parser using `ReadOnlySpan<char>`.
// Avoids `string.Split()` and `string.Substring()` to generate ZERO garbage collection overhead.
// =========================================================================================
public static class ZeroAllocationBarcodeParser
{
    // [REF STRUCT DECLARATION]:
    // [KYA KARTA HAI]: Stack-only struct that holds memory spans.
    // [KYU ZAROORI HAI]: Ref structs cannot be boxed or placed on the managed heap, ensuring 100% stack allocation.
    public readonly ref struct ParsedBarcode
    {
        // [SPAN PROPERTY]: Zero-copy window pointing directly to the original SKU memory segment
        public ReadOnlySpan<char> Sku { get; }

        // [NUMERIC PROPERTY]: Parsed unit price
        public decimal Price { get; }

        // [INTEGER PROPERTY]: Parsed inventory quantity
        public int Quantity { get; }

        // [CONSTRUCTOR]:
        public ParsedBarcode(ReadOnlySpan<char> sku, decimal price, int quantity)
        {
            // [ASSIGNMENTS]:
            Sku = sku;
            Price = price;
            Quantity = quantity;
        }
    }

    // [PARSING METHOD]: Accepts ReadOnlySpan<char> by value on the CPU stack
    public static ParsedBarcode Parse(ReadOnlySpan<char> rawData)
    {
        // [INDEX LOOKUP 1]: Searches for the first pipe '|' delimiter
        int firstPipe = rawData.IndexOf('|');

        // [DEFENSIVE GUARD CLAUSE]:
        if (firstPipe == -1) throw new FormatException("Invalid format: missing first separator.");

        // [ZERO-COPY SLICE 1]:
        // [KAISE CHALTA HAI]: Pointer arithmetic creates a new span view without allocating a new string on the heap!
        ReadOnlySpan<char> skuSpan = rawData.Slice(0, firstPipe);

        // [ZERO-COPY SLICE 2]:
        // Remaining characters after first pipe delimiter
        ReadOnlySpan<char> remainder = rawData.Slice(firstPipe + 1);

        // [INDEX LOOKUP 2]:
        int secondPipe = remainder.IndexOf('|');

        // [DEFENSIVE GUARD CLAUSE]:
        if (secondPipe == -1) throw new FormatException("Invalid format: missing second separator.");

        // [ZERO-COPY SLICE 3]: Price span
        ReadOnlySpan<char> priceSpan = remainder.Slice(0, secondPipe);

        // [ZERO-COPY SLICE 4]: Quantity span
        ReadOnlySpan<char> qtySpan = remainder.Slice(secondPipe + 1);

        // [ZERO-ALLOCATION NUMBER PARSING]:
        // .NET Core parses spans directly into decimal/int without string allocations!
        decimal price = decimal.Parse(priceSpan);
        int qty = int.Parse(qtySpan);

        // [RESULT RETURN]: Returns stack-allocated struct
        return new ParsedBarcode(skuSpan, price, qty);
    }
}
