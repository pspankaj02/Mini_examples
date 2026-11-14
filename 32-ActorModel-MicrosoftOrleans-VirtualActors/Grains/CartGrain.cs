namespace KiranaHub.VirtualActors.Grains;

using KiranaHub.VirtualActors.Runtime;

// [KYA KARTA HAI]: ICartGrain ka concrete virtual actor implementation provide karta hai.
// [KYU ZAROORI HAI]: Har customer ke cart transactions ko in-memory isolate karna taaki high concurrency me zero deadlocks hon.
// [KAISE CHALTA HAI]: VirtualGrainBase ke EnqueueTurnAsync se turns serialize karta hai aur private dictionary me items maintain karta hai.
// [OUTPUT / SIDE EFFECT]: Thread-safe cart mutation, real-time pricing recalculations.
// [VARIATIONS TO TRY]: Redis cache snapshot persistence enable kar sakte hain on each turn.
public class CartGrain : VirtualGrainBase, ICartGrain
{
    // [KYA KARTA HAI]: Cart line items ko productId ke against map me store karta hai.
    // [KYU ZAROORI HAI]: Fast $O(1)$ lookups aur updates ensure karna.
    // [KAISE CHALTA HAI]: Dictionary<string, CartLineItem> collection.
    // [OUTPUT / SIDE EFFECT]: Internal item storage.
    // [VARIATIONS TO TRY]: Presorted ordered dictionary use karein.
    private readonly Dictionary<string, CartLineItem> _items = new(StringComparer.OrdinalIgnoreCase);

    // [KYA KARTA HAI]: Applied coupon code store karta hai.
    // [KYU ZAROORI HAI]: Promotional pricing track karna.
    // [KAISE CHALTA HAI]: Nullable string field.
    // [OUTPUT / SIDE EFFECT]: Current coupon code.
    // [VARIATIONS TO TRY]: Multiple stacked coupon codes support karein.
    private string? _couponCode;

    // [KYA KARTA HAI]: Applied coupon discount percentage hold karta hai.
    // [KYU ZAROORI HAI]: Cart recalculation me percentage apply karna.
    // [KAISE CHALTA HAI]: Decimal percentage (e.g. 10.0m for 10%).
    // [OUTPUT / SIDE EFFECT]: Discount rate.
    // [VARIATIONS TO TRY]: Flat discount amount (e.g. ₹50 off) support karein.
    private decimal _discountPct = 0m;

    // [KYA KARTA HAI]: Cart checked out status flag hold karta hai.
    // [KYU ZAROORI HAI]: Ordered cart me further mutations block karna.
    // [KAISE CHALTA HAI]: Boolean flag.
    // [OUTPUT / SIDE EFFECT]: Checkout indicator.
    // [VARIATIONS TO TRY]: Abandoned state flag add karein.
    private bool _isCheckedOut = false;

    // [KYA KARTA HAI]: Cart me grocery item add ya update karta hai.
    // [KYU ZAROORI HAI]: Customer ke add-to-cart clicks process karna.
    // [KAISE CHALTA HAI]: Agar item already exist karta hai toh quantity add karta hai, warna new item insert karta hai.
    // [OUTPUT / SIDE EFFECT]: Cart items updated.
    // [VARIATIONS TO TRY]: Max quantity per item (e.g. 10 kg limit) check karein.
    public Task AddItemAsync(string productId, string name, decimal price, int quantity)
    {
        // [KYA KARTA HAI]: Turn queue me addition serialize karta hai.
        // [KYU ZAROORI HAI]: Rapid double-click se hone wale race conditions ko prevent karna.
        // [KAISE CHALTA HAI]: EnqueueTurnAsync lambda.
        // [OUTPUT / SIDE EFFECT]: Atomic addition.
        // [VARIATIONS TO TRY]: Dark-store inventory stock reservation call karein.
        return EnqueueTurnAsync(() =>
        {
            // [KYA KARTA HAI]: ProductId check karta hai.
            // [KYU ZAROORI HAI]: Empty product IDs allow nahi hone chahiye.
            // [KAISE CHALTA HAI]: ThrowIfNullOrWhiteSpace.
            // [OUTPUT / SIDE EFFECT]: Exception on invalid ID.
            // [VARIATIONS TO TRY]: GUID validation lagayein.
            ArgumentException.ThrowIfNullOrWhiteSpace(productId);

            // [KYA KARTA HAI]: Quantity check karta hai.
            // [KYU ZAROORI HAI]: Zero ya negative quantity invalid hai.
            // [KAISE CHALTA HAI]: ArgumentOutOfRangeException check.
            // [OUTPUT / SIDE EFFECT]: Exception if quantity <= 0.
            // [VARIATIONS TO TRY]: Quantity <= 0 aane par RemoveItemAsync delegate karein.
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);

            // [KYA KARTA HAI]: Checked out cart protection check karta hai.
            // [KYU ZAROORI HAI]: Ordered cart modify nahi hona chahiye.
            // [KAISE CHALTA HAI]: Boolean check.
            // [OUTPUT / SIDE EFFECT]: InvalidOperationException if already checked out.
            // [VARIATIONS TO TRY]: Naya cart automatically instantiate karein.
            if (_isCheckedOut)
            {
                throw new InvalidOperationException("Cannot modify an already checked-out cart.");
            }

            // [KYA KARTA HAI]: Check karta hai kya item pehle se cart me hai.
            // [KYU ZAROORI HAI]: Existing quantity me new quantity merge karna.
            // [KAISE CHALTA HAI]: TryGetValue lookup.
            // [OUTPUT / SIDE EFFECT]: Found existing item ya false.
            // [VARIATIONS TO TRY]: Price change warning trigger karein agar price update hua ho.
            if (_items.TryGetValue(productId, out var existing))
            {
                // [KYA KARTA HAI]: Existing item ki quantity increment karta hai.
                // [KYU ZAROORI HAI]: Incremental updates support karna.
                // [KAISE CHALTA HAI]: Record 'with' expression se new immutable item assign karta hai.
                // [OUTPUT / SIDE EFFECT]: Quantity increased.
                // [VARIATIONS TO TRY]: Max quantity cap enforce karein.
                _items[productId] = existing with { Quantity = existing.Quantity + quantity };
            }
            else
            {
                // [KYA KARTA HAI]: Naya line item dictionary me add karta hai.
                // [KYU ZAROORI HAI]: Fresh product cart entry banana.
                // [KAISE CHALTA HAI]: New CartLineItem instantiation and dictionary set.
                // [OUTPUT / SIDE EFFECT]: Item inserted.
                // [VARIATIONS TO TRY]: Perishability tag attach karein.
                _items[productId] = new CartLineItem(productId, name, price, quantity);
            }

            // [KYA KARTA HAI]: CompletedTask return.
            // [KYU ZAROORI HAI]: Async signature fulfillment.
            // [KAISE CHALTA HAI]: Return Task.CompletedTask.
            // [OUTPUT / SIDE EFFECT]: Done.
            // [VARIATIONS TO TRY]: Cart updated event stream par push karein.
            return Task.CompletedTask;
        });
    }

    // [KYA KARTA HAI]: Cart se item remove karta hai.
    // [KYU ZAROORI HAI]: Item deletion customer action handle karna.
    // [KAISE CHALTA HAI]: Dictionary.Remove se key delete karta hai.
    // [OUTPUT / SIDE EFFECT]: Item removed.
    // [VARIATIONS TO TRY]: Decrement quantity by 1 agar quantity > 1 ho.
    public Task RemoveItemAsync(string productId)
    {
        // [KYA KARTA HAI]: Turn queue me safe removal schedule karta hai.
        // [KYU ZAROORI HAI]: Concurrent delete aur update me conflict na ho.
        // [KAISE CHALTA HAI]: EnqueueTurnAsync.
        // [OUTPUT / SIDE EFFECT]: Atomic removal.
        // [VARIATIONS TO TRY]: Event emit karein.
        return EnqueueTurnAsync(() =>
        {
            // [KYA KARTA HAI]: Checked out state check.
            // [KYU ZAROORI HAI]: Closed cart immutable honi chahiye.
            // [KAISE CHALTA HAI]: Boolean inspection.
            // [OUTPUT / SIDE EFFECT]: Exception if locked.
            // [VARIATIONS TO TRY]: Return boolean success indicator.
            if (_isCheckedOut)
            {
                throw new InvalidOperationException("Cannot modify an already checked-out cart.");
            }

            // [KYA KARTA HAI]: Dictionary se product remove karta hai.
            // [KYU ZAROORI HAI]: Key deletion.
            // [KAISE CHALTA HAI]: _items.Remove(productId).
            // [OUTPUT / SIDE EFFECT]: Dictionary key removed.
            // [VARIATIONS TO TRY]: Return false if item wasn't present.
            _items.Remove(productId);

            // [KYA KARTA HAI]: CompletedTask return.
            // [KYU ZAROORI HAI]: Async compliance.
            // [KAISE CHALTA HAI]: Return.
            // [OUTPUT / SIDE EFFECT]: Done.
            // [VARIATIONS TO TRY]: Recalculate totals immediately.
            return Task.CompletedTask;
        });
    }

    // [KYA KARTA HAI]: Discount coupon code apply karta hai.
    // [KYU ZAROORI HAI]: Promotional discounts support karna.
    // [KAISE CHALTA HAI]: Percentage clamp karke fields me store karta hai.
    // [OUTPUT / SIDE EFFECT]: Coupon applied.
    // [VARIATIONS TO TRY]: Coupon validation service call karein.
    public Task ApplyCouponAsync(string couponCode, decimal discountPct)
    {
        // [KYA KARTA HAI]: Turn queue me coupon application serialize karta hai.
        // [KYU ZAROORI HAI]: Thread-safe coupon application.
        // [KAISE CHALTA HAI]: EnqueueTurnAsync.
        // [OUTPUT / SIDE EFFECT]: Atomic coupon state update.
        // [VARIATIONS TO TRY]: Max discount cap (e.g. up to ₹150) lagayein.
        return EnqueueTurnAsync(() =>
        {
            // [KYA KARTA HAI]: Checked out check.
            // [KYU ZAROORI HAI]: Post-order modification disallowed.
            // [KAISE CHALTA HAI]: Boolean check.
            // [OUTPUT / SIDE EFFECT]: Exception if true.
            // [VARIATIONS TO TRY]: Custom domain exception throw karein.
            if (_isCheckedOut)
            {
                throw new InvalidOperationException("Cannot apply coupon to an already checked-out cart.");
            }

            // [KYA KARTA HAI]: Coupon code upper-case store karta hai.
            // [KYU ZAROORI HAI]: Case-insensitive coupon tracking.
            // [KAISE CHALTA HAI]: ToUpperInvariant().
            // [OUTPUT / SIDE EFFECT]: _couponCode updated.
            // [VARIATIONS TO TRY]: Regex alphanumeric validation lagayein.
            _couponCode = couponCode.Trim().ToUpperInvariant();

            // [KYA KARTA HAI]: Discount percentage clamp karta hai between 0% aur 50%.
            // [KYU ZAROORI HAI]: Excessive discounts ya negative discount bugs prevent karna.
            // [KAISE CHALTA HAI]: Math.Clamp(discountPct, 0m, 50m).
            // [OUTPUT / SIDE EFFECT]: _discountPct set.
            // [VARIATIONS TO TRY]: VIP customer tier ke liye 70% allow karein.
            _discountPct = Math.Clamp(discountPct, 0m, 50m);

            // [KYA KARTA HAI]: CompletedTask return.
            // [KYU ZAROORI HAI]: Async fulfillment.
            // [KAISE CHALTA HAI]: Return.
            // [OUTPUT / SIDE EFFECT]: Done.
            // [VARIATIONS TO TRY]: Audit log emit karein.
            return Task.CompletedTask;
        });
    }

    // [KYA KARTA HAI]: Cart ka complete financial calculation snapshot return karta hai.
    // [KYU ZAROORI HAI]: UI aur payment gateways ko calculated total amount chahiye hota hai.
    // [KAISE CHALTA HAI]: Items sum, coupon discount, 5% GST tax, aur final net payable compute karta hai.
    // [OUTPUT / SIDE EFFECT]: CartState record.
    // [VARIATIONS TO TRY]: Packaging charges ya delivery fee shamil karein.
    public Task<CartState> GetCartAsync()
    {
        // [KYA KARTA HAI]: Turn queue me read serialize karta hai.
        // [KYU ZAROORI HAI]: Safe read without dirty reads or half-mutated collections.
        // [KAISE CHALTA HAI]: EnqueueTurnAsync returning CartState.
        // [OUTPUT / SIDE EFFECT]: Thread-safe CartState.
        // [VARIATIONS TO TRY]: Direct caching of calculated totals.
        return EnqueueTurnAsync(() =>
        {
            // [KYA KARTA HAI]: Subtotal compute karta hai across all line items.
            // [KYU ZAROORI HAI]: Base financial sum calculate karna.
            // [KAISE CHALTA HAI]: LINQ sum over LineTotal.
            // [OUTPUT / SIDE EFFECT]: Subtotal decimal.
            // [VARIATIONS TO TRY]: Fast foreach loop for zero heap allocation.
            decimal subtotal = 0m;
            foreach (var item in _items.Values)
            {
                subtotal += item.LineTotal;
            }

            // [KYA KARTA HAI]: Discount amount calculate karta hai.
            // [KYU ZAROORI HAI]: Promotional deduction determine karna.
            // [KAISE CHALTA HAI]: Math.Round(subtotal * (_discountPct / 100m), 2).
            // [OUTPUT / SIDE EFFECT]: Discount decimal value.
            // [VARIATIONS TO TRY]: Flat discount if greater than percentage.
            decimal discount = Math.Round(subtotal * (_discountPct / 100m), 2);

            // [KYA KARTA HAI]: Discounted amount calculate karta hai.
            // [KYU ZAROORI HAI]: Tax base value nikalna.
            // [KAISE CHALTA HAI]: subtotal - discount.
            // [OUTPUT / SIDE EFFECT]: Taxable amount.
            // [VARIATIONS TO TRY]: Minimum 0 floor enforce karein.
            decimal taxableAmount = Math.Max(0m, subtotal - discount);

            // [KYA KARTA HAI]: GST tax (5%) calculate karta hai.
            // [KYU ZAROORI HAI]: Government statutory tax compliance.
            // [KAISE CHALTA HAI]: 5% of taxable amount rounded to 2 decimals.
            // [OUTPUT / SIDE EFFECT]: Tax decimal value.
            // [VARIATIONS TO TRY]: Product category wise differentiated GST (0% fresh, 12% packaged).
            decimal tax = Math.Round(taxableAmount * 0.05m, 2);

            // [KYA KARTA HAI]: Final net payable amount calculate karta hai.
            // [KYU ZAROORI HAI]: Customer charging amount determine karna.
            // [KAISE CHALTA HAI]: taxableAmount + tax.
            // [OUTPUT / SIDE EFFECT]: Net payable final amount.
            // [VARIATIONS TO TRY]: Cash on delivery surcharge add karein.
            decimal netPayable = taxableAmount + tax;

            // [KYA KARTA HAI]: Immutable snapshot record construct karta hai.
            // [KYU ZAROORI HAI]: Thread-safe encapsulation.
            // [KAISE CHALTA HAI]: Record instantiation with cloned items list.
            // [OUTPUT / SIDE EFFECT]: CartState instance.
            // [VARIATIONS TO TRY]: ReadOnlyCollection wrapper use karein.
            var state = new CartState(
                CustomerId: GrainId,
                Items: _items.Values.ToList().AsReadOnly(),
                Subtotal: subtotal,
                CouponCode: _couponCode,
                DiscountAmount: discount,
                TaxAmount: tax,
                NetPayable: netPayable,
                IsCheckedOut: _isCheckedOut,
                LastUpdatedUtc: LastAccessedUtc);

            // [KYA KARTA HAI]: Return task from result.
            // [KYU ZAROORI HAI]: Async fulfillment.
            // [KAISE CHALTA HAI]: Task.FromResult(state).
            // [OUTPUT / SIDE EFFECT]: CartState task.
            // [VARIATIONS TO TRY]: ValueTask use karein.
            return Task.FromResult(state);
        });
    }

    // [KYA KARTA HAI]: Cart checkout complete karta hai.
    // [KYU ZAROORI HAI]: Final order transition ke baad cart modify na hone dena.
    // [KAISE CHALTA HAI]: IsCheckedOut flag set karta hai.
    // [OUTPUT / SIDE EFFECT]: Cart becomes read-only.
    // [VARIATIONS TO TRY]: In-memory clear karke new session ID generate karein.
    public Task CheckoutAsync()
    {
        // [KYA KARTA HAI]: Turn queue me checkout serialize karta hai.
        // [KYU ZAROORI HAI]: Atomic state change.
        // [KAISE CHALTA HAI]: EnqueueTurnAsync.
        // [OUTPUT / SIDE EFFECT]: Checkout applied.
        // [VARIATIONS TO TRY]: Event emit karein.
        return EnqueueTurnAsync(() =>
        {
            // [KYA KARTA HAI]: Empty cart checkout block karta hai.
            // [KYU ZAROORI HAI]: Khali basket checkout nahi honi chahiye.
            // [KAISE CHALTA HAI]: _items.Count == 0 check.
            // [OUTPUT / SIDE EFFECT]: InvalidOperationException if empty.
            // [VARIATIONS TO TRY]: Minimum ₹99 order value validation lagayein.
            if (_items.Count == 0)
            {
                throw new InvalidOperationException("Cannot checkout an empty cart.");
            }

            // [KYA KARTA HAI]: IsCheckedOut flag true karta hai.
            // [KYU ZAROORI HAI]: Cart freeze karna.
            // [KAISE CHALTA HAI]: Boolean set.
            // [OUTPUT / SIDE EFFECT]: _isCheckedOut = true.
            // [VARIATIONS TO TRY]: Order ID reference store karein.
            _isCheckedOut = true;

            // [KYA KARTA HAI]: CompletedTask return.
            // [KYU ZAROORI HAI]: Async completion.
            // [KAISE CHALTA HAI]: Return.
            // [OUTPUT / SIDE EFFECT]: Done.
            // [VARIATIONS TO TRY]: Flush to database.
            return Task.CompletedTask;
        });
    }
}
