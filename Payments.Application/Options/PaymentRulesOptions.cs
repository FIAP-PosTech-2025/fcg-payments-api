namespace Payments.Application.Options;

public class PaymentRulesOptions
{
    public const string SectionName = "PaymentRules";

    public decimal MaxPrecoAprovado { get; set; } = 200m;
}
