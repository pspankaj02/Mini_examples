// ==============================================================================================
// KiranaHub KiranaAI: Autonomous Recipe-to-Cart Agentic Assistant
// [KYA KARTA HAI] Customer ke natural language recipe request ko ingredient parsing, semantic search, dark-store stock checking, aur 1-click discounted smart cart bundle me convert karta hai.
// [KYU ZAROORI HAI] Quick-commerce me customer ko 10 alag masale aur sabziyan manually dhoondne ke jhanjhat se bacha kar average order value (AOV) 40% badhana.
// [KAISE CHALTA HAI] Recipe decomposition -> RAG Vector search -> Stock checking -> Automatic out-of-stock substitution -> 1-click bundle generation.
// [OUTPUT / SIDE EFFECT] Complete actionable smart bundle ready for customer checkout.
// [VARIATIONS TO TRY] Allergen filtering (gluten-free, nut-free, vegan) based on user profile.
// ==============================================================================================

// [KYA KARTA HAI] VectorEmbeddingEngine import karta hai.
// [KYU ZAROORI HAI] Ingredients ki semantic searching ke liye.
// [KAISE CHALTA HAI] ES module import.
// [OUTPUT / SIDE EFFECT] Vector engine available.
// [VARIATIONS TO TRY] Remote Vector DB client (Pinecone, Qdrant).
import { VectorEmbeddingEngine } from './VectorEmbeddingEngine.mjs';

// [KYA KARTA HAI] AgenticToolRegistry import karta hai.
// [KYU ZAROORI HAI] Function calling tools configure karne ke liye.
// [KAISE CHALTA HAI] ES module import.
// [OUTPUT / SIDE EFFECT] Tool registry available.
// [VARIATIONS TO TRY] MCP Client integration.
import { AgenticToolRegistry } from './AgenticToolRegistry.mjs';

// [KYA KARTA HAI] RecipeToCartAssistant class export karta hai.
// [KYU ZAROORI HAI] Multi-turn agentic workflow encapsulate karna.
// [KAISE CHALTA HAI] Class structure manages knowledge base, inventory and agent execution loop.
// [OUTPUT / SIDE EFFECT] Assistant blueprint available.
// [VARIATIONS TO TRY] Stateful conversation session caching.
export class RecipeToCartAssistant {
    // [KYA KARTA HAI] Assistant constructor initialize karta hai.
    // [KYU ZAROORI HAI] Catalog, dark store inventory database, vector index aur tools register karna.
    // [KAISE CHALTA HAI] Sets up internal vector engine and binds tool implementations.
    // [OUTPUT / SIDE EFFECT] Ready agent instance.
    // [VARIATIONS TO TRY] Dependency injection of external repository services.
    constructor(catalogItems, darkStoreInventory) {
        // [KYA KARTA HAI] Master product catalog list store karta hai.
        // [KYU ZAROORI HAI] Product metadata, SKU IDs, prices access karne ke liye.
        // [KAISE CHALTA HAI] Assigns items array.
        // [OUTPUT / SIDE EFFECT] Catalog stored.
        // [VARIATIONS TO TRY] Read from database.
        this.catalog = catalogItems;

        // [KYA KARTA HAI] Dark store inventory map store karta hai: { storeId => { sku => stockCount } }.
        // [KYU ZAROORI HAI] Real-time stock availability check karna.
        // [KAISE CHALTA HAI] Stores inventory nested map.
        // [OUTPUT / SIDE EFFECT] Live inventory available.
        // [VARIATIONS TO TRY] Redis distributed cache lookup.
        this.inventory = darkStoreInventory;

        // [KYA KARTA HAI] VectorEmbeddingEngine instantiate karta hai.
        // [KYU ZAROORI HAI] Semantic RAG retrieval ke liye.
        // [KAISE CHALTA HAI] Creates engine instance and builds index.
        // [OUTPUT / SIDE EFFECT] Vector index initialized.
        // [VARIATIONS TO TRY] Pre-computed vector embeddings.
        this.vectorEngine = new VectorEmbeddingEngine();
        this.vectorEngine.indexCatalog(catalogItems);

        // [KYA KARTA HAI] AgenticToolRegistry instantiate karta hai.
        // [KYU ZAROORI HAI] Tools configure karke AI loop ko delegate karna.
        // [KAISE CHALTA HAI] Creates tool registry and registers tools.
        // [OUTPUT / SIDE EFFECT] Tools ready.
        // [VARIATIONS TO TRY] Dynamic tool permissions.
        this.toolRegistry = new AgenticToolRegistry();
        this._setupTools();

        // [KYA KARTA HAI] Recipe knowledge base dictionary initialize karta hai.
        // [KYU ZAROORI HAI] Popular Indian recipes ke base ingredients aur per-person portion scaling ratios provide karna.
        // [KAISE CHALTA HAI] Key is recipe slug, value is array of ingredients with unit quantities.
        // [OUTPUT / SIDE EFFECT] Recipe knowledge base loaded.
        // [VARIATIONS TO TRY] External Chef API or Gemini multimodal recipe extraction.
        this.recipeKB = {
            'shahi-paneer': [
                { query: 'fresh paneer', baseQty: 100, unit: 'g', perPersons: 2 },
                { query: 'fresh cream malai', baseQty: 50, unit: 'ml', perPersons: 2 },
                { query: 'whole cashews kaju', baseQty: 30, unit: 'g', perPersons: 2 },
                { query: 'fresh hybrid tomatoes', baseQty: 150, unit: 'g', perPersons: 2 },
                { query: 'shahi paneer masala spice mix', baseQty: 1, unit: 'pack', perPersons: 4 }
            ],
            'masala-chai': [
                { query: 'assam tea leaves', baseQty: 20, unit: 'g', perPersons: 2 },
                { query: 'full cream milk', baseQty: 250, unit: 'ml', perPersons: 2 },
                { query: 'fresh ginger adrak', baseQty: 20, unit: 'g', perPersons: 2 },
                { query: 'green cardamom elaichi', baseQty: 5, unit: 'g', perPersons: 2 }
            ]
        };
    }

    // [KYA KARTA HAI] Tools ko registry me define aur bind karta hai.
    // [KYU ZAROORI HAI] LLM tool declarations aur backend handlers ka mapping setup karna.
    // [KAISE CHALTA HAI] Calls toolRegistry.registerTool for each action.
    // [OUTPUT / SIDE EFFECT] 3 standard tools registered.
    // [VARIATIONS TO TRY] Add refund_order or track_rider tools.
    _setupTools() {
        // [KYA KARTA HAI] Tool 1: search_catalog registration.
        // [KYU ZAROORI HAI] Agent ko natural language me products search karne ki capability dena.
        // [KAISE CHALTA HAI] Uses vector engine cosine similarity.
        // [OUTPUT / SIDE EFFECT] Search capability registered.
        // [VARIATIONS TO TRY] Add price filter.
        this.toolRegistry.registerTool(
            {
                name: 'search_catalog',
                description: 'Search KiranaHub grocery catalog using semantic vector search',
                parameters: {
                    type: 'object',
                    properties: {
                        query: { type: 'string', description: 'Product search term or intent' },
                        topK: { type: 'number', description: 'Number of results to return' }
                    },
                    required: ['query']
                }
            },
            async (args) => {
                const results = this.vectorEngine.search(args.query, args.topK || 3);
                return results.map(r => ({
                    sku: r.item.sku,
                    name: r.item.name,
                    price: r.item.price,
                    score: r.score
                }));
            }
        );

        // [KYA KARTA HAI] Tool 2: check_darkstore_stock registration.
        // [KYU ZAROORI HAI] Agent verify kar sake ki customer ke dark store me items actually in-stock hain ya nahi.
        // [KAISE CHALTA HAI] Queries dark store inventory map.
        // [OUTPUT / SIDE EFFECT] Stock levels returned.
        // [VARIATIONS TO TRY] Check multi-store fallback.
        this.toolRegistry.registerTool(
            {
                name: 'check_darkstore_stock',
                description: 'Check real-time stock levels of SKUs at a specific dark-store',
                parameters: {
                    type: 'object',
                    properties: {
                        storeId: { type: 'string', description: 'Dark-store identifier' },
                        skuList: { type: 'array', items: { type: 'string' }, description: 'Array of SKU codes' }
                    },
                    required: ['storeId', 'skuList']
                }
            },
            async (args) => {
                const storeStock = this.inventory[args.storeId] || {};
                const report = {};
                for (const sku of args.skuList) {
                    report[sku] = storeStock[sku] !== undefined ? storeStock[sku] : 0;
                }
                return report;
            }
        );

        // [KYA KARTA HAI] Tool 3: add_smart_bundle_to_cart registration.
        // [KYU ZAROORI HAI] Verified ingredients ko single discounted bundle me assemble karna.
        // [KAISE CHALTA HAI] Computes total price, applies bundle combo discount (e.g. 15%), returns cart bundle payload.
        // [OUTPUT / SIDE EFFECT] Discounted bundle structure generated.
        // [VARIATIONS TO TRY] Dynamic discount based on cart total.
        this.toolRegistry.registerTool(
            {
                name: 'add_smart_bundle_to_cart',
                description: 'Assemble verified items into a 1-click discounted smart recipe bundle',
                parameters: {
                    type: 'object',
                    properties: {
                        bundleTitle: { type: 'string', description: 'Recipe bundle name' },
                        servings: { type: 'number', description: 'Number of people' },
                        items: { type: 'array', description: 'List of verified items with quantities' },
                        discountPercent: { type: 'number', description: 'Combo discount percentage' }
                    },
                    required: ['bundleTitle', 'servings', 'items']
                }
            },
            async (args) => {
                let originalTotal = 0;
                for (const itm of args.items) {
                    originalTotal += itm.price * itm.quantity;
                }
                const discount = args.discountPercent || 15;
                const finalTotal = Math.round(originalTotal * (1 - discount / 100));
                return {
                    bundleId: `BUNDLE-${Date.now()}`,
                    title: args.bundleTitle,
                    servings: args.servings,
                    itemCount: args.items.length,
                    items: args.items,
                    originalPrice: originalTotal,
                    discountPercent: discount,
                    finalPrice: finalTotal,
                    savings: originalTotal - finalTotal,
                    deliverySLA: '10 Minutes'
                };
            }
        );
    }

    // [KYA KARTA HAI] Natural language prompt se recipe aur target servings parse karta hai.
    // [KYU ZAROORI HAI] Intent recognition — e.g. "Make Shahi Paneer for 4 people tonight".
    // [KAISE CHALTA HAI] Regex extraction of recipe name and numeric servings count.
    // [OUTPUT / SIDE EFFECT] { recipeKey, servings }.
    // [VARIATIONS TO TRY] LLM zero-shot classification.
    _parseIntent(prompt) {
        const lower = prompt.toLowerCase();
        let recipeKey = null;
        if (lower.includes('shahi paneer') || lower.includes('paneer')) {
            recipeKey = 'shahi-paneer';
        } else if (lower.includes('chai') || lower.includes('tea')) {
            recipeKey = 'masala-chai';
        }

        // [KYA KARTA HAI] Servings number extract karta hai (e.g. "for 4 people", "4 persons", or default 2).
        // [KYU ZAROORI HAI] Ingredients scaling accurately calculate karna.
        // [KAISE CHALTA HAI] Regex match for numbers adjacent to people/person.
        // [OUTPUT / SIDE EFFECT] Integer servings.
        // [VARIATIONS TO TRY] Parse words like "four" or "family".
        const match = lower.match(/(\d+)\s*(people|persons|pax|guests)?/);
        const servings = match ? parseInt(match[1], 10) : 2;

        return { recipeKey, servings: Math.max(1, servings) };
    }

    // [KYA KARTA HAI] Main autonomous agentic execution loop run karta hai.
    // [KYU ZAROORI HAI] Multi-step reasoning: Prompt -> Decomposition -> Search -> Stock Check -> Substitution -> Bundle Creation.
    // [KAISE CHALTA HAI] Sequential async steps with autonomous tool calling.
    // [OUTPUT / SIDE EFFECT] Comprehensive agent response with thought trace and final bundle.
    // [VARIATIONS TO TRY] Multi-agent collaboration with a nutrition advisor agent.
    async processRecipeRequest(prompt, darkStoreId) {
        const thoughts = [];
        thoughts.push(`[AGENT THOUGHT] Analyzing user prompt: "${prompt}" for dark store: "${darkStoreId}"`);

        // [KYA KARTA HAI] Step 1: Intent parsing.
        // [KYU ZAROORI HAI] Target recipe aur portion size determine karna.
        // [KAISE CHALTA HAI] Calls _parseIntent.
        // [OUTPUT / SIDE EFFECT] Extracted parameters.
        // [VARIATIONS TO TRY] Fallback to open web search.
        const { recipeKey, servings } = this._parseIntent(prompt);
        if (!recipeKey || !this.recipeKB[recipeKey]) {
            thoughts.push(`[AGENT THOUGHT] Recipe not recognized in current knowledge base.`);
            return {
                status: 'UNRECOGNIZED_RECIPE',
                message: `Sorry, I can currently assemble smart bundles for 'Shahi Paneer' or 'Masala Chai'.`,
                thoughts
            };
        }

        thoughts.push(`[AGENT THOUGHT] Recipe identified as '${recipeKey}' scaled for ${servings} people.`);
        const ingredients = this.recipeKB[recipeKey];

        // [KYA KARTA HAI] Step 2: Ingredient decomposition and vector catalog search.
        // [KYU ZAROORI HAI] Har ingredient ke liye catalog me matching SKUs dhoondna.
        // [KAISE CHALTA HAI] Invokes `search_catalog` tool asynchronously.
        // [OUTPUT / SIDE EFFECT] Matched SKUs.
        // [VARIATIONS TO TRY] Select multiple candidates for substitution.
        const matchedItems = [];
        for (const ing of ingredients) {
            thoughts.push(`[AGENT ACTION] Calling tool 'search_catalog' for ingredient: '${ing.query}'...`);
            const searchRes = await this.toolRegistry.executeTool('search_catalog', { query: ing.query, topK: 2 });
            const topMatch = searchRes.data && searchRes.data[0];

            if (topMatch) {
                // [KYA KARTA HAI] Servings scaling factor calculate karta hai.
                // [KYU ZAROORI HAI] 4 logon ke liye 2x quantity add karna.
                // [KAISE CHALTA HAI] Math.ceil(servings / ing.perPersons).
                // [OUTPUT / SIDE EFFECT] Scaled integer quantity.
                // [VARIATIONS TO TRY] Float quantities with metric conversion.
                const multiplier = Math.max(1, Math.ceil(servings / ing.perPersons));
                matchedItems.push({
                    sku: topMatch.sku,
                    name: topMatch.name,
                    price: topMatch.price,
                    quantity: multiplier,
                    unitSpec: `${ing.baseQty * multiplier} ${ing.unit}`
                });
                thoughts.push(`[AGENT OBSERVATION] Matched '${ing.query}' -> '${topMatch.name}' (SKU: ${topMatch.sku}, Qty: ${multiplier})`);
            }
        }

        // [KYA KARTA HAI] Step 3: Real-time dark store inventory verification.
        // [KYU ZAROORI HAI] Ensure karna ki customer ko out-of-stock items na bechein.
        // [KAISE CHALTA HAI] Invokes `check_darkstore_stock` tool.
        // [OUTPUT / SIDE EFFECT] Stock report map.
        // [VARIATIONS TO TRY] Reserve stock with temporary lease.
        const skuList = matchedItems.map(i => i.sku);
        thoughts.push(`[AGENT ACTION] Calling tool 'check_darkstore_stock' for ${skuList.length} SKUs...`);
        const stockRes = await this.toolRegistry.executeTool('check_darkstore_stock', {
            storeId: darkStoreId,
            skuList
        });
        const stockReport = stockRes.data || {};

        // [KYA KARTA HAI] Step 4: Out-of-Stock handling & Automatic intelligent substitution.
        // [KYU ZAROORI HAI] Agar koi brand out-of-stock hai (e.g. Regular Paneer = 0), toh cart bundle tootne ke bajaye substitute (e.g. Malai Paneer) recommend karna.
        // [KAISE CHALTA HAI] If stock == 0, searches secondary candidate in catalog and replaces it.
        // [OUTPUT / SIDE EFFECT] Zero out-of-stock items in final bundle.
        // [VARIATIONS TO TRY] Prompt user before automatic substitution.
        const verifiedItems = [];
        for (const item of matchedItems) {
            const currentStock = stockReport[item.sku] || 0;
            if (currentStock >= item.quantity) {
                verifiedItems.push(item);
                thoughts.push(`[AGENT OBSERVATION] SKU ${item.sku} ('${item.name}') in stock (Available: ${currentStock}). Kept in bundle.`);
            } else {
                thoughts.push(`[AGENT REASONING] SKU ${item.sku} ('${item.name}') OUT OF STOCK (Available: ${currentStock}, Needed: ${item.quantity}). Finding substitute...`);
                // [KYA KARTA HAI] Automatic substitution vector search.
                // [KYU ZAROORI HAI] Alternative product find karna.
                // [KAISE CHALTA HAI] Calls search_catalog for similar terms.
                // [OUTPUT / SIDE EFFECT] Substitute item.
                // [VARIATIONS TO TRY] Rule-based substitution table.
                const subRes = await this.toolRegistry.executeTool('search_catalog', { query: `${item.name} alternative`, topK: 3 });
                const validSub = subRes.data?.find(c => c.sku !== item.sku && (stockReport[c.sku] || 0) >= item.quantity);

                if (validSub) {
                    verifiedItems.push({
                        sku: validSub.sku,
                        name: `${validSub.name} (Substituted)`,
                        price: validSub.price,
                        quantity: item.quantity,
                        unitSpec: item.unitSpec
                    });
                    thoughts.push(`[AGENT ACTION] Substituted out-of-stock item with '${validSub.name}' (SKU: ${validSub.sku})`);
                } else {
                    thoughts.push(`[AGENT WARNING] No immediate substitute found for '${item.name}'. Omitted from bundle.`);
                }
            }
        }

        // [KYA KARTA HAI] Step 5: Construct 1-click Smart Bundle via Tool Calling.
        // [KYU ZAROORI HAI] Verified ingredients ko single checkout unit me bind karna with 15% combo savings.
        // [KAISE CHALTA HAI] Invokes `add_smart_bundle_to_cart` tool.
        // [OUTPUT / SIDE EFFECT] Complete smart bundle ready for customer.
        // [VARIATIONS TO TRY] Tiered discount based on item count.
        thoughts.push(`[AGENT ACTION] Calling tool 'add_smart_bundle_to_cart' with ${verifiedItems.length} verified items...`);
        const bundleRes = await this.toolRegistry.executeTool('add_smart_bundle_to_cart', {
            bundleTitle: `${recipeKey.replace('-', ' ').toUpperCase()} Smart Recipe Kit`,
            servings,
            items: verifiedItems,
            discountPercent: 15
        });

        thoughts.push(`[AGENT CONCLUSION] Smart bundle successfully constructed! Customer saves ₹${bundleRes.data.savings} with 10-min delivery.`);

        return {
            status: 'SUCCESS',
            prompt,
            servings,
            thoughts,
            bundle: bundleRes.data
        };
    }
}
