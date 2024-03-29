﻿namespace AugmentationsAPI.Infrastructure.Extensions
{
    using Data;
    using Data.Models;
    using ActionFilters;
    using Features.Identity.Services;
    using Features.Links.Services;
    using Features.PDF.Services;
    using Features.Augmentations.Models;
    using Features.Augmentations.Repository;
    using System.Reflection;
    using System.Text;
    using Microsoft.AspNetCore.Authentication.JwtBearer;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.IdentityModel.Tokens;
    using Microsoft.OpenApi.Models;

    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Injects this Applications Database Context to the Collection of Services.
        /// </summary>
        /// <param name="services"> The App's Collection of Services. </param>
        /// <param name="configuration"> A Collection of the Applications Configuration Providers. </param>
        /// <returns> The <see cref="IServiceCollection"/> so that additional calls can be chained. </returns>
        public static IServiceCollection AddDatabase(this IServiceCollection services,
            IConfiguration configuration)
        {
            // Using the Default Connection String to Inject the Applications Database Context
            // to the Collection of Services and Return Them
            return services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseSqlServer(configuration.GetDefaultConnectionString()));
        }

        /// <summary>
        /// Injects the Identity Services with a Custom User Class to the Collection of Services.
        /// </summary>
        /// <returns> The <see cref="IServiceCollection"/> so that additional calls can be chained. </returns>
        public static IServiceCollection AddCustomIdentity(this IServiceCollection services)
        {
            services
                // Inject the Identity Services with a Custom User Class
                .AddIdentity<User, IdentityRole>(options =>
                {
                    options.Password.RequireDigit = false;
                    options.Password.RequireNonAlphanumeric = false;
                    options.Password.RequireUppercase = false;
                    options.Password.RequireLowercase = false;
                    options.Password.RequiredLength = 4;
                })
                .AddEntityFrameworkStores<ApplicationDbContext>();

            // Return the Collection of Services
            return services;
        }

        /// <summary>
        /// Injects Configuration Settings to the Collection of Services.
        /// </summary>
        /// <param name="services"> The App's Collection of Services. </param>
        /// <param name="configuration"> A Collection of the Applications Configuration Providers. </param>
        /// <returns> The <see cref="IServiceCollection"/> so that additional calls can be chained. </returns>
        public static IServiceCollection AddConfigurationSettings(this IServiceCollection services,
            IConfiguration configuration)
        {
            // Inject the JWT Section of the App Settings to Services
            // so that the Secret Key contained in the Section can be used to Generate JWT Tokens
            services.Configure<JwtOptions>(
                configuration.GetSection(JwtOptions.Jwt));

            // Return the Collection of Services
            return services;
        }

        /// <summary>
        /// Injects Jwt Authentication Services to the Collection of Services.
        /// </summary>
        /// <param name="services"> The App's Collection of Services. </param>
        /// <param name="configuration"> A Collection of the Applications Configuration Providers. </param>
        /// <returns> The <see cref="IServiceCollection"/> so that additional calls can be chained. </returns>
        public static IServiceCollection AddJwtAuthentication(this IServiceCollection services,
            IConfiguration configuration)
        {
            // Encode the characters of the Secret Key into a Sequence of Bytes
            // and Store it in a Variable
            var securityKey = Encoding.ASCII.GetBytes(configuration.GetSection(JwtOptions.Jwt)[JwtOptions.JwtKey]!);

            // Inject the Jwt Authentication Services
            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.RequireHttpsMetadata = false;
                    options.SaveToken = true;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(securityKey),
                        ValidateIssuer = false,
                        ValidateAudience = false
                    };
                });

            // Return the Collection of Services
            return services;
        }

        /// <summary>
        /// Injects this Application's Services and Repositories to the Collection of Services.
        /// </summary>
        /// <returns> The <see cref="IServiceCollection"/> so that additional calls can be chained. </returns>
        public static IServiceCollection AddApplicationService(this IServiceCollection services)
        {
            services
                // Inject the Identity Service
                .AddTransient<IIdentityService, IdentityService>()
                // Inject the Augmentations Repository
                .AddTransient<IAugmentationRepository, AugmentationRepository>()
                // Inject the Link Generation Service for Augmentations
                .AddTransient<ILinkGenerationService<AugResponseModel>, AugmentationLinkGenerationService>()
                // Inject the PDF Generation Service for Augmentations
                .AddTransient<IPDFGenerationService<AugResponseModel>, AugmentationPDFGenerationService>()
                // Inject Routing
                .AddRouting()
                // Inject the HttpContext Accessor
                .AddHttpContextAccessor()
                // Inject Response Caching
                .AddResponseCaching()
                // Inject the Action Filter for Validating Whether an Uploaded File is a CSV File or Not
                .AddScoped<ValidateFileIsCSV>();

            // Return the Collection of Services
            return services;
        }

        /// <summary>
        /// Injects a Swagger Generator to the Collection of Services.
        /// </summary>
        /// <returns> The <see cref="IServiceCollection"/> so that additional calls can be chained. </returns>
        public static IServiceCollection AddSwagger(this IServiceCollection services)
        {
            // Inject a Swagger Generator
            services.AddSwaggerGen(options =>
            {
                // Define a Swagger Document
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "AugmentationsAPI",
                    Version = "v1",
                    Description = "An API about Deus Ex's Augmentations",
                    TermsOfService = new Uri("https://example.com/terms"),
                    Contact = new OpenApiContact
                    {
                        Name = "Ali Atanasov",
                        Email = "theumbralpyre@gmail.com",
                    },
                    License = new OpenApiLicense
                    {
                        Name = "License",
                        Url = new Uri("https://example.com/license"),
                    }
                });

                // Define the Security Scheme Type
                // Adding an Authorization Function to the Swagger UI,
                // Allowing the JWT Token to be Used for Authorization in the Swagger UI
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "JWT Token Bearer Authorization. \n Example: \"Bearer {JWT Token}\"",
                });

                // Apply the Security Scheme Defined Above Globally
                options.AddSecurityRequirement(new OpenApiSecurityRequirement()
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

                // Get the Name of the Generated XML Documentation File with Reflection
                var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";

                // Configure Swagger to Use the Comments from the XML Documentation File
                options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
            });

            // Return the Collection of Services
            return services;
        }
    }
}
