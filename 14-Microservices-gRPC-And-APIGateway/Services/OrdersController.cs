using KiranaHub.MicroservicesAndGateway.Protos;
using Microsoft.AspNetCore.Mvc;

namespace KiranaHub.MicroservicesAndGateway.Services;

// =========================================================================================
// [TOPIC]: Orders REST Microservice consuming Inventory gRPC Client
// [PURPOSE]: Microservice orchestrating external REST clients with internal binary gRPC calls.
// =========================================================================================

// [KYA KARTA HAI]: Orders REST API Controller jo internal calls ke liye gRPC client use karta hai.
[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    // [KYA KARTA HAI]: Strongly-typed gRPC client interface hold karta hai.
    private readonly InventoryGrpcService.InventoryGrpcServiceClient _inventoryClient;

    // [KYA KARTA HAI]: Logger reference.
    private readonly ILogger<OrdersController> _logger;

    // [KYA KARTA HAI]: Constructor DI: Injects gRPC client factory-managed client.
    public OrdersController(
        InventoryGrpcService.InventoryGrpcServiceClient inventoryClient,
        ILogger<OrdersController> logger)
    {
        _inventoryClient = inventoryClient;
        _logger = logger;
    }

    // [KYA KARTA HAI]: POST /api/orders/checkout endpoint jo gRPC Unary RPCs invoke karta hai.
    [HttpPost("checkout")]
    public async Task<IActionResult> CheckoutOrder([FromBody] CheckoutRequest request)
    {
        _logger.LogInformation("[REST ORDERS MICROSERVICE] Processing checkout for SKU: {Sku}, Quantity: {Qty}",
            request.Sku, request.Quantity);

        // ---------------------------------------------------------------------------------
        // 1. CALL INVENTORY SERVICE VIA GRPC UNARY RPC: CheckStock
        // ---------------------------------------------------------------------------------

        // [KYA KARTA HAI]: Protobuf request build karta hai.
        var checkReq = new CheckStockRequest { Sku = request.Sku, RequestedQuantity = request.Quantity };

        // [KYA KARTA HAI]: gRPC Unary call execute karta hai over HTTP/2.
        var checkResp = await _inventoryClient.CheckStockAsync(checkReq);

        // [KYA KARTA HAI]: Stock availability check karta hai.
        if (!checkResp.IsAvailable)
        {
            return BadRequest(new
            {
                Error = "Out of Stock",
                AvailableStock = checkResp.CurrentStock,
                RequestedQuantity = request.Quantity
            });
        }

        // ---------------------------------------------------------------------------------
        // 2. CALL INVENTORY SERVICE VIA GRPC UNARY RPC: DeductStock
        // ---------------------------------------------------------------------------------

        // [KYA KARTA HAI]: Protobuf deduction request build karta hai.
        var deductReq = new DeductStockRequest { Sku = request.Sku, Quantity = request.Quantity };

        // [KYA KARTA HAI]: gRPC stock deduction execute karta hai.
        var deductResp = await _inventoryClient.DeductStockAsync(deductReq);

        // [KYA KARTA HAI]: Deduction outcome verify karta hai.
        if (!deductResp.Success)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { Error = deductResp.Message });
        }

        // [KYA KARTA HAI]: Generated Order confirmation return karta hai.
        return Ok(new
        {
            OrderId = $"ORD-{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
            Status = "Confirmed",
            Sku = request.Sku,
            DeductedQuantity = request.Quantity,
            RemainingWarehouseStock = deductResp.RemainingStock
        });
    }
}

// [KYA KARTA HAI]: Checkout request DTO.
public record CheckoutRequest(string CustomerId, string Sku, int Quantity);

