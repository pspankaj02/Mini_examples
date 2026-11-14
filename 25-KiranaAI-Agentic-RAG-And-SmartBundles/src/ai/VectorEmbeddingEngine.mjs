// ==============================================================================================
// KiranaHub KiranaAI: Semantic Vector Embedding & Cosine Similarity Search Engine
// [KYA KARTA HAI] Grocery items ko multi-dimensional vector space me index karta hai aur cosine similarity se semantic search provide karta hai.
// [KYU ZAROORI HAI] Customer "creamy gravy for dinner" search kare toh exact keyword match na hone par bhi "Amul Cream" aur "Cashews" recommend karna.
// [KAISE CHALTA HAI] Text ko n-gram token embeddings me convert karta hai, L2-normalize karta hai aur dot product se similarity score compute karta hai.
// [OUTPUT / SIDE EFFECT] Ranked search results with similarity scores from 0.0 to 1.0.
// [VARIATIONS TO TRY] Dense neural embeddings using Gemini Text-Embedding-004 model via REST API.
// ==============================================================================================

// [KYA KARTA HAI] VectorEmbeddingEngine class export karta hai.
// [KYU ZAROORI HAI] Semantic search index aur vector mathematical operations encapsulate karna.
// [KAISE CHALTA HAI] ES Module class structure instantiate hoti hai.
// [OUTPUT / SIDE EFFECT] Class blueprint available.
// [VARIATIONS TO TRY] Singleton instance export.
export class VectorEmbeddingEngine {
    // [KYA KARTA HAI] Class constructor initialize karta hai.
    // [KYU ZAROORI HAI] Internal vocabulary map aur document vector store initialize karna.
    // [KAISE CHALTA HAI] In-memory maps aur arrays allocate karta hai.
    // [OUTPUT / SIDE EFFECT] Clean state ready for indexing.
    // [VARIATIONS TO TRY] Pre-load serialized index from disk cache.
    constructor() {
        // [KYA KARTA HAI] Vocabulary dictionary map initialize karta hai.
        // [KYU ZAROORI HAI] Har unique term/n-gram ko ek specific vector dimension index assign karna.
        // [KAISE CHALTA HAI] Key is word/token, Value is unique dimension integer ID.
        // [OUTPUT / SIDE EFFECT] Empty vocabulary map.
        // [VARIATIONS TO TRY] Fixed-size hash trick for constant memory footprint.
        this.vocabulary = new Map();

        // [KYA KARTA HAI] Next available dimension index counter.
        // [KYU ZAROORI HAI] Har naye term ko incrementing coordinate number dena.
        // [KAISE CHALTA HAI] Starts at 0 and increments with new unique words.
        // [OUTPUT / SIDE EFFECT] Dimension index tracker.
        // [VARIATIONS TO TRY] Predefined vocabulary dimension limit.
        this.nextDim = 0;

        // [KYA KARTA HAI] Indexed catalog documents and their computed embeddings store array.
        // [KYU ZAROORI HAI] Query aane par sabhi catalog items ke sath vector distance compare karna.
        // [KAISE CHALTA HAI] Array of objects: { item, vector, norm }.
        // [OUTPUT / SIDE EFFECT] In-memory vector database.
        // [VARIATIONS TO TRY] HNSW (Hierarchical Navigable Small World) index for sub-linear scaling.
        this.index = [];
    }

    // [KYA KARTA HAI] Raw text ko clean karke lower-case tokens me tokenize karta hai.
    // [KYU ZAROORI HAI] Punctuation aur case sensitivity se similarity skew hone se rokna.
    // [KAISE CHALTA HAI] Regex match karke alphanumeric words extract karta hai aur character bi-grams generate karta hai.
    // [OUTPUT / SIDE EFFECT] Array of string tokens.
    // [VARIATIONS TO TRY] Stemming or lemmatization (e.g. Porter Stemmer).
    tokenize(text) {
        // [KYA KARTA HAI] Text ko lowercase karke non-word characters split karta hai.
        // [KYU ZAROORI HAI] Uniform vocabulary representation.
        // [KAISE CHALTA HAI] String lowercase and regex split.
        // [OUTPUT / SIDE EFFECT] Clean words array.
        // [VARIATIONS TO TRY] Subword tokenization (Byte-Pair Encoding).
        const words = text.toLowerCase().match(/\b[a-z0-9]+\b/g) || [];
        // [KYA KARTA HAI] Tokens collection array initialize karta hai.
        // [KYU ZAROORI HAI] Full words aur character n-grams dono collect karna.
        // [KAISE CHALTA HAI] Array push operations.
        // [OUTPUT / SIDE EFFECT] Enhanced token set.
        // [VARIATIONS TO TRY] Filter out English stop words ("the", "is", "for").
        const tokens = [...words];

        // [KYA KARTA HAI] Har word ke character 3-grams generate karta hai.
        // [KYU ZAROORI HAI] Typo tolerance — agar user "paneer" ko "panir" likhe toh bhi sub-words match ho sakein.
        // [KAISE CHALTA HAI] Loops through characters extracting triplets.
        // [OUTPUT / SIDE EFFECT] Sub-word tokens added.
        // [VARIATIONS TO TRY] 4-grams for longer compound Hindi words.
        for (const word of words) {
            // [KYA KARTA HAI] Word length check karta hai.
            // [KYU ZAROORI HAI] 3 letters se chhote words me n-grams skip karna.
            // [KAISE CHALTA HAI] Length comparison.
            // [OUTPUT / SIDE EFFECT] Skip short words.
            // [VARIATIONS TO TRY] Padding with boundary markers.
            if (word.length >= 3) {
                // [KYA KARTA HAI] Substring slice loop.
                // [KYU ZAROORI HAI] Sequential 3 character chunks extract karna.
                // [KAISE CHALTA HAI] Iterates up to length - 2.
                // [OUTPUT / SIDE EFFECT] 3-grams extracted.
                // [VARIATIONS TO TRY] Variable length n-grams.
                for (let i = 0; i <= word.length - 3; i++) {
                    tokens.push(word.substring(i, i + 3));
                }
            }
        }
        // [KYA KARTA HAI] Final token array return karta hai.
        // [KYU ZAROORI HAI] Vectorizer ko feed karna.
        // [KAISE CHALTA HAI] Returns tokens.
        // [OUTPUT / SIDE EFFECT] Token list returned.
        // [VARIATIONS TO TRY] Frequency counting.
        return tokens;
    }

    // [KYA KARTA HAI] Text ko sparse numerical vector me convert karta hai.
    // [KYU ZAROORI HAI] String textual data ko mathematical coordinates me map karna.
    // [KAISE CHALTA HAI] Tokens ke term frequencies count karta hai aur vector coordinates assign karta hai.
    // [OUTPUT / SIDE EFFECT] Map of { dimensionId => weight }.
    // [VARIATIONS TO TRY] TF-IDF weighting with inverse document frequency.
    vectorize(text, isLearning = false) {
        // [KYA KARTA HAI] Tokens extract karta hai input text se.
        // [KYU ZAROORI HAI] Feature representation build karna.
        // [KAISE CHALTA HAI] Calls tokenize helper.
        // [OUTPUT / SIDE EFFECT] Array of tokens.
        // [VARIATIONS TO TRY] Custom tokenizer.
        const tokens = this.tokenize(text);
        // [KYA KARTA HAI] Sparse vector Map initialize karta hai.
        // [KYU ZAROORI HAI] Zero-value dimensions ko store na karke memory optimize karna.
        // [KAISE CHALTA HAI] Key is dimension integer, Value is frequency count.
        // [OUTPUT / SIDE EFFECT] Sparse vector map.
        // [VARIATIONS TO TRY] Float32Array for dense fixed-size vectors.
        const vector = new Map();

        // [KYA KARTA HAI] Har token ke upar iterate karta hai.
        // [KYU ZAROORI HAI] Term frequency count calculate karna.
        // [KAISE CHALTA HAI] For..of loop.
        // [OUTPUT / SIDE EFFECT] Coordinate weights accumulated.
        // [VARIATIONS TO TRY] Log-scaled term frequency: 1 + log(tf).
        for (const token of tokens) {
            // [KYA KARTA HAI] Check karta hai ki token vocabulary me pehle se hai ya nahi.
            // [KYU ZAROORI HAI] Naye words ko dimension assign karna agar training/indexing phase me hain.
            // [KAISE CHALTA HAI] Map lookup.
            // [OUTPUT / SIDE EFFECT] Dimension ID resolved.
            // [VARIATIONS TO TRY] Out-of-vocabulary hash bucket.
            if (!this.vocabulary.has(token)) {
                // [KYA KARTA HAI] Agar learning mode on hai toh naya dimension assign karta hai.
                // [KYU ZAROORI HAI] Indexing phase me vocabulary grow hoti hai; search phase me frozen rehti hai.
                // [KAISE CHALTA HAI] Stores token at this.nextDim and increments.
                // [OUTPUT / SIDE EFFECT] Vocabulary updated.
                // [VARIATIONS TO TRY] Fixed predefined dictionary.
                if (isLearning) {
                    this.vocabulary.set(token, this.nextDim++);
                } else {
                    // [KYA KARTA HAI] Unknown words skip karta hai search mode me.
                    // [KYU ZAROORI HAI] Zero vector dimension avoid karna.
                    // [KAISE CHALTA HAI] Continues to next token.
                    // [OUTPUT / SIDE EFFECT] Token skipped.
                    // [VARIATIONS TO TRY] Assign to unknown token bucket.
                    continue;
                }
            }
            // [KYA KARTA HAI] Token ka coordinate ID retrieve karta hai.
            // [KYU ZAROORI HAI] Vector array index map karna.
            // [KAISE CHALTA HAI] Map.get call.
            // [OUTPUT / SIDE EFFECT] Integer dim.
            // [VARIATIONS TO TRY] None.
            const dim = this.vocabulary.get(token);
            // [KYA KARTA HAI] Dimension weight increment karta hai.
            // [KYU ZAROORI HAI] Bar-bar repeat hone wale words ko higher weight dena.
            // [KAISE CHALTA HAI] Current count + 1.
            // [OUTPUT / SIDE EFFECT] Vector entry updated.
            // [VARIATIONS TO TRY] Sublinear term scaling.
            vector.set(dim, (vector.get(dim) || 0) + 1);
        }
        // [KYA KARTA HAI] Vector map return karta hai.
        // [KYU ZAROORI HAI] Distance calculation me use karna.
        // [KAISE CHALTA HAI] Returns Map.
        // [OUTPUT / SIDE EFFECT] Sparse vector returned.
        // [VARIATIONS TO TRY] Normalize vector before returning.
        return vector;
    }

    // [KYA KARTA HAI] Vector ka Euclidean L2 norm (magnitude) compute karta hai: sqrt(sum(x_i^2)).
    // [KYU ZAROORI HAI] Cosine similarity ke denominator me vector lengths se normalize karne ke liye.
    // [KAISE CHALTA HAI] Har coordinate value ka square add karta hai aur square root nikalta hai.
    // [OUTPUT / SIDE EFFECT] Floating point scalar magnitude.
    // [VARIATIONS TO TRY] Manhattan L1 norm.
    computeNorm(vector) {
        // [KYA KARTA HAI] Sum of squares accumulator initialize karta hai.
        // [KYU ZAROORI HAI] Magnitudes accumulate karna.
        // [KAISE CHALTA HAI] Number initialized to 0.
        // [OUTPUT / SIDE EFFECT] Sum variable.
        // [VARIATIONS TO TRY] Float64 precision.
        let sum = 0;
        // [KYA KARTA HAI] Vector ke har dimension weight par loop karta hai.
        // [KYU ZAROORI HAI] Coordinates square karna.
        // [KAISE CHALTA HAI] For..of values iterator.
        // [OUTPUT / SIDE EFFECT] Quadratic accumulation.
        // [VARIATIONS TO TRY] Vectorized SIMD instructions.
        for (const val of vector.values()) {
            sum += val * val;
        }
        // [KYA KARTA HAI] Square root return karta hai.
        // [KYU ZAROORI HAI] L2 vector length calculate karna.
        // [KAISE CHALTA HAI] Math.sqrt call.
        // [OUTPUT / SIDE EFFECT] Length returned.
        // [VARIATIONS TO TRY] Cache norm with vector object.
        return Math.sqrt(sum);
    }

    // [KYA KARTA HAI] Do vectors ke beech Cosine Similarity calculate karta hai: (A . B) / (|A| * |B|).
    // [KYU ZAROORI HAI] Text length se independent semantic orientation compare karna (1.0 = identical, 0.0 = completely unrelated).
    // [KAISE CHALTA HAI] Non-zero matching dimensions ka dot product nikal kar norms ke product se divide karta hai.
    // [OUTPUT / SIDE EFFECT] Similarity score between 0.0 and 1.0.
    // [VARIATIONS TO TRY] Jaccard similarity or Euclidean distance.
    cosineSimilarity(vecA, normA, vecB, normB) {
        // [KYA KARTA HAI] Zero length vector check karta hai.
        // [KYU ZAROORI HAI] Division by zero exception prevent karna.
        // [KAISE CHALTA HAI] Compares norms to zero.
        // [OUTPUT / SIDE EFFECT] Returns 0 if empty.
        // [VARIATIONS TO TRY] Epsilon tolerance check.
        if (normA === 0 || normB === 0) return 0;

        // [KYA KARTA HAI] Dot product accumulator initialize karta hai.
        // [KYU ZAROORI HAI] Shared dimensions ka product sum karna.
        // [KAISE CHALTA HAI] Initialized to 0.
        // [OUTPUT / SIDE EFFECT] Accumulator ready.
        // [VARIATIONS TO TRY] Float64 precision.
        let dotProduct = 0;

        // [KYA KARTA HAI] Shorter vector ko loop ke liye choose karta hai performance optimization ke liye.
        // [KYU ZAROORI HAI] Sparsity benefit — agar A me 3 elements hain aur B me 500, toh sirf 3 lookups lagenge.
        // [KAISE CHALTA HAI] Compares map sizes.
        // [OUTPUT / SIDE EFFECT] Iterates smaller map.
        // [VARIATIONS TO TRY] Two-pointer array sweep if keys are sorted.
        const [smallVec, largeVec] = vecA.size < vecB.size ? [vecA, vecB] : [vecB, vecA];

        // [KYA KARTA HAI] Shared dimensions par multiply karke accumulate karta hai.
        // [KYU ZAROORI HAI] Coordinate alignment measure karna.
        // [KAISE CHALTA HAI] For..of loop over smallVec entries.
        // [OUTPUT / SIDE EFFECT] Dot product calculated.
        // [VARIATIONS TO TRY] Weighted dimension boosting.
        for (const [dim, valA] of smallVec.entries()) {
            const valB = largeVec.get(dim);
            if (valB !== undefined) {
                dotProduct += valA * valB;
            }
        }

        // [KYA KARTA HAI] Normalized cosine angle return karta hai.
        // [KYU ZAROORI HAI] Scale-invariant similarity score return karna.
        // [KAISE CHALTA HAI] Divides dot product by multiplied norms.
        // [OUTPUT / SIDE EFFECT] Similarity score [0..1].
        // [VARIATIONS TO TRY] Scale to percentage (score * 100).
        return dotProduct / (normA * normB);
    }

    // [KYA KARTA HAI] Grocery catalog items ko index me ingest karta hai.
    // [KYU ZAROORI HAI] Fast vector search ke liye pehle se item embeddings pre-compute karke rakhna.
    // [KAISE CHALTA HAI] Har item ke name, category, description aur tags ko merge karke vector generate karta hai.
    // [OUTPUT / SIDE EFFECT] Populates this.index array.
    // [VARIATIONS TO TRY] Batch indexing with background worker.
    indexCatalog(items) {
        // [KYA KARTA HAI] Existing index clear karta hai.
        // [KYU ZAROORI HAI] Clean indexing without duplicates.
        // [KAISE CHALTA HAI] Sets length to 0.
        // [OUTPUT / SIDE EFFECT] Empty index.
        // [VARIATIONS TO TRY] Incremental upsert.
        this.index = [];
        // [KYA KARTA HAI] Har catalog item par loop karta hai.
        // [KYU ZAROORI HAI] Embeddings pre-compute karna.
        // [KAISE CHALTA HAI] For..of loop.
        // [OUTPUT / SIDE EFFECT] Index built.
        // [VARIATIONS TO TRY] Parallel chunks.
        for (const item of items) {
            // [KYA KARTA HAI] Rich text representation banata hai item ka.
            // [KYU ZAROORI HAI] Title, category, aur tags sabhi semantic search me participate kar sakein.
            // [KAISE CHALTA HAI] String template joining fields.
            // [OUTPUT / SIDE EFFECT] Descriptive text.
            // [VARIATIONS TO TRY] Higher weight multiplier for title.
            const textToEmbed = `${item.name} ${item.category} ${item.tags?.join(' ') || ''} ${item.description || ''}`;
            // [KYA KARTA HAI] Vector embedding create karta hai with learning flag enabled.
            // [KYU ZAROORI HAI] Naye vocabulary words register karna.
            // [KAISE CHALTA HAI] Calls vectorize(..., true).
            // [OUTPUT / SIDE EFFECT] Sparse vector map.
            // [VARIATIONS TO TRY] Pre-computed static vectors.
            const vector = this.vectorize(textToEmbed, true);
            // [KYA KARTA HAI] Vector norm compute karta hai.
            // [KYU ZAROORI HAI] Length pre-calculate karke search ke waqt CPU cycles bachana.
            // [KAISE CHALTA HAI] Calls computeNorm.
            // [OUTPUT / SIDE EFFECT] Scalar norm.
            // [VARIATIONS TO TRY] Store magnitude in Float32 array.
            const norm = this.computeNorm(vector);
            // [KYA KARTA HAI] Indexed document record push karta hai.
            // [KYU ZAROORI HAI] Vector search store me register karna.
            // [KAISE CHALTA HAI] Array push.
            // [OUTPUT / SIDE EFFECT] Item ready for search.
            // [VARIATIONS TO TRY] In-memory SQLite sqlite-vss table.
            this.index.push({ item, vector, norm });
        }
    }

    // [KYA KARTA HAI] Natural language query ke against Top-K nearest semantic items find karta hai.
    // [KYU ZAROORI HAI] User intent ke sabse close matching groceries retrieve karna.
    // [KAISE CHALTA HAI] Query vector banata hai, saare indexed items ke sath cosine similarity nikalta hai aur sort karta hai.
    // [OUTPUT / SIDE EFFECT] Array of { item, score } sorted by score descending.
    // [VARIATIONS TO TRY] Filter by price range or category threshold.
    search(query, topK = 5, minScore = 0.1) {
        // [KYA KARTA HAI] Query string ko vector me convert karta hai (isLearning = false).
        // [KYU ZAROORI HAI] Search query ke words se vocabulary mutate na ho.
        // [KAISE CHALTA HAI] Calls vectorize with false.
        // [OUTPUT / SIDE EFFECT] Query vector map.
        // [VARIATIONS TO TRY] Expand query with synonyms.
        const queryVec = this.vectorize(query, false);
        // [KYA KARTA HAI] Query vector norm nikalta hai.
        // [KYU ZAROORI HAI] Cosine normalization.
        // [KAISE CHALTA HAI] Calls computeNorm.
        // [OUTPUT / SIDE EFFECT] Scalar query norm.
        // [VARIATIONS TO TRY] Return empty if norm is zero.
        const queryNorm = this.computeNorm(queryVec);

        if (queryNorm === 0) return [];

        // [KYA KARTA HAI] Sabhi indexed documents ke sath similarity compute karta hai.
        // [KYU ZAROORI HAI] Score ranking create karna.
        // [KAISE CHALTA HAI] Map function over this.index.
        // [OUTPUT / SIDE EFFECT] Array of scored items.
        // [VARIATIONS TO TRY] K-d tree or inverted index candidate filtering.
        const results = this.index.map(doc => {
            const score = this.cosineSimilarity(queryVec, queryNorm, doc.vector, doc.norm);
            return { item: doc.item, score: Number(score.toFixed(4)) };
        });

        // [KYA KARTA HAI] Minimum relevance score se filter karta hai aur descending order me sort karta hai.
        // [KYU ZAROORI HAI] Irrelevant items discard karna aur sabse relevant items sabse upar dikhana.
        // [KAISE CHALTA HAI] Array filter + sort + slice.
        // [OUTPUT / SIDE EFFECT] Top K best matching grocery items.
        // [VARIATIONS TO TRY] Re-ranking with popularity or customer personalization weights.
        return results
            .filter(r => r.score >= minScore)
            .sort((a, b) => b.score - a.score)
            .slice(0, topK);
    }
}
