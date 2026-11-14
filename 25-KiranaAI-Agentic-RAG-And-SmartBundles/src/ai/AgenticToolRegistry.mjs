// ==============================================================================================
// KiranaHub KiranaAI: Schema-Driven Agentic Tool Registry & Function Calling Interface
// [KYA KARTA HAI] LLM Agent ke liye type-safe function schemas register karta hai aur dynamic execution invoke karta hai.
// [KYU ZAROORI HAI] AI ko sirf baatein karne ke bajaye real KiranaHub databases query karne aur carts mutate karne ki power dena.
// [KAISE CHALTA HAI] OpenAI/Gemini JSON Schema format me tool specifications expose karta hai aur name dispatching se handler run karta hai.
// [OUTPUT / SIDE EFFECT] Validated tool execution results returned to the reasoning loop.
// [VARIATIONS TO TRY] Dynamic permission checks and OAuth token enforcement per tool.
// ==============================================================================================

// [KYA KARTA HAI] AgenticToolRegistry class export karta hai.
// [KYU ZAROORI HAI] Available tools ka registry map aur execution dispatcher maintain karna.
// [KAISE CHALTA HAI] Class structure manages tool metadata and async handlers.
// [OUTPUT / SIDE EFFECT] Tool registry blueprint available.
// [VARIATIONS TO TRY] Auto-register tools from TypeScript decorators.
export class AgenticToolRegistry {
    // [KYA KARTA HAI] Registry constructor initialize karta hai.
    // [KYU ZAROORI HAI] In-memory tool storage map initialize karna.
    // [KAISE CHALTA HAI] Creates new Map instance.
    // [OUTPUT / SIDE EFFECT] Empty tools dictionary.
    // [VARIATIONS TO TRY] Inject audit logger.
    constructor() {
        // [KYA KARTA HAI] Tools map storage.
        // [KYU ZAROORI HAI] Tool name se schema aur handler lookup karna.
        // [KAISE CHALTA HAI] Key is tool name, value is { declaration, handler }.
        // [OUTPUT / SIDE EFFECT] Internal tools registry.
        // [VARIATIONS TO TRY] Readonly frozen map.
        this.tools = new Map();
    }

    // [KYA KARTA HAI] Naya tool schema aur execution handler register karta hai.
    // [KYU ZAROORI HAI] LLM ko nayi capability provide karna (e.g. stock checking, coupon applying).
    // [KAISE CHALTA HAI] Validates declaration object aur map me save karta hai.
    // [OUTPUT / SIDE EFFECT] Tool registered and available for invocation.
    // [VARIATIONS TO TRY] Schema validation with Zod.
    registerTool(declaration, handler) {
        // [KYA KARTA HAI] Tool name uniqueness check karta hai.
        // [KYU ZAROORI HAI] Duplicate tool registration overwrite warning or prevention.
        // [KAISE CHALTA HAI] Checks this.tools.has(declaration.name).
        // [OUTPUT / SIDE EFFECT] Safe registration.
        // [VARIATIONS TO TRY] Throw error on duplicate.
        if (this.tools.has(declaration.name)) {
            console.warn(`[WARN] Tool '${declaration.name}' already registered. Overwriting handler.`);
        }
        // [KYA KARTA HAI] Tool metadata aur async handler map me set karta hai.
        // [KYU ZAROORI HAI] Dispatch lookup ke liye.
        // [KAISE CHALTA HAI] Map.set with declaration and handler.
        // [OUTPUT / SIDE EFFECT] Registered in tools map.
        // [VARIATIONS TO TRY] Middleware wrappers for rate limiting.
        this.tools.set(declaration.name, { declaration, handler });
    }

    // [KYA KARTA HAI] LLM prompt ke liye standard function calling schema declarations return karta hai.
    // [KYU ZAROORI HAI] Gemini/OpenAI API payload me `tools: [...]` array pass karna hota hai.
    // [KAISE CHALTA HAI] Extracts declaration objects from internal map.
    // [OUTPUT / SIDE EFFECT] Array of JSON schemas.
    // [VARIATIONS TO TRY] Format as OpenAPI 3.0 specification.
    getToolDeclarations() {
        // [KYA KARTA HAI] Declarations array return karta hai.
        // [KYU ZAROORI HAI] LLM system prompt me inject karna.
        // [KAISE CHALTA HAI] Maps tools.values() to item.declaration.
        // [OUTPUT / SIDE EFFECT] Schema list.
        // [VARIATIONS TO TRY] Filter tools by caller role.
        return Array.from(this.tools.values()).map(t => t.declaration);
    }

    // [KYA KARTA HAI] Tool name aur arguments receive karke associated handler execute karta hai.
    // [KYU ZAROORI HAI] LLM ke tool_call decision ko real backend system execution me convert karna.
    // [KAISE CHALTA HAI] Map se handler dhoondta hai, execute karta hai aur standard JSON format me result return karta hai.
    // [OUTPUT / SIDE EFFECT] Structured tool execution output.
    // [VARIATIONS TO TRY] Timeout execution with AbortController.
    async executeTool(name, args) {
        // [KYA KARTA HAI] Tool existence verify karta hai.
        // [KYU ZAROORI HAI] Hallucinated/invalid tool names par graceful error handle karna.
        // [KAISE CHALTA HAI] Map lookup for tool entry.
        // [OUTPUT / SIDE EFFECT] Resolves tool or returns error object.
        // [VARIATIONS TO TRY] Suggest closest matching tool name on typo.
        const entry = this.tools.get(name);
        if (!entry) {
            return {
                status: 'ERROR',
                message: `Tool '${name}' is not registered in KiranaHub Agentic Registry.`
            };
        }

        try {
            // [KYA KARTA HAI] Tool ka registered asynchronous handler call karta hai arguments ke sath.
            // [KYU ZAROORI HAI] Real database query ya calculation execute karna.
            // [KAISE CHALTA HAI] Awaits async handler execution.
            // [OUTPUT / SIDE EFFECT] Business logic executes.
            // [VARIATIONS TO TRY] Validate args schema before calling handler.
            const result = await entry.handler(args);
            // [KYA KARTA HAI] Success response payload construct karta hai.
            // [KYU ZAROORI HAI] Uniform envelope format across all tools.
            // [KAISE CHALTA HAI] Returns JSON object with status OK.
            // [OUTPUT / SIDE EFFECT] Success response returned.
            // [VARIATIONS TO TRY] Include execution latency metrics.
            return {
                status: 'SUCCESS',
                tool: name,
                data: result
            };
        } catch (error) {
            // [KYA KARTA HAI] Execution exceptions catch karta hai.
            // [KYU ZAROORI HAI] AI agent loop crash hone se rokna agar koi microservice call fail ho jaye.
            // [KAISE CHALTA HAI] Catches error and encapsulates message.
            // [OUTPUT / SIDE EFFECT] Handled failure response.
            // [VARIATIONS TO TRY] Retry transient failures automatically.
            return {
                status: 'FAILURE',
                tool: name,
                error: error.message || String(error)
            };
        }
    }
}
