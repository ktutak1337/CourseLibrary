using System;
using CourseLibrary.Application.Services;
using CourseLibrary.Core.Repositories;
using CourseLibrary.Infrastructure.Persistence.Mongo.Documents;
using CourseLibrary.Infrastructure.Persistence.Mongo.Repositories;
using CourseLibrary.Infrastructure.Services;
using CourseLibrary.Infrastructure.Swagger;
using Convey;
using Convey.Persistence.MongoDB;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace CourseLibrary.Infrastructure
{
    public static class Extensions
    {
        public static IConveyBuilder AddInfrastructure(this IConveyBuilder builder)
        {
            builder.Services.AddTransient<IOrdersRepository, OrdersRepository>();
            builder.Services.AddTransient<IDispatcher, Dispatcher>();
            return builder
                .AddMongo()
                .AddMongoRepository<OrderDocument, Guid>("orders");
        }
        
        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {
            services.AddSwaggerDocs();
            return services;
        }
        


        public static IApplicationBuilder UseInfrastructure(this IApplicationBuilder builder)
            => builder.UseSwaggerDocs();

        public static IServiceCollection AddSwaggerDocs(this IServiceCollection services)
        {
            var settings = services.GetOptions<SwaggerSettings>("swagger");

            if(!settings.Enabled)
            {
                return services;
            }

            services.AddSingleton(new SwaggerSettings());
            return services.AddSwaggerGen(setup =>
            {
                setup.SwaggerDoc(settings.Name, new OpenApiInfo { Title = settings.Title, Version = settings.Version });
                
                if (settings.Authorization)
                {
                    setup.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                    {
                        Name = "Authorization",
                        In = ParameterLocation.Header,
                        Type = SecuritySchemeType.ApiKey,
                        Scheme = "Bearer",
                        Description = "JWT Authorization header using the Bearer scheme (Example: Bearer {token}).",
                    });

                    if(!(settings.OAuth2 is null))
                    {
                        setup.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
                        {
                            Flows = new OpenApiOAuthFlows
                            {
                                Implicit = OAuthFlow.Setup(settings),
                                Password = OAuthFlow.Setup(settings),
                                ClientCredentials = OAuthFlow.Setup(settings),
                                AuthorizationCode = OAuthFlow.Setup(settings)
                            },
                            In = ParameterLocation.Header,
                            Name = "Authorization",
                            Type = SecuritySchemeType.OAuth2
                        });
                    }

                    setup.AddSecurityRequirement(new OpenApiSecurityRequirement()
                    {
                        {
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Type = ReferenceType.SecurityScheme,
                                    Id = "Bearer"
                                }
                            },
                            Array.Empty<string>()
                        }
                    });
                }
            });
        }

        public static IApplicationBuilder UseSwaggerDocs(this IApplicationBuilder builder)
        {
            var settings = builder.ApplicationServices.GetService<IConfiguration>()
                .GetOptions<SwaggerSettings>("swagger");
            
            if (!settings.Enabled)
            {
                return builder;
            }

            var routePrefix = string.IsNullOrWhiteSpace(settings.RoutePrefix) ? "swagger ": settings.RoutePrefix;

            builder.UseStaticFiles()
                .UseSwagger(setup => setup.RouteTemplate = routePrefix + "/{documentName}/swagger.json");

            return builder.UseSwaggerUI(setup =>
            {
                setup.SwaggerEndpoint($"/{routePrefix}/{settings.Name}/swagger.json", settings.Title);
                setup.RoutePrefix = routePrefix;
            });
        }
        public static TModel GetOptions<TModel>(this IConfiguration configuration, string sectionName) 
            where TModel : new()
        {
            if (!string.IsNullOrWhiteSpace(sectionName))
            {
                var model = new TModel();
                configuration.GetSection(sectionName).Bind(model);

                return model;
            }

            return default(TModel);
        }

        public static TModel GetOptions<TModel>(this IServiceCollection services, string sectionName)
            where TModel : new()
        {
            if (!string.IsNullOrWhiteSpace(sectionName))
            {
                using var serviceProvider = services.BuildServiceProvider();
                var configuration = serviceProvider.GetService<IConfiguration>();
                return configuration.GetOptions<TModel>(sectionName);
            }
            
            return default(TModel);
        }
    }
}