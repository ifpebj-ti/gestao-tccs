using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace gestaotcc.WebApi.SchemaFilters;

public class DateAndTimeOnlySchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type == typeof(DateOnly))
        {
            schema.Type = "string";
            schema.Format = "date";
            schema.Example = new Microsoft.OpenApi.Any.OpenApiString("2003-07-27");
        }

        if (context.Type == typeof(TimeOnly))
        {
            schema.Type = "string";
            schema.Format = "time";
            schema.Example = new Microsoft.OpenApi.Any.OpenApiString("20:00");
        }
    }
}
