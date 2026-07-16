using FluentAssertions;
using SmallBusinessPOS.Domain.Entities;
using SmallBusinessPOS.Domain.Enums;

namespace SmallBusinessPOS.Domain.Tests;

public class ProductEntityTests
{
    private static readonly Guid BusinessId = Guid.CreateVersion7();
    private static readonly Guid CategoryId = Guid.CreateVersion7();

    [Fact]
    public void Create_ValidProduct_ReturnsProduct()
    {
        var product = Product.Create(
            BusinessId, "POL-ENT", "Pollo entero",
            ProductType.PreparedItem, UnitOfMeasure.Unit,
            salePrice: 650m, estimatedCost: 280m,
            categoryId: CategoryId);

        product.BusinessId.Should().Be(BusinessId);
        product.Code.Should().Be("POL-ENT");
        product.Name.Should().Be("Pollo entero");
        product.ProductType.Should().Be(ProductType.PreparedItem);
        product.SalePrice.Should().Be(650m);
        product.EstimatedCost.Should().Be(280m);
        product.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_CodeIsUppercased()
    {
        var product = Product.Create(
            BusinessId, "pol-ent", "Pollo entero",
            ProductType.Standard, UnitOfMeasure.Unit, 100m);

        product.Code.Should().Be("POL-ENT");
    }

    [Fact]
    public void Create_NegativePrice_ThrowsArgumentOutOfRangeException()
    {
        var act = () => Product.Create(
            BusinessId, "P001", "Test",
            ProductType.Standard, UnitOfMeasure.Unit,
            salePrice: -1m);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_NegativeCost_ThrowsArgumentOutOfRangeException()
    {
        var act = () => Product.Create(
            BusinessId, "P001", "Test",
            ProductType.Standard, UnitOfMeasure.Unit,
            salePrice: 0m, estimatedCost: -5m);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_ZeroPrice_IsValid()
    {
        var product = Product.Create(
            BusinessId, "P001", "Test gratis",
            ProductType.Standard, UnitOfMeasure.Unit,
            salePrice: 0m);

        product.SalePrice.Should().Be(0m);
    }

    [Fact]
    public void Disable_SetsIsActiveFalse()
    {
        var product = Product.Create(
            BusinessId, "P001", "Test",
            ProductType.Standard, UnitOfMeasure.Unit, 100m);

        product.Disable();
        product.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Enable_SetsIsActiveTrue()
    {
        var product = Product.Create(
            BusinessId, "P001", "Test",
            ProductType.Standard, UnitOfMeasure.Unit, 100m);
        product.Disable();
        product.Enable();
        product.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Update_CannotSetNegativePrice()
    {
        var product = Product.Create(
            BusinessId, "P001", "Test",
            ProductType.Standard, UnitOfMeasure.Unit, 100m);

        var act = () => product.Update(
            "P001", "Test", null, ProductType.Standard,
            UnitOfMeasure.Unit, -50m, 0m, null, true, false);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ProductComponent_CannotBeZeroOrNegativeQuantity()
    {
        var parentId = Guid.CreateVersion7();
        var componentId = Guid.CreateVersion7();

        var act = () => ProductComponent.Create(parentId, componentId, 0m);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ProductComponent_CannotReferenceItself()
    {
        var id = Guid.CreateVersion7();
        var act = () => ProductComponent.Create(id, id, 1m);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ProductComponent_FractionalQuantityAllowed()
    {
        // Medio pollo = 0.5 del pollo entero
        var parentId = Guid.CreateVersion7();
        var polloId = Guid.CreateVersion7();

        var component = ProductComponent.Create(parentId, polloId, 0.5m);
        component.Quantity.Should().Be(0.5m);
    }

    [Fact]
    public void ProductComponent_QuarterChicken()
    {
        // Cuarto de pollo = 0.25 del pollo entero
        var parentId = Guid.CreateVersion7();
        var polloId = Guid.CreateVersion7();

        var component = ProductComponent.Create(parentId, polloId, 0.25m);
        component.Quantity.Should().Be(0.25m);
    }

    [Fact]
    public void InventoryStock_IsBelowMinimum_WhenQuantityLessThanOrEqualMinimum()
    {
        var stock = InventoryStock.Create(BusinessId, Guid.CreateVersion7(), Guid.CreateVersion7(), 5m);
        stock.SetMinimumQuantity(10m);

        stock.IsBelowMinimum().Should().BeTrue();
    }

    [Fact]
    public void InventoryStock_NegativeMinimum_Throws()
    {
        var stock = InventoryStock.Create(BusinessId, Guid.CreateVersion7(), Guid.CreateVersion7(), 0m);
        var act = () => stock.SetMinimumQuantity(-1m);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
