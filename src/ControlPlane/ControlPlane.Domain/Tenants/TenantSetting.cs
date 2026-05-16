using Api.Domain.Common;

namespace ControlPlane.Domain.Tenants;

public class TenantSetting : Entity
{
    public Guid TenantId { get; private set; }
    public string Key { get; private set; } = "";
    public string Value { get; private set; } = "";

    private TenantSetting() : base() { }

    public static TenantSetting Create(Guid tenantId, string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new DomainException("Setting key cannot be empty.");

        return new TenantSetting { TenantId = tenantId, Key = key.Trim(), Value = value ?? "" };
    }

    internal void UpdateValue(string value) => Value = value ?? "";
}
