using Api.Domain.Common;

namespace Parties.Domain.Parties;

public class PartyRole : Entity
{
    public PartyRoleType RoleType { get; private set; }
    public PartyRoleStatus Status { get; private set; }
    public DateTime ActivatedAt { get; private set; }
    public DateTime? DeactivatedAt { get; private set; }
    public Money? CreditLimit { get; private set; }
    public int? PaymentTermsDays { get; private set; }
    public string? EmployeeNumber { get; private set; }

    private PartyRole() : base() { }

    private PartyRole(PartyRoleType roleType, Money? creditLimit, int? paymentTermsDays, string? employeeNumber)
        : base()
    {
        RoleType = roleType;
        Status = PartyRoleStatus.Active;
        ActivatedAt = DateTime.UtcNow;
        CreditLimit = creditLimit;
        PaymentTermsDays = paymentTermsDays;
        EmployeeNumber = employeeNumber;
    }

    public static PartyRole Create(PartyRoleType roleType, Money? creditLimit = null,
        int? paymentTermsDays = null, string? employeeNumber = null)
    {
        if (paymentTermsDays.HasValue && paymentTermsDays.Value < 0)
            throw new DomainException("Payment terms days cannot be negative.");

        return new PartyRole(roleType, creditLimit, paymentTermsDays, employeeNumber);
    }

    internal void Activate()
    {
        if (Status == PartyRoleStatus.Active) return;
        Status = PartyRoleStatus.Active;
        ActivatedAt = DateTime.UtcNow;
        DeactivatedAt = null;
        SetUpdatedAt();
    }

    internal void Deactivate()
    {
        Status = PartyRoleStatus.Inactive;
        DeactivatedAt = DateTime.UtcNow;
        SetUpdatedAt();
    }

    internal void Suspend()
    {
        Status = PartyRoleStatus.Suspended;
        SetUpdatedAt();
    }

    public bool IsActive => Status == PartyRoleStatus.Active;
}
