using System.Text;
using System.Threading.RateLimiting;
using Marmitas.Domain.Constants;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace Marmitas.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiControllers(this IServiceCollection services)
    {
        services.AddControllers(options =>
        {
            options.Conventions.Add(new ApiV1RoutePrefixConvention());
        });
        return services;
    }

    public static IServiceCollection AddPlatformRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var permitLimit = configuration.GetValue<int?>("RateLimiting:PermitPerMinute") ?? 200;

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                var key = httpContext.Connection.RemoteIpAddress?.ToString() ?? "local";
                return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = Math.Max(50, permitLimit),
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    AutoReplenishment = true
                });
            });
        });

        return services;
    }

    public static IServiceCollection AddSwaggerConfig(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Marmitas API",
                Version = "v1",
                Description = "API REST para gerenciamento, venda avulsa, assinatura recorrente e entrega de marmitas."
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Informe apenas o token JWT"
            });
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                    },
                    Array.Empty<string>()
                }
            });
        });

        return services;
    }

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwt = configuration.GetSection("Jwt");
        var key = jwt["Key"];

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt["Issuer"],
                    ValidateAudience = true,
                    ValidAudience = jwt["Audience"],
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key!)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = "sub",
                    RoleClaimType = "role"
                };
            });

        services.AddAuthorization(options =>
        {
            foreach (var permission in PermissionNames.All)
            {
                options.AddPolicy(permission, policy =>
                    policy.RequireAssertion(context =>
                        context.User.HasClaim("permission", permission) ||
                        context.User.IsInRole(RoleNames.SuperAdmin) ||
                        context.User.IsInRole(RoleNames.Admin) ||
                        (permission is PermissionNames.OrdersCreate or PermissionNames.OrdersRead or PermissionNames.OrdersCancel &&
                         context.User.IsInRole(RoleNames.Customer)) ||
                        (permission is PermissionNames.OrdersRead or PermissionNames.OrdersUpdate &&
                         (context.User.IsInRole(RoleNames.RestaurantOwner) || context.User.IsInRole(RoleNames.RestaurantStaff))) ||
                        (permission == PermissionNames.OrdersRead && context.User.IsInRole(RoleNames.Driver))));
            }
        });

        return services;
    }
}

public sealed class ApiV1RoutePrefixConvention : IApplicationModelConvention
{
    public void Apply(ApplicationModel application)
    {
        foreach (var controller in application.Controllers)
        {
            AddV1Selectors(controller.Selectors);
            foreach (var action in controller.Actions)
            {
                AddV1Selectors(action.Selectors);
            }
        }
    }

    private static void AddV1Selectors(IList<SelectorModel> selectors)
    {
        var extraSelectors = new List<SelectorModel>();

        foreach (var selector in selectors)
        {
            var template = selector.AttributeRouteModel?.Template;
            if (string.IsNullOrWhiteSpace(template))
                continue;

            if (template.StartsWith("api/", StringComparison.OrdinalIgnoreCase) &&
                !template.StartsWith("api/v1/", StringComparison.OrdinalIgnoreCase))
            {
                var v1Template = "api/v1/" + template[4..];
                var clone = new SelectorModel(selector)
                {
                    AttributeRouteModel = new AttributeRouteModel(selector.AttributeRouteModel!)
                    {
                        Template = v1Template
                    }
                };
                extraSelectors.Add(clone);
            }
        }

        foreach (var extra in extraSelectors)
        {
            selectors.Add(extra);
        }
    }
}
