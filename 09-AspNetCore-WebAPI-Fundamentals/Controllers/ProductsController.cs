using KiranaHub.WebApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace KiranaHub.WebApi.Controllers;

// =========================================================================================
// [TOPIC]: ASP.NET Core Controller-Based RESTful Web API
// [PURPOSE]: Implementation of HTTP REST conventions (GET, POST, DELETE, ProblemDetails, Status Codes).
// =========================================================================================

// [KYA KARTA HAI]: Marks class as an API Controller (enables automatic 400 validation, attribute routing, and [FromBody] inference).
// [KYU ZAROORI HAI]: Boilerplate ModelState.IsValid code likhne se bachata hai; invalid DTOs par framework khud 400 return karta hai.
[ApiController]
// [KYA KARTA HAI]: Route prefix define karta hai ("api/products").
// [KYU ZAROORI HAI]: Clean standard REST resource URI hierarchy provide karna.
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    // [KYA KARTA HAI]: In-memory products catalog store karta hai.
    // [KYU ZAROORI HAI]: Fast testing without external database dependencies.
    // [KAISE CHALTA HAI]: Static list heap memory me persistent rehti hai across all HTTP requests.
    private static readonly List<ProductReadDto> Catalog = new()
    {
        // [KYA KARTA HAI]: Sample Milk product seed karta hai.
        new(1, "AMUL-MILK-500", "Amul Taaza Milk 500ml", "Dairy", 33m, 50),
        // [KYA KARTA HAI]: Sample Butter product seed karta hai.
        new(2, "AMUL-BTR-100", "Amul Butter 100g", "Dairy", 58m, 30),
        // [KYA KARTA HAI]: Sample Atta product seed karta hai.
        new(3, "ATTA-5KG", "Aashirvaad Atta 5kg", "Grains", 245m, 15)
    };

    // [KYA KARTA HAI]: GET /api/products endpoint handle karta hai.
    // [KYU ZAROORI HAI]: Client ko complete catalog list provide karna.
    // [OUTPUT / RETURN]: HTTP 200 OK with List<ProductReadDto>.
    [HttpGet]
    public ActionResult<IEnumerable<ProductReadDto>> GetAll()
    {
        // [KYA KARTA HAI]: Catalog list ko HTTP 200 OK response ke sath return karta hai.
        return Ok(Catalog);
    }

    // [KYA KARTA HAI]: GET /api/products/{id} endpoint handle karta hai with integer route constraint.
    // [KYU ZAROORI HAI]: Specific product lookup by ID.
    // [OUTPUT / RETURN]: 200 OK or 404 Not Found.
    [HttpGet("{id:int}")]
    public ActionResult<ProductReadDto> GetById(int id)
    {
        // [KYA KARTA HAI]: Catalog me product match karta hai by ID.
        var product = Catalog.FirstOrDefault(x => x.Id == id);

        // [KYA KARTA HAI]: Check karta hai ki product exist karta hai ya nahi.
        if (product == null)
        {
            // [KYA KARTA HAI]: 404 Not Found status code return karta hai with RFC 7807 ProblemDetails object.
            // [KYU ZAROORI HAI]: Standardized error communication.
            return NotFound(new ProblemDetails
            {
                Title = "Product Not Found",
                Detail = $"Product with ID {id} does not exist in KiranaHub catalog.",
                Status = StatusCodes.Status404NotFound
            });
        }

        // [KYA KARTA HAI]: Matching product return karta hai with 200 OK.
        return Ok(product);
    }

    // [KYA KARTA HAI]: POST /api/products endpoint handle karta hai to create a new product.
    // [KYU ZAROORI HAI]: Catalog me naya product add karna.
    // [OUTPUT / RETURN]: 201 Created with Location header or 409 Conflict.
    [HttpPost]
    public ActionResult<ProductReadDto> Create([FromBody] ProductCreateDto input)
    {
        // [KYA KARTA HAI]: Check karta hai ki SKU pehle se exist toh nahi karta.
        // [KYU ZAROORI HAI]: Duplicate inventory SKU prevention.
        if (Catalog.Any(x => x.Sku.Equals(input.Sku, StringComparison.OrdinalIgnoreCase)))
        {
            // [KYA KARTA HAI]: HTTP 409 Conflict return karta hai agar duplicate SKU paya jaye.
            return Conflict(new ProblemDetails
            {
                Title = "Duplicate SKU Conflict",
                Detail = $"Product with SKU '{input.Sku}' already exists.",
                Status = StatusCodes.Status409Conflict
            });
        }

        // [KYA KARTA HAI]: Naya sequential ID generate karta hai.
        int newId = Catalog.Count > 0 ? Catalog.Max(x => x.Id) + 1 : 1;

        // [KYA KARTA HAI]: DTO input se naya immutable ProductReadDto object create karta hai.
        var created = new ProductReadDto(newId, input.Sku, input.Name, input.Category, input.Price, input.Stock);

        // [KYA KARTA HAI]: Catalog list me naya item add karta hai.
        Catalog.Add(created);

        // [KYA KARTA HAI]: HTTP 201 Created response return karta hai with "Location" response header.
        // [KYU ZAROORI HAI]: REST standard: Client ko batata hai ki naya resource kis URL par fetch kiya ja sakta hai.
        // [OUTPUT / RETURN]: 201 Created with Location header pointing to /api/products/{newId}.
        return CreatedAtAction(nameof(GetById), new { id = newId }, created);
    }

    // [KYA KARTA HAI]: DELETE /api/products/{id} endpoint handle karta hai.
    // [KYU ZAROORI HAI]: Catalog se product remove karna.
    // [OUTPUT / RETURN]: 204 NoContent or 404 NotFound.
    [HttpDelete("{id:int}")]
    public IActionResult Delete(int id)
    {
        // [KYA KARTA HAI]: Product find karta hai by ID.
        var product = Catalog.FirstOrDefault(x => x.Id == id);

        // [KYA KARTA HAI]: Agar product missing ho toh 404 return karta hai.
        if (product == null) return NotFound();

        // [KYA KARTA HAI]: Catalog list se product delete karta hai.
        Catalog.Remove(product);

        // [KYA KARTA HAI]: HTTP 204 No Content status code return karta hai.
        // [KYU ZAROORI HAI]: REST standard: Resource successfully deleted, no payload body to return.
        return NoContent();
    }

    // [KYA KARTA HAI]: Simulated crash endpoint for middleware testing.
    // [KYU ZAROORI HAI]: Verify karna ki GlobalExceptionHandlerMiddleware 500 error catch karta hai.
    [HttpGet("simulate-server-crash")]
    public IActionResult SimulateCrash()
    {
        // [KYA KARTA HAI]: Deliberately unhandled exception throw karta hai.
        // [OUTPUT / SIDE EFFECT]: Middleware catches this and returns 500 ProblemDetails.
        throw new InvalidOperationException("Simulated catastrophic database failure for middleware testing!");
    }
}
