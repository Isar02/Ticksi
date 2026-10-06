using Ticksi.Application.Features.Orders.Commands.CreateOrder;

namespace Ticksi.Tests.OrderTests.UnitTests;

public class CreateOrderCommandValidatorTests
{
    private readonly CreateOrderCommandValidator _validator = new();

    [Fact]
    public async Task Validate_DistinctTicketTypesWithinLimits_Passes()
    {
        var result = await _validator.ValidateAsync(Command(Item(1), Item(OrderItem.Constraints.MaxQuantity)));

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Validate_NoItems_IsRejected()
    {
        var result = await _validator.ValidateAsync(Command());

        Assert.Equal("Items", Assert.Single(result.Errors).PropertyName);
    }

    [Fact]
    public async Task Validate_SameTicketTypeTwice_IsRejected()
    {
        var item = Item(1);

        var result = await _validator.ValidateAsync(Command(item, Item(2, item.TicketTypeId)));

        Assert.Equal("Each ticket type can appear only once.", Assert.Single(result.Errors).ErrorMessage);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(OrderItem.Constraints.MaxQuantity + 1)]
    public async Task Validate_QuantityOutOfRange_IsRejected(int quantity)
    {
        var result = await _validator.ValidateAsync(Command(Item(1), Item(quantity)));

        var error = Assert.Single(result.Errors);
        Assert.Equal(("Items[1].Quantity", "Quantity must be between 1 and 10."), (error.PropertyName, error.ErrorMessage));
    }

    [Fact]
    public async Task Validate_MissingTicketTypeAndNullItem_AreRejected()
    {
        var result = await _validator.ValidateAsync(Command(Item(1, Guid.Empty), null!));

        Assert.Equal(["Items[0].TicketTypeId", "Items[1]"], result.Errors.Select(e => e.PropertyName));
    }

    private static CreateOrderCommand Command(params OrderItemInput[] items) => new() { Items = [.. items] };

    private static OrderItemInput Item(int quantity, Guid? ticketTypeId = null) =>
        new() { TicketTypeId = ticketTypeId ?? Guid.NewGuid(), Quantity = quantity };
}
