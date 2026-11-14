namespace KiranaHub.ColumnarOlap.Compression;

/// <summary>
/// High-Performance Dictionary Compression Encoder for Columnar OLAP.
/// Maps repetitive string values (e.g. Category names, Brand names, Order Status)
/// to compact 1-byte integer IDs (0 to 255).
/// Reduces memory bandwidth by up to 30x (1 byte vs 32-byte string reference + heap object).
/// </summary>
public sealed class DictionaryEncoder
{
    // [KYA KARTA HAI]: String value se 1-byte dictionary ID ka lookup mapping store karta hai.
    // [KYU ZAROORI HAI]: Ingestion time par O(1) encoding speed achieve karna.
    // [KAISE CHALTA HAI]: Dictionary with StringComparer.Ordinal.
    // [OUTPUT / SIDE EFFECT]: In-memory encoding hash map.
    // [VARIATIONS TO TRY]: Case-insensitive string comparer.
    private readonly Dictionary<string, byte> _stringToId = new(StringComparer.Ordinal);

    // [KYA KARTA HAI]: 1-byte ID se original string ka reverse array store karta hai.
    // [KYU ZAROORI HAI]: Query output format aur aggregation decoding ke waqt O(1) instant lookup.
    // [KAISE CHALTA HAI]: List of strings indexed by byte ID.
    // [OUTPUT / SIDE EFFECT]: Fast reverse translation list.
    // [VARIATIONS TO TRY]: Fixed-size string[] array.
    private readonly List<string> _idToString = new();

    // [KYA KARTA HAI]: String ko 1-byte dictionary code me encode karta hai.
    // [KYU ZAROORI HAI]: Column chunk me string references ke bajay compact byte array store karna.
    // [KAISE CHALTA HAI]: Agar pehle se exist karta hai toh cached byte return karta hai, warna naya ID assign karta hai.
    // [OUTPUT / SIDE EFFECT]: 1-byte dictionary ID returned.
    // [VARIATIONS TO TRY]: ushort (16-bit) ID if unique strings exceed 256.
    public byte Encode(string value)
    {
        if (_stringToId.TryGetValue(value, out byte id))
        {
            return id;
        }

        if (_idToString.Count >= 256)
        {
            throw new InvalidOperationException("Dictionary capacity exceeded! Max 256 unique categories supported by 1-byte dictionary.");
        }

        byte newId = (byte)_idToString.Count;
        _stringToId[value] = newId;
        _idToString.Add(value);

        return newId;
    }

    // [KYA KARTA HAI]: 1-byte dictionary ID se original string decode karta hai.
    // [KYU ZAROORI HAI]: Analytical query results ko human-readable format me print karna.
    // [KAISE CHALTA HAI]: Direct list index lookup _idToString[id].
    // [OUTPUT / SIDE EFFECT]: Original string returned.
    // [VARIATIONS TO TRY]: Return ReadOnlySpan<char>.
    public string Decode(byte id)
    {
        if ((uint)id >= (uint)_idToString.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(id), $"Dictionary ID {id} not found.");
        }

        return _idToString[id];
    }

    // [KYA KARTA HAI]: Dictionary me total unique strings count property.
    // [KYU ZAROORI HAI]: Cardinality check aur statistics ke liye.
    // [KAISE CHALTA HAI]: List.Count getter.
    // [OUTPUT / SIDE EFFECT]: Unique elements count integer.
    // [VARIATIONS TO TRY]: Memory footprint estimation.
    public int Cardinality => _idToString.Count;

    // [KYA KARTA HAI]: Tamam registered dictionary entries expose karta hai.
    // [KYU ZAROORI HAI]: Metadata inspection aur serialization ke liye.
    // [KAISE CHALTA HAI]: Returns IReadOnlyList<string>.
    // [OUTPUT / SIDE EFFECT]: Read-only view of dictionary vocabulary.
    // [VARIATIONS TO TRY]: ReadOnlyMemory representation.
    public IReadOnlyList<string> Vocabulary => _idToString;
}
