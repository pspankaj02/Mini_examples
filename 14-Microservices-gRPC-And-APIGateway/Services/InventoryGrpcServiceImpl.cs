using System.Collections.Concurrent;
using Grpc.Core;
using KiranaHub.MicroservicesAndGateway.Protos;

namespace KiranaHub.MicroservicesAndGateway.Services;

// =========================================================================================
// [TOPIC]: gRPC Microservice Implementation (Unary RPC & Server Streaming RPC)
// [PURPOSE]: Binary Protobuf over HTTP/2 service for microsecond inventory operations.
// =========================================================================================

// [KYA KARTA HAI]: Auto-generated abstract base class InventoryGrpcService.InventoryGrpcServiceBase se inherit karta hai.
// [KYU ZAROORI HAI]: High-performance inter-service communication; JSON serialization overhead aur HTTP/1.1 head-of-line blocking eliminate karna.
public class InventoryGrpcServiceImpl : InventoryGrpcService.InventoryGrpcServiceBase
{
    // [KYA KARTA HAI]: In-memory thread-safe stock ledger dictionary (SKU -> Stock Count).
    private static readonly ConcurrentDictionary<string, int> WarehouseStock = new()
    {
        ["MILK-01"] = 50,
        ["BTR-01"] = 30,
        ["ATTA-01"] = 20
    };

    // [KYA KARTA HAI]: Logger reference.
    private readonly ILogger<InventoryGrpcServiceImpl> _logger;

    // [KYA KARTA HAI]: Constructor DI.
    public InventoryGrpcServiceImpl(ILogger<InventoryGrpcServiceImpl> logger)
    {
        _logger = logger;
    }

    // -------------------------------------------------------------------------------------
    // 1. UNARY RPC: CHECK STOCK
    // -------------------------------------------------------------------------------------

    // [KYA KARTA HAI]: Single request accept karke single response return karta hai (Unary pattern).
    public override Task<CheckStockResponse> CheckStock(CheckStockRequest request, ServerCallContext context)
    {
        // [KYA KARTA HAI]: Request arrival log karta hai.
        _logger.LogInformation("[GRPC INVENTORY] CheckStock called for SKU: {Sku}, Requested: {Qty}",
            request.Sku, request.RequestedQuantity);

        // [KYA KARTA HAI]: Warehouse ledger me SKU lookup karta hai.
        int currentStock = WarehouseStock.GetValueOrDefault(request.Sku, 0);

        // [KYA KARTA HAI]: Check karta hai ki stock sufficient hai ya nahi.
        bool isAvailable = currentStock >= request.RequestedQuantity;

        // [KYA KARTA HAI]: Protobuf response message construct karta hai.
        var response = new CheckStockResponse
        {
            Sku = request.Sku,
            CurrentStock = currentStock,
            IsAvailable = isAvailable
        };

        // [KYA KARTA HAI]: Task completed response return karta hai.
        return Task.FromResult(response);
    }

    // -------------------------------------------------------------------------------------
    // 2. UNARY RPC: DEDUCT STOCK
    // -------------------------------------------------------------------------------------

    // [KYA KARTA HAI]: Warehouse inventory se atomically stock deduct karta hai.
    public override Task<DeductStockResponse> DeductStock(DeductStockRequest request, ServerCallContext context)
    {
        // [KYA KARTA HAI]: Request log karta hai.
        _logger.LogInformation("[GRPC INVENTORY] DeductStock called for SKU: {Sku}, Quantity: {Qty}",
            request.Sku, request.Quantity);

        // [KYA KARTA HAI]: Current stock lookup karta hai.
        int current = WarehouseStock.GetValueOrDefault(request.Sku, 0);

        // [KYA KARTA HAI]: Check karta hai ki stock deduction possible hai ya nahi.
        if (current < request.Quantity)
        {
            // [KYA KARTA HAI]: Failure response return karta hai with error reason.
            return Task.FromResult(new DeductStockResponse
            {
                Sku = request.Sku,
                Success = false,
                RemainingStock = current,
                Message = $"Insufficient inventory. Current stock: {current}, Requested: {request.Quantity}"
            });
        }

        // [KYA KARTA HAI]: Atomic deduction execute karta hai dictionary me.
        int newStock = current - request.Quantity;
        WarehouseStock[request.Sku] = newStock;

        // [KYA KARTA HAI]: Success response return karta hai with remaining stock balance.
        return Task.FromResult(new DeductStockResponse
        {
            Sku = request.Sku,
            Success = true,
            RemainingStock = newStock,
            Message = "Stock deducted successfully."
        });
    }

    // -------------------------------------------------------------------------------------
    // 3. SERVER STREAMING RPC: SUBSCRIBE TO STOCK STREAM
    // -------------------------------------------------------------------------------------

    // [KYA KARTA HAI]: Single request ke response me continuous stream of messages bhejta hai (Server Streaming pattern).
    // [KYU ZAROORI HAI]: Real-time inventory telemetry: client HTTP disconnect kiye bina stock changes stream karta rehta hai.
    // [KAISE CHALTA HAI]: IServerStreamWriter<T>.WriteAsync() HTTP/2 DATA frames emit karta hai.
    public override async Task SubscribeStockStream(
        SubscribeStockStreamRequest request,
        IServerStreamWriter<StockUpdateStreamResponse> responseStream,
        ServerCallContext context)
    {
        _logger.LogInformation("[GRPC INVENTORY] Client subscribed to stock stream for category: {Category}", request.Category);

        // [KYA KARTA HAI]: Demonstration ke liye 3 continuous stream events emit karta hai.
        for (int i = 1; i <= 3; i++)
        {
            // [KYA KARTA HAI]: Check karta hai client disconnect toh nahi ho gaya.
            if (context.CancellationToken.IsCancellationRequested) break;

            // [KYA KARTA HAI]: Stream response message construct karta hai.
            var streamUpdate = new StockUpdateStreamResponse
            {
                Sku = "MILK-01",
                CurrentStock = 50 - i,
                StockDelta = -1,
                Reason = $"Customer checkout #{i}",
                TimestampUtc = DateTime.UtcNow.ToString("O")
            };

            // [KYA KARTA HAI]: Client stream pipe me write karta hai.
            await responseStream.WriteAsync(streamUpdate);

            // [KYA KARTA HAI]: 50ms interval wait karta hai next frame se pehle.
            await Task.Delay(50);
        }
    }
}

