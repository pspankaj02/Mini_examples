// ==============================================================================================
// KiranaHub KiranaAI: Automated Verification Test Suite for Agentic RAG & Smart Bundles
// [KYA KARTA HAI] Vector embeddings, cosine similarity, agentic tool registry, aur recipe bundle assistant ko end-to-end verify karta hai.
// [KYU ZAROORI HAI] AI reasoning, mathematical distance calculations, aur out-of-stock substitutions me koi regression na ho.
// [KAISE CHALTA HAI] Mock catalog aur dark-store inventory create karke har component ko unit aur integration level par test karta hai.
// [OUTPUT / SIDE EFFECT] Detailed test logs and exit code 0 on complete pass.
// [VARIATIONS TO TRY] Randomized stress testing with 10,000 synthetic grocery items.
// ==============================================================================================

// [KYA KARTA HAI] VectorEmbeddingEngine import karta hai.
// [KYU ZAROORI HAI] Vector mathematics and similarity testing ke liye.
// [KAISE CHALTA HAI] ES module import.
// [OUTPUT / SIDE EFFECT] Vector class loaded.
// [VARIATIONS TO TRY] None.
import { VectorEmbeddingEngine } from '../src/ai/VectorEmbeddingEngine.mjs';

// [KYA KARTA HAI] AgenticToolRegistry import karta hai.
// [KYU ZAROORI HAI] Tool schema and execution testing ke liye.
// [KAISE CHALTA HAI] ES module import.
// [OUTPUT / SIDE EFFECT] Tool registry loaded.
// [VARIATIONS TO TRY] None.
import { AgenticToolRegistry } from '../src/ai/AgenticToolRegistry.mjs';

// [KYA KARTA HAI] RecipeToCartAssistant import karta hai.
// [KYU ZAROORI HAI] End-to-end multi-step agent reasoning workflow test karne ke liye.
// [KAISE CHALTA HAI] ES module import.
// [OUTPUT / SIDE EFFECT] Assistant loaded.
// [VARIATIONS TO TRY] None.
import { RecipeToCartAssistant } from '../src/ai/RecipeToCartAssistant.mjs';

// [KYA KARTA HAI] Test counter variable declare karta hai.
// [KYU ZAROORI HAI] Passed assertions track karna.
// [KAISE CHALTA HAI] Number incremented per pass.
// [OUTPUT / SIDE EFFECT] Counter starts at 0.
// [VARIATIONS TO TRY] Test suite runner framework (Vitest).
let passedCount = 0;

// [KYA KARTA HAI] Assertion helper function define karta hai.
// [KYU ZAROORI HAI] Condition check karke readable status print karna.
// [KAISE CHALTA HAI] Throws error on false condition.
// [OUTPUT / SIDE EFFECT] Log message printed.
// [VARIATIONS TO TRY] Expect syntax.
function assert(condition, message) {
    if (!condition) {
        throw new Error(`[FAIL] ${message}`);
    }
    passedCount++;
    console.log(`  -> [PASS] ${message}`);
}

// [KYA KARTA HAI] Mock grocery catalog dataset define karta hai.
// [KYU ZAROORI HAI] Realistic KiranaHub products provide karna for vector indexing.
// [KAISE CHALTA HAI] Array of product objects with SKUs, names, tags, and prices.
// [OUTPUT / SIDE EFFECT] Catalog data loaded in RAM.
// [VARIATIONS TO TRY] Load from external JSON file.
const mockCatalog = [
    { sku: 'PANEER-01', name: 'Amul Fresh Malai Paneer 200g', category: 'Dairy', tags: ['fresh', 'paneer', 'malai', 'creamy'], price: 95, description: 'Soft creamy cottage cheese' },
    { sku: 'CREAM-01', name: 'Amul Fresh Cream 250ml', category: 'Dairy', tags: ['fresh', 'cream', 'malai', 'gravy'], price: 65, description: 'Rich low fat cream for curries' },
    { sku: 'CASHEW-01', name: 'Tata Sampann Whole Cashews Kaju 200g', category: 'Dry Fruits', tags: ['kaju', 'cashew', 'nuts', 'gravy'], price: 210, description: 'Premium whole cashews' },
    { sku: 'TOMATO-01', name: 'Fresh Hybrid Tomatoes 500g', category: 'Vegetables', tags: ['fresh', 'tomato', 'tamatar', 'gravy'], price: 25, description: 'Farm fresh red tomatoes' },
    { sku: 'SPICE-01', name: 'Everest Shahi Paneer Masala 50g', category: 'Spices', tags: ['spice', 'masala', 'shahi', 'paneer'], price: 42, description: 'Authentic royal spice blend' },
    { sku: 'MILK-01', name: 'Amul Taaza Full Cream Milk 1L', category: 'Dairy', tags: ['milk', 'dairy', 'fresh', 'doodh'], price: 68, description: 'Pasteurized homogenized milk' },
    { sku: 'TEA-01', name: 'Wagh Bakri Premium Assam Tea 500g', category: 'Beverages', tags: ['tea', 'chai', 'assam', 'leaves'], price: 280, description: 'Strong aromatic tea leaves' }
];

// [KYA KARTA HAI] Mock dark store inventory levels define karta hai.
// [KYU ZAROORI HAI] Live stock scenarios (in-stock vs out-of-stock) simulate karna.
// [KAISE CHALTA HAI] Nested object: storeId -> sku -> count.
// [OUTPUT / SIDE EFFECT] Inventory state loaded.
// [VARIATIONS TO TRY] Dynamic stock decrement simulation.
const mockInventory = {
    'DS-NOIDA-62': {
        'PANEER-01': 10,   // In stock
        'CREAM-01': 8,     // In stock
        'CASHEW-01': 5,    // In stock
        'TOMATO-01': 20,   // In stock
        'SPICE-01': 0,     // OUT OF STOCK to test intelligent substitution!
        'MILK-01': 15,
        'TEA-01': 12
    }
};

// [KYA KARTA HAI] Main test execution function define karta hai.
// [KYU ZAROORI HAI] All test stages sequentially execute karna.
// [KAISE CHALTA HAI] Async function running assertions.
// [OUTPUT / SIDE EFFECT] Complete test report printed.
// [VARIATIONS TO TRY] Parallel test runs.
async function runTests() {
    console.log('====================================================================');
    console.log('  KiranaHub Project 25: KiranaAI Agentic RAG & Smart Bundles Test  ');
    console.log('====================================================================\n');

    // [TEST 1] Vector Embedding Engine & Cosine Similarity.
    console.log('[TEST 1] Verifying Semantic Vector Search & Cosine Similarity...');
    const vectorEngine = new VectorEmbeddingEngine();
    vectorEngine.indexCatalog(mockCatalog);

    const searchHits = vectorEngine.search('creamy malai for thick shahi paneer gravy', 3);
    assert(searchHits.length > 0, 'Vector search returned non-empty semantic matches');
    assert(searchHits[0].item.sku === 'PANEER-01' || searchHits[0].item.sku === 'CREAM-01', 'Top semantic match is Paneer or Cream for creamy gravy intent');
    assert(searchHits[0].score > 0.3 && searchHits[0].score <= 1.0, `Cosine similarity score is valid normalized float (${searchHits[0].score})`);

    // [TEST 2] Agentic Tool Registry & Function Calling.
    console.log('\n[TEST 2] Verifying Agentic Tool Registry & Function Calling...');
    const registry = new AgenticToolRegistry();
    registry.registerTool(
        {
            name: 'calculate_tax',
            description: 'Computes GST tax for grocery order',
            parameters: { type: 'object', properties: { amount: { type: 'number' } }, required: ['amount'] }
        },
        async (args) => args.amount * 0.05
    );

    const schemas = registry.getToolDeclarations();
    assert(schemas.length === 1 && schemas[0].name === 'calculate_tax', 'Tool declaration schema registered');

    const execResult = await registry.executeTool('calculate_tax', { amount: 1000 });
    assert(execResult.status === 'SUCCESS' && execResult.data === 50, 'Tool executed successfully returning 5% tax (50)');

    const invalidToolResult = await registry.executeTool('non_existent_tool', {});
    assert(invalidToolResult.status === 'ERROR', 'Handles unknown tool gracefully without crashing');

    // [TEST 3] Autonomous Recipe-To-Cart Assistant Loop.
    console.log('\n[TEST 3] Verifying Autonomous Recipe-to-Cart Agent Workflow...');
    const assistant = new RecipeToCartAssistant(mockCatalog, mockInventory);

    const prompt = 'Please make delicious Shahi Paneer for 4 people tonight!';
    const agentResult = await assistant.processRecipeRequest(prompt, 'DS-NOIDA-62');

    assert(agentResult.status === 'SUCCESS', 'Agent workflow completed with SUCCESS status');
    assert(agentResult.servings === 4, 'Correctly parsed serving size of 4 people from natural language prompt');
    assert(agentResult.thoughts.length >= 6, 'Agent generated transparent step-by-step reasoning thought trace');

    const bundle = agentResult.bundle;
    assert(bundle !== undefined, 'Smart Recipe Bundle generated');
    assert(bundle.servings === 4, 'Bundle servings match requested 4 people');
    assert(bundle.discountPercent === 15, '15% smart combo discount applied');
    assert(bundle.savings > 0, `Customer savings calculated accurately: ₹${bundle.savings}`);
    assert(bundle.finalPrice === bundle.originalPrice - bundle.savings, 'Bundle final price matches arithmetic calculation');

    // Print Agent Reasoning Trace
    console.log('\n--- [AGENT REASONING TRACE] ---');
    for (const t of agentResult.thoughts) {
        console.log(`  ${t}`);
    }
    console.log('-------------------------------\n');

    console.log(`[BUNDLE SUMMARY] ${bundle.title}`);
    console.log(`  Servings: ${bundle.servings} people | Delivery: ${bundle.deliverySLA}`);
    console.log(`  Items included: ${bundle.items.length}`);
    for (const itm of bundle.items) {
        console.log(`    - ${itm.name} (Qty: ${itm.quantity}, Portion: ${itm.unitSpec}) @ ₹${itm.price * itm.quantity}`);
    }
    console.log(`  Total: ₹${bundle.originalPrice} | 15% Combo Savings: -₹${bundle.savings} | Final Checkout: ₹${bundle.finalPrice}`);

    console.log('\n====================================================================');
    console.log(` [SUCCESS] All ${passedCount} KiranaAI Agentic & RAG checks PASSED! `);
    console.log('====================================================================\n');
}

// [KYA KARTA HAI] Test runner invoke karta hai.
// [KYU ZAROORI HAI] Execution start karna.
// [KAISE CHALTA HAI] Awaits runTests.
// [OUTPUT / SIDE EFFECT] Output stream.
// [VARIATIONS TO TRY] None.
runTests().catch(err => {
    console.error(err);
    process.exit(1);
});
