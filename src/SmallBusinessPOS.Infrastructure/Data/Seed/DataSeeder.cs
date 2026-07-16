using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmallBusinessPOS.Domain.Entities;
using SmallBusinessPOS.Domain.Enums;
using SmallBusinessPOS.Infrastructure.Data.Identity;

namespace SmallBusinessPOS.Infrastructure.Data.Seed;

/// <summary>
/// Crea datos iniciales para desarrollo y primer uso.
/// IMPORTANTE: Las contraseñas de desarrollo se leen de configuración,
/// no se incluyen literalmente en el repositorio.
/// </summary>
public class DataSeeder(
    AppDbContext db,
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    ILogger<DataSeeder> logger)
{
    public async Task SeedAsync(
        string adminPassword,
        string supervisorPassword,
        string cashierPassword)
    {
        try
        {
            await db.Database.MigrateAsync();
            await SeedRolesAsync();
            await SeedBusinessAsync();
            await SeedUsersAsync(adminPassword, supervisorPassword, cashierPassword);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error durante el seed de datos iniciales.");
            throw;
        }
    }

    private async Task SeedRolesAsync()
    {
        string[] roles = ["Administrator", "Supervisor", "Cashier"];

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
                logger.LogInformation("Rol creado: {Role}", role);
            }
        }
    }

    private async Task SeedBusinessAsync()
    {
        if (await db.Businesses.AnyAsync())
            return;

        // --- Negocio ---
        var business = Business.Create(
            name: "Pollo Sabroso",
            currency: "DOP",
            timeZone: "America/Santo_Domingo",
            businessType: BusinessType.RotisserieChicken,
            phone: "809-555-0001");
        business.SetCreatedBy("system");

        db.Businesses.Add(business);

        // --- Configuración ---
        var settings = BusinessSettings.CreateDefault(business.Id);
        db.BusinessSettings.Add(settings);

        // --- Sucursal principal ---
        var branch = Branch.Create(
            businessId: business.Id,
            name: "Sucursal Principal",
            isMain: true,
            address: "Calle Principal #1",
            phone: "809-555-0001");
        branch.SetCreatedBy("system");

        db.Branches.Add(branch);

        // --- Caja principal ---
        var register = CashRegister.Create(
            business.Id,
            branch.Id,
            code: "C01",
            name: "Caja principal");
        register.SetCreatedBy("system");
        db.CashRegisters.Add(register);

        // --- Métodos de pago ---
        var efectivo = PaymentMethod.Create(business.Id, "CASH", "Efectivo", PaymentMethodType.Cash);
        var tarjeta = PaymentMethod.Create(business.Id, "CARD", "Tarjeta", PaymentMethodType.DebitCard);
        var transferencia = PaymentMethod.Create(business.Id, "BANK", "Transferencia", PaymentMethodType.BankTransfer);

        efectivo.SetCreatedBy("system");
        tarjeta.SetCreatedBy("system");
        transferencia.SetCreatedBy("system");

        db.PaymentMethods.AddRange(efectivo, tarjeta, transferencia);

        // --- Categorías ---
        var catPollos = Category.Create(business.Id, "Pollos", "Pollos horneados", 1);
        var catAcompañamientos = Category.Create(business.Id, "Acompañamientos", "Yuca, ensalada y más", 2);
        var catBebidas = Category.Create(business.Id, "Bebidas", "Refrescos y agua", 3);
        var catCombos = Category.Create(business.Id, "Combos", "Paquetes combinados", 4);

        catPollos.SetCreatedBy("system");
        catAcompañamientos.SetCreatedBy("system");
        catBebidas.SetCreatedBy("system");
        catCombos.SetCreatedBy("system");

        db.Categories.AddRange(catPollos, catAcompañamientos, catBebidas, catCombos);

        // --- Productos ---
        var polloEntero = Product.Create(
            business.Id, "POL-ENT", "Pollo horneado entero",
            ProductType.PreparedItem, UnitOfMeasure.Unit, 650m, 280m,
            catPollos.Id, tracksInventory: true);

        var medioPolo = Product.Create(
            business.Id, "POL-MED", "Medio pollo",
            ProductType.PreparedItem, UnitOfMeasure.Portion, 350m, 140m,
            catPollos.Id, tracksInventory: false, allowsFractionalQuantity: false,
            description: "Mitad de pollo horneado");

        var cuartoPolo = Product.Create(
            business.Id, "POL-CUA", "Cuarto de pollo",
            ProductType.PreparedItem, UnitOfMeasure.Portion, 190m, 70m,
            catPollos.Id, tracksInventory: false, allowsFractionalQuantity: false,
            description: "Un cuarto de pollo horneado");

        var yucaPeq = Product.Create(
            business.Id, "YUC-PEQ", "Yuca pequeña",
            ProductType.PreparedItem, UnitOfMeasure.Portion, 80m, 25m,
            catAcompañamientos.Id, tracksInventory: false);

        var yucaGrd = Product.Create(
            business.Id, "YUC-GRD", "Yuca grande",
            ProductType.PreparedItem, UnitOfMeasure.Portion, 120m, 40m,
            catAcompañamientos.Id, tracksInventory: false);

        var ensalada = Product.Create(
            business.Id, "ENS-001", "Ensalada",
            ProductType.PreparedItem, UnitOfMeasure.Portion, 80m, 20m,
            catAcompañamientos.Id, tracksInventory: false);

        var refrescoPeq = Product.Create(
            business.Id, "REF-PEQ", "Refresco pequeño",
            ProductType.Standard, UnitOfMeasure.Unit, 60m, 25m,
            catBebidas.Id, tracksInventory: false);

        var refresco2L = Product.Create(
            business.Id, "REF-2LT", "Refresco de 2 litros",
            ProductType.Standard, UnitOfMeasure.Unit, 120m, 55m,
            catBebidas.Id, tracksInventory: false);

        var agua = Product.Create(
            business.Id, "AGU-001", "Agua",
            ProductType.Standard, UnitOfMeasure.Unit, 40m, 15m,
            catBebidas.Id, tracksInventory: false);

        var comboFamiliar = Product.Create(
            business.Id, "CMB-FAM", "Combo familiar",
            ProductType.Combo, UnitOfMeasure.Unit, 1050m, 395m,
            catCombos.Id, tracksInventory: false,
            description: "1 pollo entero + 1 yuca grande + 1 ensalada + 1 refresco 2L");

        foreach (var p in new[] { polloEntero, medioPolo, cuartoPolo, yucaPeq, yucaGrd, ensalada, refrescoPeq, refresco2L, agua, comboFamiliar })
            p.SetCreatedBy("system");

        db.Products.AddRange(polloEntero, medioPolo, cuartoPolo, yucaPeq, yucaGrd, ensalada, refrescoPeq, refresco2L, agua, comboFamiliar);

        // --- Componentes del combo ---
        db.ProductComponents.AddRange(
            ProductComponent.Create(comboFamiliar.Id, polloEntero.Id, 1m),
            ProductComponent.Create(comboFamiliar.Id, yucaGrd.Id, 1m),
            ProductComponent.Create(comboFamiliar.Id, ensalada.Id, 1m),
            ProductComponent.Create(comboFamiliar.Id, refresco2L.Id, 1m));

        // --- Componentes de medio pollo (consume 0.5 del pollo entero) ---
        db.ProductComponents.AddRange(
            ProductComponent.Create(medioPolo.Id, polloEntero.Id, 0.5m));

        // --- Componentes de cuarto de pollo (consume 0.25 del pollo entero) ---
        db.ProductComponents.AddRange(
            ProductComponent.Create(cuartoPolo.Id, polloEntero.Id, 0.25m));

        // --- Stock inicial del pollo entero ---
        var stock = InventoryStock.Create(business.Id, branch.Id, polloEntero.Id, 50m);
        db.InventoryStocks.Add(stock);

        await db.SaveChangesAsync();
        logger.LogInformation("Datos iniciales del negocio creados correctamente.");
    }

    private async Task SeedUsersAsync(
        string adminPassword,
        string supervisorPassword,
        string cashierPassword)
    {
        var business = await db.Businesses.FirstOrDefaultAsync();
        if (business is null) return;

        await CreateUserAsync(
            email: "admin@pollosaboroso.local",
            firstName: "Admin",
            lastName: "Sistema",
            role: "Administrator",
            password: adminPassword,
            businessId: business.Id);

        await CreateUserAsync(
            email: "supervisor@pollosaboroso.local",
            firstName: "Juan",
            lastName: "García",
            role: "Supervisor",
            password: supervisorPassword,
            businessId: business.Id);

        await CreateUserAsync(
            email: "cajero@pollosaboroso.local",
            firstName: "María",
            lastName: "López",
            role: "Cashier",
            password: cashierPassword,
            businessId: business.Id);
    }

    private async Task CreateUserAsync(
        string email,
        string firstName,
        string lastName,
        string role,
        string password,
        Guid businessId)
    {
        if (await userManager.FindByEmailAsync(email) is not null)
            return;

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            BusinessId = businessId,
            IsActive = true,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, password);

        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(user, role);
            logger.LogInformation("Usuario creado: {Email} → {Role}", email, role);
        }
        else
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            logger.LogError("Error creando usuario {Email}: {Errors}", email, errors);
        }
    }
}
