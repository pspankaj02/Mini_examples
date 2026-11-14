namespace KiranaHub.TracingApm.Profiling;

using System.Text;
using KiranaHub.TracingApm.Graph;

// [KYA KARTA HAI]: Distributed trace spans ke liye ASCII Call-Tree aur Flame Graph profiler render karta hai.
// [KYU ZAROORI HAI]: Multi-service latency bottlenecks (e.g. 3rd party payment gateway slow response) ko visually highlight karna without heavy UI tools.
// [KAISE CHALTA HAI]: Root span se start karke recursively children traverse karta hai, indentations aur branch markers lagata hai, percentage share calculate karta hai, aur visual ASCII latency bar draw karta hai.
// [OUTPUT / SIDE EFFECT]: Formatted multi-line ASCII flame graph string.
// [VARIATIONS TO TRY]: HTML flame graph with interactive click-to-expand SVG spans.
public class FlameGraphRenderer
{
    // [KYA KARTA HAI]: SpanGraph ko formatted ASCII Call-Tree me convert karta hai.
    // [KYU ZAROORI HAI]: Visual terminal inspection and root cause diagnosis.
    // [KAISE CHALTA HAI]: Root span find karta hai aur RenderSpan() recursively call karta hai.
    // [OUTPUT / SIDE EFFECT]: Complete ASCII call tree string.
    // [VARIATIONS TO TRY]: Colorized terminal output with ANSI colors.
    public static string Render(SpanGraph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);

        var sb = new StringBuilder();
        sb.AppendLine($"==================================================================================");
        sb.AppendLine($"  🔥 DISTRIBUTED TRACE FLAME GRAPH | TraceId: {graph.TraceId}");
        sb.AppendLine($"  Total Duration: {graph.TotalDurationMs:F2}ms | Total Spans: {graph.SpanCount} | Has Errors: {graph.HasErrors}");
        sb.AppendLine($"==================================================================================");

        var root = graph.GetRootSpan();
        if (root == null)
        {
            sb.AppendLine("  [No root span found in trace graph]");
            return sb.ToString();
        }

        var bottleneck = graph.FindBottleneckSpan();
        double totalDuration = graph.TotalDurationMs;

        RenderSpan(graph, root, sb, indent: "", isLast: true, totalDuration, bottleneck);

        return sb.ToString();
    }

    // [KYA KARTA HAI]: Individual span aur uske children ko recursively render karta hai with branch markers.
    // [KYU ZAROORI HAI]: Hierarchical call tree formatting.
    // [KAISE CHALTA HAI]: Current span render karta hai, percentage calculate karta hai, visual bar banata hai, aur children iterate karta hai.
    // [OUTPUT / SIDE EFFECT]: Appends span lines to StringBuilder.
    // [VARIATIONS TO TRY]: Self-time vs child-time breakdown.
    private static void RenderSpan(
        SpanGraph graph,
        Span span,
        StringBuilder sb,
        string indent,
        bool isLast,
        double totalDuration,
        Span? bottleneck)
    {
        string marker = isLast ? "└── " : "├── ";
        double pct = totalDuration > 0 ? (span.DurationMs / totalDuration) * 100.0 : 0.0;
        string bar = RenderBar(pct);

        bool isBottleneck = bottleneck != null && span.SpanId == bottleneck.SpanId && span.ParentSpanId != null;
        string statusTag = span.Status == SpanStatus.Error ? " [🚨 ERROR]" : "";
        string bottleneckTag = isBottleneck ? " [⚠️ BOTTLENECK]" : "";

        sb.AppendLine($"{indent}{marker}{span.Name} ({span.Kind}) [{span.DurationMs:F2}ms] {bar} {pct,5:F1}%{statusTag}{bottleneckTag}");

        // Print attributes if any
        if (span.Attributes.Count > 0)
        {
            string childIndent = indent + (isLast ? "    " : "│   ");
            var attrStrings = span.Attributes.Select(a => $"{a.Key}={a.Value}");
            sb.AppendLine($"{childIndent}   ↳ attrs: {string.Join(", ", attrStrings)}");
        }

        var children = graph.GetChildren(span.SpanId);
        string nextIndent = indent + (isLast ? "    " : "│   ");

        for (int i = 0; i < children.Count; i++)
        {
            bool childIsLast = (i == children.Count - 1);
            RenderSpan(graph, children[i], sb, nextIndent, childIsLast, totalDuration, bottleneck);
        }
    }

    // [KYA KARTA HAI]: Percentage value ke mutabiq 10-char ASCII progress bar draw karta hai.
    // [KYU ZAROORI HAI]: Visual latency footprint estimation.
    // [KAISE CHALTA HAI]: Filled chars '█' aur unfilled chars '░'.
    // [OUTPUT / SIDE EFFECT]: String like "[██████░░░░]".
    // [VARIATIONS TO TRY]: 20-char bar for wider terminals.
    private static string RenderBar(double percentage)
    {
        int filled = (int)Math.Round((Math.Clamp(percentage, 0.0, 100.0) / 100.0) * 10.0);
        int empty = 10 - filled;
        return $"[{new string('█', filled)}{new string('░', empty)}]";
    }
}
