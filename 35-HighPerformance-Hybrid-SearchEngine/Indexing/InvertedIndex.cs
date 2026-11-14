namespace KiranaHub.HybridSearch.Indexing;

using System.Collections.Concurrent;

// [KYA KARTA HAI]: High-performance Inverted Index data structure aur text tokenization pipeline implement karta hai.
// [KYU ZAROORI HAI]: Fast sub-millisecond full-text grocery catalog search ke liye terms se document IDs ka mapping maintain karna.
// [KAISE CHALTA HAI]: Product text tokenize karta hai, stop-words filter karta hai, case normalize karta hai, aur dictionary me `term -> List<PostingEntry>` index karta hai; saath hi document lengths aur corpus statistics calculate karta hai.
// [OUTPUT / SIDE EFFECT]: Inverted posting lists and document metadata.
// [VARIATIONS TO TRY]: Positional index compression with Elias-Gamma or Varint encoding.
public class InvertedIndex
{
    // [KYA KARTA HAI]: Inverted index mapping (Term -> List of PostingEntries).
    // [KYU ZAROORI HAI]: Diye gaye term ke matching documents quickly retrieve karna.
    // [KAISE CHALTA HAI]: ConcurrentDictionary<string, List<PostingEntry>>.
    // [OUTPUT / SIDE EFFECT]: Posting lists storage.
    // [VARIATIONS TO TRY]: ImmutableArray for read-heavy workloads.
    private readonly ConcurrentDictionary<string, List<PostingEntry>> _index = new(StringComparer.OrdinalIgnoreCase);

    // [KYA KARTA HAI]: Indexed documents ka metadata store karta hai (DocId -> DocumentDetails).
    // [KYU ZAROORI HAI]: Document title, category, price, aur total token length access karna.
    // [KAISE CHALTA HAI]: ConcurrentDictionary<string, IndexedDocument>.
    // [OUTPUT / SIDE EFFECT]: Document store.
    // [VARIATIONS TO TRY]: External document database reference.
    private readonly ConcurrentDictionary<string, IndexedDocument> _documents = new(StringComparer.OrdinalIgnoreCase);

    // [KYA KARTA HAI]: English aur common query stop-words ka HashSet.
    // [KYU ZAROORI HAI]: "the", "and", "is", "of" jaise non-discriminative terms ko index bloating se rokna.
    // [KAISE CHALTA HAI]: HashSet<string> lookup in O(1).
    // [OUTPUT / SIDE EFFECT]: Filter set.
    // [VARIATIONS TO TRY]: Hinglish stop words ("ka", "ki", "ke", "hai") add karein.
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "a", "an", "the", "and", "or", "in", "on", "at", "to", "for", "of", "with", "is", "it", "by"
    };

    // [KYA KARTA HAI]: Total indexed documents count return karta hai ($N$).
    // [KYU ZAROORI HAI]: BM25 Inverse Document Frequency (IDF) formula me total corpus size zaroori hota hai.
    // [KAISE CHALTA HAI]: _documents.Count property.
    // [OUTPUT / SIDE EFFECT]: Total documents integer.
    // [VARIATIONS TO TRY]: Dynamic shard count aggregation.
    public int TotalDocuments => _documents.Count;

    // [KYA KARTA HAI]: Sabhi documents ki average token length return karta hai ($\text{avgdl}$).
    // [KYU ZAROORI HAI]: BM25 me document length normalization parameter ($b$) ke liye denominator provide karna.
    // [KAISE CHALTA HAI]: Total tokens divided by total documents.
    // [OUTPUT / SIDE EFFECT]: Double average length.
    // [VARIATIONS TO TRY]: Moving average calculation on document insertions.
    public double AverageDocumentLength => TotalDocuments == 0 ? 0 : _documents.Values.Average(d => d.TokenLength);

    // [KYA KARTA HAI]: Catalog document ko tokenize karke inverted index me add karta hai.
    // [KYU ZAROORI HAI]: Product search discovery enable karna.
    // [KAISE CHALTA HAI]: Content ko Tokenize() se गुजarta hai, term frequencies count karta hai, aur har unique term ke posting list me document ID aur frequency append karta hai.
    // [OUTPUT / SIDE EFFECT]: Document indexed, vocabulary expanded.
    // [VARIATIONS TO TRY]: Field boosting (e.g. title tokens get 3x frequency weight over description tokens).
    public void IndexDocument(string id, string title, string description, string category, decimal price)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        // [KYA KARTA HAI]: Full text combine karke tokens generate karta hai.
        // [KYU ZAROORI HAI]: Title aur description dono se search keyword extraction.
        // [KAISE CHALTA HAI]: Tokenize($"{title} {description} {category}").
        // [OUTPUT / SIDE EFFECT]: Token string array.
        // [VARIATIONS TO TRY]: N-gram tokenizer for character level fuzzy indexing.
        var tokens = Tokenize($"{title} {description} {category}");
        int tokenLength = tokens.Count;

        // [KYA KARTA HAI]: Document metadata record karta hai.
        // [KYU ZAROORI HAI]: Storage in document catalog.
        // [KAISE CHALTA HAI]: new IndexedDocument.
        // [OUTPUT / SIDE EFFECT]: Document cached.
        // [VARIATIONS TO TRY]: Pre-computed vector embeddings attachment.
        var doc = new IndexedDocument(id, title, description, category, price, tokenLength);
        _documents[id] = doc;

        // [KYA KARTA HAI]: Document-level term frequencies count karta hai.
        // [KYU ZAROORI HAI]: Calculate $f(t, D)$ for BM25.
        // [KAISE CHALTA HAI]: GroupBy token counting.
        // [OUTPUT / SIDE EFFECT]: Dictionary<string, int>.
        // [VARIATIONS TO TRY]: Arraypool dictionary.
        var termFreqs = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var t in tokens)
        {
            termFreqs[t] = termFreqs.GetValueOrDefault(t, 0) + 1;
        }

        // [KYA KARTA HAI]: Inverted index me posting entries append karta hai.
        // [KYU ZAROORI HAI]: Term to document mapping.
        // [KAISE CHALTA HAI]: For each term, add PostingEntry(id, frequency).
        // [OUTPUT / SIDE EFFECT]: Inverted posting list updated.
        // [VARIATIONS TO TRY]: In-place sorted posting list insertion by docId.
        foreach (var (term, freq) in termFreqs)
        {
            var postings = _index.GetOrAdd(term, _ => new List<PostingEntry>());
            lock (postings)
            {
                postings.Add(new PostingEntry(id, freq));
            }
        }
    }

    // [KYA KARTA HAI]: Text string ko normalized tokens me split karta hai.
    // [KYU ZAROORI HAI]: Punctuation strip karna, lowercase karna, aur stop-words remove karna.
    // [KAISE CHALTA HAI]: Char-by-char traversal ya string split on whitespace and punctuation.
    // [OUTPUT / SIDE EFFECT]: Cleaned string token list.
    // [VARIATIONS TO TRY]: Porter Stemmer algorithm shamil karein.
    public static List<string> Tokenize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new List<string>();
        }

        // [KYA KARTA HAI]: Split on non-alphanumeric characters.
        // [KYU ZAROORI HAI]: Remove punctuation (, . - / !).
        // [KAISE CHALTA HAI]: char.IsLetterOrDigit inspection.
        // [OUTPUT / SIDE EFFECT]: Filtered word list.
        // [VARIATIONS TO TRY]: Regex tokenization.
        var words = text.ToLowerInvariant()
            .Split(new[] { ' ', '\t', '\r', '\n', ',', '.', ';', ':', '-', '/', '(', ')', '[', ']', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);

        var tokens = new List<string>();
        foreach (var word in words)
        {
            var cleaned = word.Trim();
            // [KYA KARTA HAI]: Stop words aur single char tokens filter karta hai.
            // [KYU ZAROORI HAI]: Index quality maintain karna.
            // [KAISE CHALTA HAI]: StopWords.Contains check.
            // [OUTPUT / SIDE EFFECT]: High-value tokens only.
            // [VARIATIONS TO TRY]: Custom domain whitelist.
            if (cleaned.Length > 1 && !StopWords.Contains(cleaned))
            {
                tokens.Add(cleaned);
            }
        }

        return tokens;
    }

    // [KYA KARTA HAI]: Term ke liye posting list return karta hai.
    // [KYU ZAROORI HAI]: Query matching documents dhoondna.
    // [KAISE CHALTA HAI]: TryGetValue lookup on _index.
    // [OUTPUT / SIDE EFFECT]: ReadOnlyList of PostingEntry.
    // [VARIATIONS TO TRY]: Skip pointers for fast list intersections.
    public IReadOnlyList<PostingEntry> GetPostings(string term)
    {
        if (_index.TryGetValue(term, out var postings))
        {
            lock (postings)
            {
                return postings.ToList().AsReadOnly();
            }
        }
        return Array.Empty<PostingEntry>();
    }

    // [KYA KARTA HAI]: Document details return karta hai by ID.
    // [KYU ZAROORI HAI]: Search result metadata display karna.
    // [KAISE CHALTA HAI]: TryGetValue on _documents.
    // [OUTPUT / SIDE EFFECT]: IndexedDocument or null.
    // [VARIATIONS TO TRY]: Throw KeyNotFoundException.
    public IndexedDocument? GetDocument(string id) => _documents.TryGetValue(id, out var doc) ? doc : null;

    // [KYA KARTA HAI]: Sabhi unique vocabulary terms return karta hai.
    // [KYU ZAROORI HAI]: Fuzzy Levenshtein spell checking ke liye lexicon lookup.
    // [KAISE CHALTA HAI]: _index.Keys snapshot.
    // [OUTPUT / SIDE EFFECT]: Unique term collection.
    // [VARIATIONS TO TRY]: Trie traversal.
    public IEnumerable<string> GetVocabulary() => _index.Keys;

    // [KYA KARTA HAI]: Sabhi indexed documents return karta hai.
    // [KYU ZAROORI HAI]: Vector search iteration and corpus scans.
    // [KAISE CHALTA HAI]: _documents.Values.
    // [OUTPUT / SIDE EFFECT]: All documents.
    // [VARIATIONS TO TRY]: Paged iterator.
    public IEnumerable<IndexedDocument> GetAllDocuments() => _documents.Values;
}

// [KYA KARTA HAI]: Inverted index posting entry record (DocumentId aur TermFrequency).
// [KYU ZAROORI HAI]: Track karna ki specific document me word kitni baar aaya.
// [KAISE CHALTA HAI]: Record instantiation.
// [OUTPUT / SIDE EFFECT]: Posting entry.
// [VARIATIONS TO TRY]: Term positions array shamil karein for exact phrase match.
public record PostingEntry(string DocumentId, int TermFrequency);

// [KYA KARTA HAI]: Indexed grocery document metadata record.
// [KYU ZAROORI HAI]: Product information encapsulation.
// [KAISE CHALTA HAI]: Record instantiation.
// [OUTPUT / SIDE EFFECT]: Document model.
// [VARIATIONS TO TRY]: Image URL aur stock count fields.
public record IndexedDocument(
    string Id,
    string Title,
    string Description,
    string Category,
    decimal Price,
    int TokenLength);
