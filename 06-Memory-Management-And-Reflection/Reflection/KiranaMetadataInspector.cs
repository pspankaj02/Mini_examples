using System.Reflection;

namespace KiranaHub.MemoryAndReflection.Reflection;

// =========================================================================================
// [CUSTOM ATTRIBUTE 1]:
// [TARGET]: Properties containing sensitive customer PII data.
// =========================================================================================
[AttributeUsage(AttributeTargets.Property)]
public class SensitiveDataAttribute : Attribute
{
    // [READ-WRITE PROPERTY]: Masking pattern string
    public string MaskWith { get; set; }

    // [CONSTRUCTOR]:
    public SensitiveDataAttribute(string maskWith = "***REDACTED***")
    {
        MaskWith = maskWith;
    }
}

// =========================================================================================
// [CUSTOM ATTRIBUTE 2]:
// [TARGET]: Classes requiring security audit logging.
// =========================================================================================
[AttributeUsage(AttributeTargets.Class)]
public class AuditLogAttribute : Attribute
{
    // [READONLY PROPERTY]: Audit categorization
    public string ActionCategory { get; }

    // [CONSTRUCTOR]:
    public AuditLogAttribute(string actionCategory)
    {
        ActionCategory = actionCategory;
    }
}

// =========================================================================================
// [DECORATED MODEL CLASS]:
// =========================================================================================
[AuditLog("CustomerBilling")]
public class CustomerBillingProfile
{
    // [REGULAR PROPERTY]:
    public string CustomerId { get; set; } = "CUST-880";

    // [DECORATED SENSITIVE PROPERTY 1]: Credit Card number masked
    [SensitiveData(MaskWith = "XXXX-XXXX-XXXX-1234")]
    public string CreditCardNumber { get; set; } = "4111-2222-3333-1234";

    // [DECORATED SENSITIVE PROPERTY 2]: UPI pin hash masked
    [SensitiveData(MaskWith = "SECRET")]
    public string UpiPinHash { get; set; } = "HASH_SECRET_777";

    // [REGULAR NUMERIC PROPERTY]:
    public decimal WalletBalance { get; set; } = 1500.50m;
}

// =========================================================================================
// [REFLECTION INSPECTOR ENGINE]:
// =========================================================================================
public static class KiranaMetadataInspector
{
    // [INSPECTION METHOD]: Dynamically reads metadata attributes and masks sensitive values
    public static Dictionary<string, string> GenerateAuditedSafeLog<T>(T instance) where T : class
    {
        // [DICTIONARY INITIALIZATION]:
        var output = new Dictionary<string, string>();

        // [TYPE METADATA EXTRACTION]:
        // CLR metadata table se type information retrieve karta hai
        Type type = typeof(T);

        // [CLASS-LEVEL ATTRIBUTE DISCOVERY]:
        var auditAttr = type.GetCustomAttribute<AuditLogAttribute>();
        output["_AuditCategory"] = auditAttr != null ? auditAttr.ActionCategory : "None";

        // [PROPERTY METADATA DISCOVERY]:
        PropertyInfo[] properties = type.GetProperties();

        // [FOREACH ENUMERATION OF PROPERTIES]:
        foreach (var prop in properties)
        {
            // [PROPERTY ATTRIBUTE CHECK]:
            var sensitiveAttr = prop.GetCustomAttribute<SensitiveDataAttribute>();

            // [DYNAMIC PROPERTY VALUE EXTRACTION]:
            object? rawValue = prop.GetValue(instance);

            // [MASKING CONDITIONAL]:
            if (sensitiveAttr != null)
            {
                // Assigns masked placeholder instead of real value
                output[prop.Name] = sensitiveAttr.MaskWith;
            }
            else
            {
                // Assigns stringified raw value
                output[prop.Name] = rawValue?.ToString() ?? "null";
            }
        }

        // [RETURN MASKED LOG DICTIONARY]:
        return output;
    }
}
