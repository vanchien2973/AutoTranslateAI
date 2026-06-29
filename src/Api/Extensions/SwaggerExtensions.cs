using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace Api.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "AutoTranslateAI API",
                Version = "v1",
                Description = "API for the AutoTranslateAI dubbing pipeline.",
            });
        });

        // --- Enable these when the corresponding feature actually exists ---
        // API versioning (per-version Swagger docs) — needs the Asp.Versioning.Mvc.ApiExplorer
        // package + API versioning configured, then re-enable Options.ConfigureSwaggerOptions:
        //   services.AddTransient<IConfigureOptions<SwaggerGenOptions>, Options.ConfigureSwaggerOptions>();
        // FluentValidation rules shown in Swagger — needs MicroElements.Swashbuckle.FluentValidation
        // + validators registered in the Application layer:
        //   services.AddFluentValidationRulesToSwagger();

        return services;
    }

    public static void ConfigureSwagger(this WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "AutoTranslateAI API v1");
            options.DisplayRequestDuration();
            options.DocExpansion(DocExpansion.None);
        });

        // Convenience redirect from root to the Swagger UI (this project is local-only).
        app.MapGet("/", () => Results.Redirect("/swagger"))
            .ExcludeFromDescription();
    }
}
