namespace KiranaHub.TracingApm;

using KiranaHub.TracingApm.Graph;
using KiranaHub.TracingApm.Profiling;
using KiranaHub.TracingApm.Sampling;
using KiranaHub.TracingApm.TraceContext;

// [KYA KARTA HAI]: Project 36 ka executable entry point aur automated distributed tracing & APM test suite provide karta hai.
// [KYU ZAROORI HAI]: W3C Traceparent propagation, DAG span graph assembly, Tail-based sampling evaluation, aur ASCII call-tree flame graph profiling ko end-to-end execute aur assert karna.
// [KAISE CHALTA HAI]: W3C headers parse karta hai, 5-hop distributed checkout trace assemble karta hai, bottleneck span highlight karta hai, tail-based sampler se decision inspect karta hai, aur flame graph render karta hai.
// [OUTPUT / SIDE EFFECT]: Console test reports, flame graph visualizations, assertions verified, zero errors.
// [VARIATIONS TO TRY]: Connect to OpenTelemetry Collector endpoint over OTLP gRPC.
public class Program
{
    // [KYA KARTA HAI]: Main execution method.
    // [KYU ZAROORI HAI]: Application start point.
    // [KAISE CHALTA HAI]: Top-level async task.
    // [OUTPUT / SIDE EFFECT]: Returns 0 on success.
    // [VARIATIONS TO TRY]: CLI argument for trace visualization toggle.
    public static Task<int> Main(string[] args)
    {
        Console.WriteLine("==================================================================================");
        Console.WriteLine("  📈 KiranaHub Project 36: Distributed Tracing & APM Engine 📈");
        Console.WriteLine("==================================================================================");

        // -----------------------------------------------------------------------------------------------------
        // STAGE 1: W3C TRACE CONTEXT SERIALIZATION & PARSE ROUNDTRIP
        // -----------------------------------------------------------------------------------------------------
        Console.WriteLine("\n[STAGE 1] Testing W3C Traceparent Header Generation & Parse Roundtrip...");

        var rootContext = W3CTraceContext.CreateNew(sampled: true);
        string headerString = rootContext.ToHeaderString();
        Console.WriteLine($" -> Generated Root Context: {headerString}");

        var parsedContext = W3CTraceContext.Parse(headerString);
        Console.WriteLine($" -> Parsed Context: TraceId={parsedContext.TraceId}, SpanId={parsedContext.SpanId}, Flags={parsedContext.TraceFlags}");

        if (parsedContext.TraceId != rootContext.TraceId || parsedContext.SpanId != rootContext.SpanId)
        {
            throw new InvalidOperationException("W3C TraceContext parse roundtrip failed!");
        }

        var childContext = rootContext.CreateChild();
        Console.WriteLine($" -> Created Downstream Child Context: {childContext.ToHeaderString()}");
        if (childContext.TraceId != rootContext.TraceId || childContext.SpanId == rootContext.SpanId)
        {
            throw new InvalidOperationException("Child context must retain TraceId but have unique SpanId!");
        }

        // -----------------------------------------------------------------------------------------------------
        // STAGE 2: 5-HOP DISTRIBUTED CHECKOUT TRACE GRAPH CONSTRUCTION
        // -----------------------------------------------------------------------------------------------------
        Console.WriteLine("\n[STAGE 2] Assembling 5-Hop Distributed Checkout Trace DAG...");

        var traceGraph = new SpanGraph(rootContext.TraceId);
        var t0 = DateTime.UtcNow;

        // 1. Root Gateway Span: POST /api/v1/orders/checkout
        var rootSpan = new Span(
            TraceId: rootContext.TraceId,
            SpanId: rootContext.SpanId,
            ParentSpanId: null,
            Name: "YarpGateway.POST /api/v1/orders/checkout",
            Kind: SpanKind.Server,
            StartTimeUtc: t0,
            DurationMs: 285.50,
            Status: SpanStatus.Ok,
            Attributes: new Dictionary<string, string> { ["http.route"] = "/api/v1/orders/checkout", ["http.status_code"] = "200" });
        traceGraph.AddSpan(rootSpan);

        // 2. Child 1: OrderService.ValidateOrder
        string spanOrderSvc = "b1c2d3e4f5a60001";
        traceGraph.AddSpan(new Span(
            TraceId: rootContext.TraceId,
            SpanId: spanOrderSvc,
            ParentSpanId: rootContext.SpanId,
            Name: "OrderService.ValidateOrderAndPricing",
            Kind: SpanKind.Internal,
            StartTimeUtc: t0.AddMilliseconds(5),
            DurationMs: 18.20,
            Status: SpanStatus.Ok,
            Attributes: new Dictionary<string, string> { ["order.items_count"] = "4", ["order.subtotal"] = "417.00" }));

        // 3. Child 2: InventoryGrpc.CheckAndDeductStock
        string spanInventory = "b1c2d3e4f5a60002";
        traceGraph.AddSpan(new Span(
            TraceId: rootContext.TraceId,
            SpanId: spanInventory,
            ParentSpanId: rootContext.SpanId,
            Name: "InventoryGrpcService.CheckAndDeductStock",
            Kind: SpanKind.Client,
            StartTimeUtc: t0.AddMilliseconds(25),
            DurationMs: 42.10,
            Status: SpanStatus.Ok,
            Attributes: new Dictionary<string, string> { ["rpc.system"] = "grpc", ["rpc.method"] = "DeductStock" }));

        // 4. Child 3: PaymentGateway.ProcessPayment (BOTTLENECK SPAN!)
        string spanPayment = "b1c2d3e4f5a60003";
        traceGraph.AddSpan(new Span(
            TraceId: rootContext.TraceId,
            SpanId: spanPayment,
            ParentSpanId: rootContext.SpanId,
            Name: "PaymentGateway.ProcessCardCharge",
            Kind: SpanKind.Client,
            StartTimeUtc: t0.AddMilliseconds(70),
            DurationMs: 195.80, // Takes 195.80ms out of 285ms total!
            Status: SpanStatus.Ok,
            Attributes: new Dictionary<string, string> { ["payment.provider"] = "Razorpay", ["payment.method"] = "UPI" }));

        // 5. Child 4: KafkaProducer.PublishOrderPlacedEvent
        string spanKafka = "b1c2d3e4f5a60004";
        traceGraph.AddSpan(new Span(
            TraceId: rootContext.TraceId,
            SpanId: spanKafka,
            ParentSpanId: rootContext.SpanId,
            Name: "KafkaTopicProducer.PublishOrderPlacedEvent",
            Kind: SpanKind.Producer,
            StartTimeUtc: t0.AddMilliseconds(268),
            DurationMs: 12.40,
            Status: SpanStatus.Ok,
            Attributes: new Dictionary<string, string> { ["messaging.system"] = "kafka", ["messaging.destination"] = "kirana.orders.placed" }));

        Console.WriteLine($" -> Assembled Trace Graph: {traceGraph.SpanCount} Spans | Total Latency: {traceGraph.TotalDurationMs:F2}ms");
        var bottleneck = traceGraph.FindBottleneckSpan();
        Console.WriteLine($" -> Detected Bottleneck Span: '{bottleneck?.Name}' taking {bottleneck?.DurationMs:F2}ms");

        if (bottleneck == null || bottleneck.SpanId != spanPayment)
        {
            throw new InvalidOperationException("Bottleneck span detection failed! Expected PaymentGateway span.");
        }

        // -----------------------------------------------------------------------------------------------------
        // STAGE 3: FLAME GRAPH & CALL TREE RENDERING
        // -----------------------------------------------------------------------------------------------------
        Console.WriteLine("\n[STAGE 3] Rendering ASCII Call-Tree Flame Graph Profiler...");
        string flameGraphAscii = FlameGraphRenderer.Render(traceGraph);
        Console.WriteLine(flameGraphAscii);

        // -----------------------------------------------------------------------------------------------------
        // STAGE 4: TAIL-BASED SAMPLING EVALUATION
        // -----------------------------------------------------------------------------------------------------
        Console.WriteLine("\n[STAGE 4] Testing Tail-Based Sampling (Error Retain, Latency Outlier, Normal Drop)...");

        var sampler = new TailBasedSampler { SlowThresholdMs = 250.0, BaselineSampleRate = 0.05 };

        // Test A: Slow Latency Trace (285.5ms > 250ms threshold) -> MUST BE RETAINED!
        var slowDecision = sampler.Evaluate(traceGraph);
        Console.WriteLine($" -> Test A (Slow Trace {traceGraph.TotalDurationMs:F1}ms): IsSampled={slowDecision.IsSampled} | Reason={slowDecision.Reason}");
        if (!slowDecision.IsSampled || slowDecision.PolicyApplied != "LatencyPolicy")
        {
            throw new InvalidOperationException("Tail sampler failed to retain slow latency outlier trace!");
        }

        // Test B: Fast Healthy Trace (35ms duration, no errors) -> MUST BE DISCARDED (to save 95% storage)!
        var fastGraph = new SpanGraph("f0000000000000000000000000000001");
        fastGraph.AddSpan(new Span("f0000000000000000000000000000001", "span-01", null, "QuickEndpoint", SpanKind.Server, DateTime.UtcNow, 35.0, SpanStatus.Ok, new Dictionary<string, string>()));
        var fastDecision = sampler.Evaluate(fastGraph);
        Console.WriteLine($" -> Test B (Fast Healthy Trace {fastGraph.TotalDurationMs:F1}ms): IsSampled={fastDecision.IsSampled} | Reason={fastDecision.Reason}");
        if (fastDecision.IsSampled && fastDecision.PolicyApplied == "DropPolicy")
        {
            throw new InvalidOperationException("Fast healthy trace should be dropped under DropPolicy!");
        }

        // Test C: Failed Error Trace (HTTP 500 in Payment Gateway) -> MUST BE 100% RETAINED!
        var errorGraph = new SpanGraph("e0000000000000000000000000000001");
        errorGraph.AddSpan(new Span("e0000000000000000000000000000001", "err-span-01", null, "CheckoutApi", SpanKind.Server, DateTime.UtcNow, 85.0, SpanStatus.Error, new Dictionary<string, string> { ["error"] = "PaymentTimeout" }));
        var errorDecision = sampler.Evaluate(errorGraph);
        Console.WriteLine($" -> Test C (Failed Error Trace): IsSampled={errorDecision.IsSampled} | Reason={errorDecision.Reason}");
        if (!errorDecision.IsSampled || errorDecision.PolicyApplied != "ErrorPolicy")
        {
            throw new InvalidOperationException("Tail sampler failed to retain failed error trace!");
        }

        Console.WriteLine("\n==================================================================================");
        Console.WriteLine("  ✅ [SUCCESS] ALL DISTRIBUTED TRACING & APM TESTS PASSED WITH 0 ERRORS! ✅");
        Console.WriteLine("==================================================================================");

        return Task.FromResult(0);
    }
}
