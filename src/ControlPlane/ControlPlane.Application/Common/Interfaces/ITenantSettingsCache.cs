namespace ControlPlane.Application.Common.Interfaces;

public interface ITenantSettingsCache
{
    void Invalidate(Guid tenantId);
}
