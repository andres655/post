namespace SmallBusinessPOS.Web.Components.Pages.Pos;

public static class PosFlowGuidance
{
    public static bool CanAttemptCheckout(bool hasOpenSession, bool hasProducts, decimal total, decimal paid)
    {
        if (!hasOpenSession || !hasProducts)
            return false;

        return total <= 0m || paid > 0m;
    }

    public static bool CanConfirmCheckout(bool hasOpenSession, bool hasProducts, decimal total, decimal paid)
    {
        if (!hasOpenSession || !hasProducts || total <= 0m)
            return false;

        return paid >= total;
    }

    public static string GetPaymentHint(bool hasOpenSession, bool hasProducts, decimal total, decimal paid)
    {
        if (!hasOpenSession)
            return "Abre la caja antes de cobrar para registrar la venta correctamente.";

        if (!hasProducts)
            return "Agrega al menos un producto para iniciar la venta.";

        if (total <= 0m)
            return "El total debe ser mayor que cero para confirmar la venta.";

        if (paid < total)
            return $"Faltan RD$ {(total - paid):N2} para cubrir el total del ticket.";

        if (paid == total)
            return "Pago exacto. Puedes confirmar la venta.";

        return $"Se devolverá cambio de RD$ {(paid - total):N2}.";
    }

    public static string GetCheckoutLabel(bool hasOpenSession, bool hasProducts, decimal total, decimal paid)
    {
        if (!hasOpenSession)
            return "Abrir caja";

        if (!hasProducts)
            return "Agregar productos";

        if (total <= 0m)
            return "Completar venta";

        if (paid < total)
            return "Completar pagos";

        return "Cobrar venta";
    }
}
