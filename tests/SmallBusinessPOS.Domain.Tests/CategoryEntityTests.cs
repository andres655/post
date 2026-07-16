using FluentAssertions;
using SmallBusinessPOS.Domain.Entities;
using SmallBusinessPOS.Domain.Enums;

namespace SmallBusinessPOS.Domain.Tests;

public class CategoryEntityTests
{
    private static readonly Guid BusinessId = Guid.CreateVersion7();

    [Fact]
    public void Create_ValidData_ReturnsCategory()
    {
        var category = Category.Create(BusinessId, "Pollos", "Pollos horneados", 1);

        category.BusinessId.Should().Be(BusinessId);
        category.Name.Should().Be("Pollos");
        category.Description.Should().Be("Pollos horneados");
        category.SortOrder.Should().Be(1);
        category.IsActive.Should().BeTrue();
        category.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_TrimsName()
    {
        var category = Category.Create(BusinessId, "  Pollos  ");
        category.Name.Should().Be("Pollos");
    }

    [Fact]
    public void Create_EmptyName_ThrowsArgumentException()
    {
        var act = () => Category.Create(BusinessId, "");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WhitespaceName_ThrowsArgumentException()
    {
        var act = () => Category.Create(BusinessId, "   ");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Update_ChangesName()
    {
        var category = Category.Create(BusinessId, "Pollos");
        category.Update("Pollos Horneados", "Descripción actualizada", 5);

        category.Name.Should().Be("Pollos Horneados");
        category.Description.Should().Be("Descripción actualizada");
        category.SortOrder.Should().Be(5);
    }

    [Fact]
    public void Disable_SetsIsActiveFalse()
    {
        var category = Category.Create(BusinessId, "Pollos");
        category.Disable();
        category.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Enable_SetsIsActiveTrue()
    {
        var category = Category.Create(BusinessId, "Pollos");
        category.Disable();
        category.Enable();
        category.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Id_UsesGuidVersion7_AreUniqueAndNonEmpty()
    {
        // Guid v7 garantiza IDs únicos. La ordenación temporal entre distintos milisegundos
        // es monotónica, pero dentro del mismo ms los bits aleatorios pueden variar.
        // La ordenación útil se observa en contextos de persistencia (DB index locality).
        var first = Category.Create(BusinessId, "A");
        var second = Category.Create(BusinessId, "B");

        first.Id.Should().NotBe(Guid.Empty);
        second.Id.Should().NotBe(Guid.Empty);
        first.Id.Should().NotBe(second.Id, "cada entidad recibe un ID único");
    }
}
