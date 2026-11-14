using System;
using System.Diagnostics;
using System.Net.Http.Json;
using Grpc.Core;
using Grpc.Net.Client;
using KiranaHub.MicroservicesAndGateway.Protos;
using KiranaHub.MicroservicesAndGateway.Services;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Transforms;

// =========================================================================================
// [TOPIC]: Microservices, gRPC & YARP API Gateway Bootstrap & Verification Harness
// [PURPOSE]: gRPC server, gRPC client, REST microservice, and YARP Reverse Proxy routing.
// =========================================================================================

// [KYA KARTA HAI]: Plaintext HTTP/2 support enable karta hai for gRPC communication without TLS.
AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

// [KYA KARTA HAI]: WebApplicationBuilder instance initialize karta hai.
var builder = WebApplication.CreateBuilder(args);

// [KYA KARTA HAI]: Console logging configure karta hai.
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// [KYA KARTA HAI]: Kestrel web server par do alag ports bind karta hai:
// Port 5225: External REST APIs & YARP Gateway (HTTP/1.1)
// Port 5226: Internal Microservice gRPC Binary Traffic (HTTP/2 H2C Plaintext)
builder.WebHost.ConfigureKestrel(options =>
{
    // [KYA KARTA HAI]: Port 5225 par HTTP/1.1 protocol listen karta hai.
    options.ListenLocalhost(5225, o =>
    {
        o.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1;
    });

    // [KYA KARTA HAI]: Port 5226 par pure HTTP/2 protocol listen karta hai for binary gRPC calls.
    options.ListenLocalhost(5226, o =>
    {
        o.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2;
    });
});

// [KYA KARTA HAI]: Framework gRPC services IoC container me register karta hai.
builder.Services.AddGrpc();

// [KYA KARTA HAI]: MVC API Controllers enable karta hai for Orders REST service.
builder.Services.AddControllers();

// [KYA KARTA HAI]: Strongly-typed gRPC client register karta hai pointing to internal gRPC port 5226.
builder.Services.AddGrpcClient<InventoryGrpcService.InventoryGrpcServiceClient>(options =>
{
    options.Address = new Uri("http://localhost:5226");
});

// ----------------------------------------------------------------------------------------
// 1. YARP REVERSE PROXY (API GATEWAY) CONFIGURATION
// ----------------------------------------------------------------------------------------

// [KYA KARTA HAI]: YARP Gateway routes in-memory programmatically define karta hai.
var routes = new[]
{
    new RouteConfig
    {
        RouteId = "orders-route",
        ClusterId = "orders-cluster",
        Match = new RouteMatch
        {
            // [KYA KARTA HAI]: Gateway endpoint prefix match karta hai: /gateway/orders/...
            Path = "/gateway/orders/{**catch-all}"
        }
    }.WithTransformPathRemovePrefix("/gateway") // Transforms /gateway/orders/checkout -> /orders/checkout
};

// [KYA KARTA HAI]: YARP Gateway destination clusters define karta hai.
var clusters = new[]
{
    new ClusterConfig
    {
        ClusterId = "orders-cluster",
        Destinations = new Dictionary<string, DestinationConfig>
        {
            ["dest1"] = new DestinationConfig { Address = "http://localhost:5225/api/" }
        }
    }
};

// [KYA KARTA HAI]: YARP Reverse Proxy services register karta hai in-memory configuration ke sath.
builder.Services.AddReverseProxy()
                .LoadFromMemory(routes, clusters);

// [KYA KARTA HAI]: WebApplication pipeline object build karta hai.
var app = builder.Build();

// ----------------------------------------------------------------------------------------
// 2. MAP ENDPOINTS & GATEWAY REVERSE PROXY
// ----------------------------------------------------------------------------------------

// [KYA KARTA HAI]: gRPC Inventory service endpoint map karta hai HTTP/2 pipeline me.
app.MapGrpcService<InventoryGrpcServiceImpl>();

// [KYA KARTA HAI]: REST API Controllers map karta hai.
app.MapControllers();

// [KYA KARTA HAI]: YARP Reverse Proxy pipeline middleware activate karta hai.
app.MapReverseProxy();

// [KYA KARTA HAI]: Server asynchronously start karta hai.
await app.StartAsync();

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.WriteLine("==========================================================================");
Console.WriteLine("   KIRANAHUB - 14: MICROSERVICES, GRPC & YARP API GATEWAY DEMONSTRATION   ");
Console.WriteLine("==========================================================================\n");

// ----------------------------------------------------------------------------------------
// 3. TEST 1: GRPC UNARY RPC EXECUTION (CheckStock)
// ----------------------------------------------------------------------------------------
Console.WriteLine("--> [Step 1] Invoking gRPC Unary RPC: InventoryGrpcService.CheckStock");

// [KYA KARTA HAI]: gRPC channel create karta hai for binary HTTP/2 communication on port 5226.
using var grpcChannel = GrpcChannel.ForAddress("http://localhost:5226");
var inventoryClient = new InventoryGrpcService.InventoryGrpcServiceClient(grpcChannel);

// [KYA KARTA HAI]: CheckStock Unary RPC execute karta hai.
var checkStockResult = await inventoryClient.CheckStockAsync(new CheckStockRequest
{
    Sku = "MILK-01",
    RequestedQuantity = 5
});

Console.WriteLine($"gRPC CheckStock Response:");
Console.WriteLine($"   SKU           : {checkStockResult.Sku}");
Console.WriteLine($"   Is Available  : {checkStockResult.IsAvailable}");
Console.WriteLine($"   Current Stock : {checkStockResult.CurrentStock}");

// [KYA KARTA HAI]: Assert karta hai initial stock 50 units available hai.
Debug.Assert(checkStockResult.IsAvailable && checkStockResult.CurrentStock == 50);
Console.WriteLine("[PASS] gRPC Unary RPC call verified successfully!\n");

// ----------------------------------------------------------------------------------------
// 4. TEST 2: GRPC SERVER STREAMING RPC (SubscribeStockStream)
// ----------------------------------------------------------------------------------------
Console.WriteLine("--> [Step 2] Invoking gRPC Server Streaming RPC: SubscribeStockStream");

// [KYA KARTA HAI]: Server streaming call initiate karta hai.
using var streamCall = inventoryClient.SubscribeStockStream(new SubscribeStockStreamRequest
{
    Category = "Dairy"
});

int receivedStreamFrames = 0;

// [KYA KARTA HAI]: Asynchronous stream reader par while loop chala kar incoming stream frames consume karta hai.
while (await streamCall.ResponseStream.MoveNext(CancellationToken.None))
{
    receivedStreamFrames++;
    var currentFrame = streamCall.ResponseStream.Current;
    Console.WriteLine($"   [STREAM FRAME #{receivedStreamFrames}] SKU: {currentFrame.Sku} | Stock: {currentFrame.CurrentStock} | Reason: '{currentFrame.Reason}'");
}

// [KYA KARTA HAI]: Assert karta hai exactly 3 frames receive huye.
Debug.Assert(receivedStreamFrames == 3, "Did not receive expected 3 streaming frames!");
Console.WriteLine("[PASS] gRPC Server Streaming RPC verified successfully!\n");

// ----------------------------------------------------------------------------------------
// 5. TEST 3: YARP API GATEWAY REVERSE PROXY ROUTING
// ----------------------------------------------------------------------------------------
Console.WriteLine("--> [Step 3] Testing YARP API Gateway Routing: /gateway/orders/checkout -> Orders Microservice");

// [KYA KARTA HAI]: HttpClient instantiate karta hai API Gateway hit karne ke liye.
using var httpClient = new HttpClient();

// [KYA KARTA HAI]: Checkout payload create karta hai.
var checkoutPayload = new CheckoutRequest("CUST-GATEWAY-1", "MILK-01", 2);

// [KYA KARTA HAI]: YARP API Gateway route hit karta hai (/gateway/orders/checkout).
// [KAISE CHALTA HAI]: YARP request intercept karta hai, prefix strip karta hai, Orders microservice ko forward karta hai, Orders microservice gRPC DeductStock call karti hai, aur response reverse proxy se wapas client ko milta hai!
var gatewayResponse = await httpClient.PostAsJsonAsync("http://localhost:5225/gateway/orders/checkout", checkoutPayload);

Console.WriteLine($"YARP API Gateway Response Status: {gatewayResponse.StatusCode}");
Debug.Assert(gatewayResponse.IsSuccessStatusCode, "YARP API Gateway failed to proxy request!");

// [KYA KARTA HAI]: Response JSON content read karta hai.
var responseJson = await gatewayResponse.Content.ReadAsStringAsync();
Console.WriteLine($"Gateway Response Body: {responseJson}");

// [KYA KARTA HAI]: Verify karta hai stock 50 me se 2 deduct hokar 48 reh gaya.
Debug.Assert(responseJson.Contains("\"remainingWarehouseStock\":48"));
Console.WriteLine("[PASS] YARP API Gateway proxying to microservices verified!\n");

Console.WriteLine("==========================================================================");
Console.WriteLine("   [SUCCESS] ALL 14-MICROSERVICES-GRPC-AND-GATEWAY TESTS PASSED!          ");
Console.WriteLine("==========================================================================");

// [KYA KARTA HAI]: Clean server shutdown.
await app.StopAsync();
