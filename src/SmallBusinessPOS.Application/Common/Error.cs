namespace SmallBusinessPOS.Application.Common;

/// <summary>
/// Representa un error de negocio esperado.
/// No se usa para excepciones del sistema.
/// </summary>
public sealed record Error(string Code, string Description)
{
    public static readonly Error None = new(string.Empty, string.Empty);

    public static Error NotFound(string entity, Guid id) =>
        new($"{entity}.NotFound", $"{entity} con Id '{id}' no fue encontrado.");

    public static Error NotFound(string entity, string value) =>
        new($"{entity}.NotFound", $"{entity} '{value}' no fue encontrado.");

    public static Error Conflict(string code, string description) =>
        new(code, description);

    public static Error Validation(string field, string message) =>
        new($"Validation.{field}", message);

    public static Error BusinessRule(string code, string description) =>
        new(code, description);

    public override string ToString() => $"[{Code}] {Description}";
}
