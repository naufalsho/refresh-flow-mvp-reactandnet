using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace refresh_flow_mvp_reactandnet.Server.Middleware
{
    public class SeedSecretRequirementFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var isSeedEndpoint = context.ApiDescription.RelativePath?.StartsWith("dev/seed") == true;
            if (!isSeedEndpoint) return;

            operation.Security ??= new List<OpenApiSecurityRequirement>();
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "SeedSecret"
                    }
                }] = new List<string>()
            });
        }
    }
}
