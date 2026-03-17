using System.Globalization;
using System.Linq;
using InteractiveLeads.Application.Requests;
using InteractiveLeads.Infrastructure.Tenancy.Models;

namespace InteractiveLeads.Infrastructure.Tenancy
{
    /// <summary>
    /// Aplica filtros PrimeNG ao query de tenants. Match all = AND, match any = OR.
    /// </summary>
    public static class TenantFilterExpressionBuilder
    {
        public static IQueryable<InteractiveTenantInfo> ApplyFilters(
            IQueryable<InteractiveTenantInfo> query,
            List<ColumnFilterRequest>? filters,
            string? filterOperator = null)
        {
            if (filters is not { Count: > 0 })
                return query;

            var isOr = string.Equals(filterOperator?.Trim(), "or", StringComparison.OrdinalIgnoreCase);

            if (isOr)
            {
                var withFilters = filters
                    .Where(f => !string.IsNullOrWhiteSpace(f.Field) && !string.IsNullOrWhiteSpace(f.Value))
                    .Select(f => ApplyOneFilter(query, f))
                    .Where(q => q != null)
                    .ToList();
                if (withFilters.Count == 0) return query;
                if (withFilters.Count == 1) return withFilters[0]!;
                var union = withFilters[0]!;
                for (var i = 1; i < withFilters.Count; i++)
                    union = union.Union(withFilters[i]!);
                return union.Distinct();
            }

            foreach (var f in filters)
            {
                var q = ApplyOneFilter(query, f);
                if (q != null)
                    query = q;
            }

            return query;
        }

        /// <summary>
        /// Aplica um único filtro ao query. Retorna null se o filtro for inválido.
        /// </summary>
        private static IQueryable<InteractiveTenantInfo>? ApplyOneFilter(IQueryable<InteractiveTenantInfo> query, ColumnFilterRequest f)
        {
            if (string.IsNullOrWhiteSpace(f.Field) || string.IsNullOrWhiteSpace(f.Value))
                return null;

            var field = f.Field.Trim().ToLowerInvariant();
            var mode = (f.MatchMode ?? "contains").ToLowerInvariant();
            var value = f.Value.Trim();
            var v = value.ToLower();

            return (field, mode) switch
            {
                ("name", "contains") => query.Where(t => t.Name != null && t.Name.ToLower().Contains(v)),
                ("name", "startswith") => query.Where(t => t.Name != null && t.Name.ToLower().StartsWith(v)),
                ("name", "endswith") => query.Where(t => t.Name != null && t.Name.ToLower().EndsWith(v)),
                ("name", "equals") => query.Where(t => t.Name != null && t.Name.Equals(value, StringComparison.OrdinalIgnoreCase)),
                ("name", "notequals") => query.Where(t => t.Name == null || !t.Name.Equals(value, StringComparison.OrdinalIgnoreCase)),
                ("email", "contains") => query.Where(t => t.Email.ToLower().Contains(v)),
                ("email", "startswith") => query.Where(t => t.Email.ToLower().StartsWith(v)),
                ("email", "endswith") => query.Where(t => t.Email.ToLower().EndsWith(v)),
                ("email", "equals") => query.Where(t => t.Email.Equals(value, StringComparison.OrdinalIgnoreCase)),
                ("email", "notequals") => query.Where(t => !t.Email.Equals(value, StringComparison.OrdinalIgnoreCase)),
                ("firstname", "contains") => query.Where(t => t.FirstName.ToLower().Contains(v)),
                ("firstname", "startswith") => query.Where(t => t.FirstName.ToLower().StartsWith(v)),
                ("firstname", "endswith") => query.Where(t => t.FirstName.ToLower().EndsWith(v)),
                ("firstname", "equals") => query.Where(t => t.FirstName.Equals(value, StringComparison.OrdinalIgnoreCase)),
                ("firstname", "notequals") => query.Where(t => !t.FirstName.Equals(value, StringComparison.OrdinalIgnoreCase)),
                ("lastname", "contains") => query.Where(t => t.LastName.ToLower().Contains(v)),
                ("lastname", "startswith") => query.Where(t => t.LastName.ToLower().StartsWith(v)),
                ("lastname", "endswith") => query.Where(t => t.LastName.ToLower().EndsWith(v)),
                ("lastname", "equals") => query.Where(t => t.LastName.Equals(value, StringComparison.OrdinalIgnoreCase)),
                ("lastname", "notequals") => query.Where(t => !t.LastName.Equals(value, StringComparison.OrdinalIgnoreCase)),
                ("expirationdate", "dateis") or ("expirationdate", "equals") when DateTime.TryParse(value, null, DateTimeStyles.RoundtripKind, out var d1) => query.Where(t => t.ExpirationDate.Date == d1.Date),
                ("expirationdate", "dateisnot") or ("expirationdate", "notequals") when DateTime.TryParse(value, null, DateTimeStyles.RoundtripKind, out var d2) => query.Where(t => t.ExpirationDate.Date != d2.Date),
                ("expirationdate", "datebefore") or ("expirationdate", "lt") or ("expirationdate", "lte") when DateTime.TryParse(value, null, DateTimeStyles.RoundtripKind, out var d3) => query.Where(t => t.ExpirationDate.Date <= d3.Date),
                ("expirationdate", "dateafter") or ("expirationdate", "gt") or ("expirationdate", "gte") when DateTime.TryParse(value, null, DateTimeStyles.RoundtripKind, out var d4) => query.Where(t => t.ExpirationDate.Date >= d4.Date),
                ("isactive", "notequals") when bool.TryParse(value, out var bn) => query.Where(t => t.IsActive != bn),
                ("isactive", _) when bool.TryParse(value, out var b) => query.Where(t => t.IsActive == b),
                _ when field == "name" => query.Where(t => t.Name != null && t.Name.ToLower().Contains(v)),
                _ when field == "email" => query.Where(t => t.Email.ToLower().Contains(v)),
                _ when field == "firstname" => query.Where(t => t.FirstName.ToLower().Contains(v)),
                _ when field == "lastname" => query.Where(t => t.LastName.ToLower().Contains(v)),
                _ => null
            };
        }
    }
}
