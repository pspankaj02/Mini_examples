using KiranaHub.UnitTests.Services;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace KiranaHub.UnitTests.Tests;

// =========================================================================================
// [TOPIC]: Unit Testing with xUnit & NSubstitute Mocking Framework
// [PURPOSE]: Test business isolation, boundary cases, exception simulation, and interaction verification.
// =========================================================================================

// [KYA KARTA HAI]: xUnit test fixture class jo OrderFulfillmentService ke sabhi scenarios test karti hai.
// [KYU ZAROORI HAI]: Automated regression suite jo code change hote hi bugs ko pakad leti hai.
// [KAISE CHALTA HAI]: xUnit har [Fact]/[Theory] ke liye test class ka naya instance banata hai (clean state isolation).
public class OrderFulfillmentServiceTests
{
    // [KYA KARTA HAI]: Mocked inventory dependency ka private field hold karta hai.
    // [KYU ZAROORI HAI]: Test method me setup aur interaction assertions ke liye reference chahiye.
    private readonly IInventoryService _mockInventory;

    // [KYA KARTA HAI]: Mocked payment gateway dependency ka private field hold karta hai.
    // [KYU ZAROORI HAI]: Fake payments and exception simulations configure karne ke liye.
    private readonly IPaymentGateway _mockPayment;

    // [KYA KARTA HAI]: Mocked notification service dependency ka private field hold karta hai.
    // [KYU ZAROORI HAI]: Verify karne ke liye ki confirmation messages trigger huye ya nahi.
    private readonly INotificationService _mockNotification;

    // [KYA KARTA HAI]: System Under Test (SUT) ka private field.
    // [KYU ZAROORI HAI]: Actual business class jiska behavior hum test kar rahe hain.
    private readonly OrderFulfillmentService _sut;

    // [KYA KARTA HAI]: Test constructor jo har test execution se pehle fresh mocks instantiate karta hai.
    // [KYU ZAROORI HAI]: Test isolation (no shared mutable state between tests).
    // [KAISE CHALTA HAI]: xUnit har test ke liye constructor run karta hai (like [SetUp] in NUnit).
    public OrderFulfillmentServiceTests()
    {
        // [KYA KARTA HAI]: NSubstitute se IInventoryService ka in-memory dynamic proxy mock create karta hai.
        // [KYU ZAROORI HAI]: Real database ya warehouse network call se bachne ke liye.
        // [KAISE CHALTA HAI]: Castle Core DynamicProxy runtime par IL emit karke interface implement karta hai.
        // [OUTPUT / SIDE EFFECT]: In-memory mock object ready to intercept method calls.
        _mockInventory = Substitute.For<IInventoryService>();

        // [KYA KARTA HAI]: NSubstitute se IPaymentGateway ka mock generate karta hai.
        // [KYU ZAROORI HAI]: Real bank ya external gateway par bina actual paise debit kiye test karna.
        _mockPayment = Substitute.For<IPaymentGateway>();

        // [KYA KARTA HAI]: NSubstitute se INotificationService ka mock generate karta hai.
        // [KYU ZAROORI HAI]: Actual SMS/WhatsApp messages transmit na hon test runs me.
        _mockNotification = Substitute.For<INotificationService>();

        // [KYA KARTA HAI]: SUT (OrderFulfillmentService) ko mocked dependencies ke sath initialize karta hai.
        // [KYU ZAROORI HAI]: Service Under Test ko isolated environment provide karna.
        _sut = new OrderFulfillmentService(_mockInventory, _mockPayment, _mockNotification);
    }

    // [KYA KARTA HAI]: xUnit attribute jo mark karta hai ki ye method ek independent, parameter-less test case hai.
    // [KYU ZAROORI HAI]: Test runner is method ko identify aur execute karta hai.
    [Fact]
    public async Task FulfillOrderAsync_WhenStockAndPaymentSucceed_ReturnsSuccessResult()
    {
        // ------------------------------------------------------------------------------------
        // ARRANGE (TEST PREREQUISITES SETUP)
        // ------------------------------------------------------------------------------------

        // [KYA KARTA HAI]: Test customer identifier define karta hai.
        // [KYU ZAROORI HAI]: Realistic customer context create karna.
        string customerId = "CUST-001";

        // [KYA KARTA HAI]: Test product SKU (Milk) define karta hai.
        // [KYU ZAROORI HAI]: Target catalog item specify karna.
        string sku = "MILK-01";

        // [KYA KARTA HAI]: Purchase quantity 2 units set karta hai.
        // [KYU ZAROORI HAI]: Valid positive integer requirement fulfill karna.
        int qty = 2;

        // [KYA KARTA HAI]: Unit price 33 INR set karta hai.
        // [KYU ZAROORI HAI]: Total price calculation (2 * 33 = 66) establish karna.
        decimal price = 33m;

        // [KYA KARTA HAI]: Mock inventory ko train karta hai ki jab MILK-01 with qty 2 pucha jaye toh 'true' (stock available) return kare.
        // [KYU ZAROORI HAI]: Method stubbing: predictable response simulate karna.
        // [KAISE CHALTA HAI]: NSubstitute internal dictionary me arguments aur return Task register karta hai.
        _mockInventory.HasSufficientStockAsync(sku, qty).Returns(Task.FromResult(true));

        // [KYA KARTA HAI]: Mock payment gateway ko train karta hai ki total 66m amount ke liye successful transaction ID return kare.
        // [KYU ZAROORI HAI]: Successful payment scenario simulate karna.
        _mockPayment.ProcessPaymentAsync(customerId, 66m).Returns(Task.FromResult("TXN-SUCCESS-777"));

        // ------------------------------------------------------------------------------------
        // ACT (EXECUTE SYSTEM UNDER TEST)
        // ------------------------------------------------------------------------------------

        // [KYA KARTA HAI]: SUT ka actual fulfillment method call karta hai arranged inputs ke sath.
        // [KYU ZAROORI HAI]: Core business workflow ko exercise karna.
        // [KAISE CHALTA HAI]: Service mock dependencies call karti hai aur result produce karti hai.
        // [OUTPUT / RETURN]: FulfillmentResult instance.
        var result = await _sut.FulfillOrderAsync(customerId, sku, qty, price);

        // ------------------------------------------------------------------------------------
        // ASSERT (VERIFY OUTCOMES & BEHAVIOR)
        // ------------------------------------------------------------------------------------

        // [KYA KARTA HAI]: Verify karta hai ki result.IsSuccess true hai.
        // [KYU ZAROORI HAI]: Confirm karna ki business flow successfully complete hua.
        Assert.True(result.IsSuccess);

        // [KYA KARTA HAI]: Check karta hai ki generated OrderId null nahi hai.
        // [KYU ZAROORI HAI]: Har successful order ke pass unique order ID hona lazmi hai.
        Assert.NotNull(result.OrderId);

        // [KYA KARTA HAI]: Verify karta hai ki koi error message return nahi hua.
        // [KYU ZAROORI HAI]: Success state me ErrorMessage null rehna chahiye.
        Assert.Null(result.ErrorMessage);

        // ------------------------------------------------------------------------------------
        // VERIFY MOCK INTERACTIONS (BEHAVIORAL VERIFICATION)
        // ------------------------------------------------------------------------------------

        // [KYA KARTA HAI]: Verify karta hai ki warehouse stock deduction method exactly 1 bar call hua right arguments ke sath.
        // [KYU ZAROORI HAI]: Ensure karna ki inventory update skip nahi hui aur double-deduct bhi nahi hui.
        // [KAISE CHALTA HAI]: NSubstitute call history inspect karta hai; mismatch par assertion exception deta hai.
        await _mockInventory.Received(1).DeductStockAsync(sku, qty);

        // [KYA KARTA HAI]: Verify karta hai ki customer ko notification confirmation message exactly 1 bar trigger hua with generated order ID.
        // [KYU ZAROORI HAI]: Communication SLA verification.
        await _mockNotification.Received(1).SendOrderConfirmationAsync(customerId, result.OrderId!);
    }

    // [KYA KARTA HAI]: Test case jo out-of-stock scenario verify karta hai.
    // [KYU ZAROORI HAI]: Negative path testing: ensure payment is NEVER triggered if stock is missing.
    [Fact]
    public async Task FulfillOrderAsync_WhenStockIsInsufficient_ReturnsFailureAndNeverChargesPayment()
    {
        // ------------------------------------------------------------------------------------
        // ARRANGE
        // ------------------------------------------------------------------------------------

        // [KYA KARTA HAI]: Test customer ID.
        string customerId = "CUST-002";

        // [KYA KARTA HAI]: Test product SKU (Atta).
        string sku = "ATTA-05";

        // [KYA KARTA HAI]: High quantity request (10 bags).
        int qty = 10;

        // [KYA KARTA HAI]: Unit price 245 INR.
        decimal price = 245m;

        // [KYA KARTA HAI]: Mock inventory ko train karta hai ki requested SKU out of stock hai (returns false).
        // [KYU ZAROORI HAI]: Out-of-stock condition simulate karna.
        _mockInventory.HasSufficientStockAsync(sku, qty).Returns(Task.FromResult(false));

        // ------------------------------------------------------------------------------------
        // ACT
        // ------------------------------------------------------------------------------------

        // [KYA KARTA HAI]: Method call karta hai out-of-stock scenario me.
        var result = await _sut.FulfillOrderAsync(customerId, sku, qty, price);

        // ------------------------------------------------------------------------------------
        // ASSERT
        // ------------------------------------------------------------------------------------

        // [KYA KARTA HAI]: Verify karta hai ki operation unsuccessful raha (IsSuccess == false).
        // [KYU ZAROORI HAI]: Business rule: out of stock orders must fail gracefully.
        Assert.False(result.IsSuccess);

        // [KYA KARTA HAI]: Check karta hai ki error message me "Insufficient stock" text clearly mentioned hai.
        // [KYU ZAROORI HAI]: User-facing error messaging informative honi chahiye.
        Assert.Contains("Insufficient stock", result.ErrorMessage);

        // ------------------------------------------------------------------------------------
        // VERIFY MOCK INTERACTIONS (ENSURE ZERO UNINTENDED SIDE EFFECTS)
        // ------------------------------------------------------------------------------------

        // [KYA KARTA HAI]: Assert karta hai ki PaymentGateway par ProcessPaymentAsync KABHI BHI call nahi hua.
        // [KYU ZAROORI HAI]: Critical financial rule: Agar item warehouse me nahi hai toh customer ka card charge nahi hona chahiye!
        // [KAISE CHALTA HAI]: NSubstitute DidNotReceive() method invocation history me check karta hai zero calls.
        await _mockPayment.DidNotReceive().ProcessPaymentAsync(Arg.Any<string>(), Arg.Any<decimal>());

        // [KYA KARTA HAI]: Assert karta hai ki stock deduction kabhi call nahi hua.
        // [KYU ZAROORI HAI]: Non-existent stock deduct nahi kiya ja sakta.
        await _mockInventory.DidNotReceive().DeductStockAsync(Arg.Any<string>(), Arg.Any<int>());

        // [KYA KARTA HAI]: Assert karta hai ki confirmation notification kabhi transmit nahi hua.
        // [KYU ZAROORI HAI]: Failed order par success email nahi jaani chahiye.
        await _mockNotification.DidNotReceive().SendOrderConfirmationAsync(Arg.Any<string>(), Arg.Any<string>());
    }

    // [KYA KARTA HAI]: Test case jo payment gateway crash/exception scenario ko test karta hai.
    // [KYU ZAROORI HAI]: Fault tolerance and transaction rollback safety test karna.
    [Fact]
    public async Task FulfillOrderAsync_WhenPaymentGatewayThrowsException_FailsAndDoesNotDeductInventory()
    {
        // ------------------------------------------------------------------------------------
        // ARRANGE
        // ------------------------------------------------------------------------------------

        // [KYA KARTA HAI]: Test customer ID.
        string customerId = "CUST-003";

        // [KYA KARTA HAI]: Test product SKU (Tea).
        string sku = "TEA-01";

        // [KYA KARTA HAI]: Purchase quantity.
        int qty = 1;

        // [KYA KARTA HAI]: Unit price.
        decimal price = 210m;

        // [KYA KARTA HAI]: Stock available simulate karta hai.
        _mockInventory.HasSufficientStockAsync(sku, qty).Returns(Task.FromResult(true));

        // [KYA KARTA HAI]: Payment gateway par network crash / timeout exception mock karta hai via ThrowsAsync.
        // [KYU ZAROORI HAI]: Third party bank failures test karna without relying on live downtime.
        _mockPayment.ProcessPaymentAsync(customerId, 210m).ThrowsAsync(new InvalidOperationException("Bank server timed out."));

        // ------------------------------------------------------------------------------------
        // ACT & ASSERT (EXCEPTION VERIFICATION)
        // ------------------------------------------------------------------------------------

        // [KYA KARTA HAI]: Assert karta hai ki service invocation InvalidOperationException throw karegi.
        // [KYU ZAROORI HAI]: Unhandled third party errors bubble up properly for retry or logging middlewares.
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.FulfillOrderAsync(customerId, sku, qty, price));

        // [KYA KARTA HAI]: Check karta hai ki exception message exact expected crash reason reflect karta hai.
        Assert.Equal("Bank server timed out.", ex.Message);

        // ------------------------------------------------------------------------------------
        // BEHAVIORAL SAFETY VERIFICATION
        // ------------------------------------------------------------------------------------

        // [KYA KARTA HAI]: Confirm karta hai ki warehouse stock KABHI BHI deduct nahi hua jab payment crash ho gayi.
        // [KYU ZAROORI HAI]: Prevent inventory loss when payment was never collected.
        await _mockInventory.DidNotReceive().DeductStockAsync(sku, qty);
    }

    // [KYA KARTA HAI]: xUnit Theory attribute jo data-driven parameterized tests chalata hai multiple inputs ke sath.
    // [KYU ZAROORI HAI]: Ek hi test method ke zariye multiple edge cases aur boundary invalid values test karna without code duplication.
    [Theory]
    // [KYA KARTA HAI]: Test case 1: Empty customer ID string pass karta hai.
    [InlineData("", "MILK-01", 1, 30.0)]
    // [KYA KARTA HAI]: Test case 2: Whitespace-only customer ID pass karta hai.
    [InlineData("   ", "MILK-01", 1, 30.0)]
    // [KYA KARTA HAI]: Test case 3: Zero quantity pass karta hai.
    [InlineData("CUST-1", "MILK-01", 0, 30.0)]
    // [KYA KARTA HAI]: Test case 4: Negative quantity (-5) pass karta hai.
    [InlineData("CUST-1", "MILK-01", -5, 30.0)]
    // [KYA KARTA HAI]: Test case 5: Zero price pass karta hai.
    [InlineData("CUST-1", "MILK-01", 1, 0.0)]
    // [KYA KARTA HAI]: Test case 6: Negative price (-10.0) pass karta hai.
    [InlineData("CUST-1", "MILK-01", 1, -10.0)]
    public async Task FulfillOrderAsync_WhenInputArgumentsAreInvalid_ThrowsArgumentException(
        string customerId, string sku, int qty, decimal price)
    {
        // ------------------------------------------------------------------------------------
        // ACT & ASSERT
        // ------------------------------------------------------------------------------------

        // [KYA KARTA HAI]: Assert karta hai ki invalid argument pass hone par method ArgumentException throw karta hai.
        // [KYU ZAROORI HAI]: Defensive programming verification: corrupted parameters never reach business pipeline.
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.FulfillOrderAsync(customerId, sku, qty, price));
    }
}
