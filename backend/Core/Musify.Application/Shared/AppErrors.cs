using ErrorOr;

namespace Musify.Application.Shared
{
    public static class AppErrors
    {
        public static Error NotFound(string entity, object id) =>
            Error.NotFound($"{entity}.NotFound", $"{entity} {id} was not found.");

        public static Error Forbidden(string entity, object id) =>
            Error.Forbidden($"{entity}.Forbidden", $"You do not have permission to modify {entity} {id}.");

        public static Error Conflict(string code, string description) =>
            Error.Conflict(code, description);
    }
}
