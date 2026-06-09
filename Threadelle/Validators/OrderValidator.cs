using FluentValidation;
using Threadelle.Models;

public class OrderValidator : AbstractValidator<Order>
{
    public OrderValidator()
    {

        RuleFor(order => order.SubTotal).GreaterThanOrEqualTo(0).WithMessage("Subtotal must be greater than or equal to 0.");

        RuleFor(order => order.DiscountAmount).GreaterThanOrEqualTo(0).WithMessage("Discount amount must be greater than or equal to 0.");

        RuleFor(order => order.TotalPrice).GreaterThanOrEqualTo(0).WithMessage("Total price must be greater than or equal to 0.");
    }
}