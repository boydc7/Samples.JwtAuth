using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.DynamoDBv2;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Opw.HttpExceptions.AspNetCore;
using Opw.HttpExceptions.AspNetCore.Mappers;
using Samples.JwtAuth.Api.Filters;
using Samples.JwtAuth.Api.Middleware;
using Samples.JwtAuth.Api.Models;
using Samples.JwtAuth.Api.Services;
using Samples.JwtAuth.Api.Validations;

namespace Samples.JwtAuth.Api
{
    public class ApiStartup
    {
        public void Configure(IApplicationBuilder app)
        {
            app.UseHttpExceptions()
               .UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(e => e.MapControllers())
               .UseSwagger()
               .UseSwaggerUI(o =>
                             {
                                 o.SwaggerEndpoint("/swagger/auth/swagger.json", "JwtAuth API");
                                 o.RoutePrefix = string.Empty;
                             });

            var applicationLifetime = app.ApplicationServices.GetRequiredService<IHostApplicationLifetime>();

            applicationLifetime.ApplicationStopping.Register(() =>
                                                             {
                                                                 ApplicationShutdownCancellationSource.Instance.TryCancel();

                                                                 Console.WriteLine("*** Shutdown initiated, stopping services...");
                                                             });

            applicationLifetime.ApplicationStarted.Register(() =>
                                                            {
                                                                var log = app.ApplicationServices.GetRequiredService<ILogger<ApiStartup>>();

                                                                log.LogInformation("AuthApi service is running and ready for requests");
                                                            });
        }

        public void ConfigureServices(IServiceCollection services)
        {
            var configuration = Program.BuildConfiguration;
            var authApiConfiguration = new AuthApiConfiguration();

            // We first bind the configuration at the root (to pickup settings that are prefixed by SMP_ but aren't like SMP_SMP__xxxx),
            // then we bind the AuthApi only section (which will overwrite any existing values if present), then we bind the env-var-only config
            // at the root config to ensure we get SMP_ prefixed variables that do not follow the SMP_SMP__xxxx nomenclature and instead are just
            // SMP_xxx
            var envVarOnlyConfig = new ConfigurationBuilder().AddEnvironmentVariables("SMP_").Build();

            configuration.Bind(authApiConfiguration);
            configuration.Bind("AuthApi", authApiConfiguration);
            envVarOnlyConfig.Bind(authApiConfiguration);

            services.AddSingleton(authApiConfiguration);
            services.AddSingleton(authApiConfiguration.Auth);
            services.AddSingleton(authApiConfiguration.Aws);

            // Framework
            services.AddMvc(o => { o.Filters.Add(new ModelAttributeValidationFilter()); })
                    .AddFluentValidation(c => c.RegisterValidatorsFromAssemblyContaining<SignupRequestValidator>(lifetime: ServiceLifetime.Singleton))
                    .AddJsonOptions(x =>
                                    {
                                        x.JsonSerializerOptions.IgnoreNullValues = true;
                                        x.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                                        x.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                                        x.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
                                        x.JsonSerializerOptions.ReadCommentHandling = JsonCommentHandling.Skip;
                                        x.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
                                    })
                    .SetCompatibilityVersion(CompatibilityVersion.Latest)
                    .AddHttpExceptions(xo =>
                                       {
#if DEBUG
                                           xo.IncludeExceptionDetails = _ => true;
#else
                                           xo.IncludeExceptionDetails = _ => false;
#endif

                                           xo.ShouldLogException = _ => false;

                                           xo.ExceptionMapper<AuthApiException, AuthApiExceptionMapper>();
                                           xo.ExceptionMapper<ValidationException, AuthApiExceptionMapper>();
                                           xo.ExceptionMapper<Exception, ProblemDetailsExceptionMapper<Exception>>();
                                       });

            services.AddSwaggerGen(c =>
                                   {
                                       c.SwaggerDoc("auth", new OpenApiInfo
                                                            {
                                                                Version = "v1",
                                                                Title = "Simple JwtAuth API",
                                                                Description = "Jwt Auth service related APIs"
                                                            });

                                       foreach (var xmlFile in Directory.EnumerateFiles(AppContext.BaseDirectory, "*.xml", SearchOption.TopDirectoryOnly))
                                       {
                                           c.IncludeXmlComments(xmlFile);
                                       }
                                   });

            // Auth
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(b =>
                                  {
                                      b.RequireHttpsMetadata = false;
                                      b.SaveToken = true;

                                      b.TokenValidationParameters = new TokenValidationParameters
                                                                    {
                                                                        ValidateIssuer = true,
                                                                        ValidateAudience = true,
                                                                        ValidateLifetime = true,
                                                                        RequireAudience = true,
                                                                        RequireExpirationTime = true,
                                                                        RequireSignedTokens = true,
                                                                        ValidateIssuerSigningKey = true,
                                                                        ValidIssuer = authApiConfiguration.Auth.JwtIssuer,
                                                                        ValidAudience = authApiConfiguration.Auth.JwtAudience,
                                                                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authApiConfiguration.Auth.JwtSecretKey)),
                                                                        TokenDecryptionKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authApiConfiguration.Auth.JwtEncryptKey)),
                                                                        ClockSkew = TimeSpan.FromSeconds(30)
                                                                    };
                                  });

            // Services
            services.AddSingleton<IPasswordValidator, MinLengthPasswordValidator>()
                    .AddSingleton<IPasswordValidator, NumericCharacterPasswordValidator>()
                    .AddSingleton<IPasswordValidator, SpecialCharacterPasswordValidator>()
                    .AddSingleton<IPasswordValidator, AlphaCharacterPasswordValidator>()
                    .AddSingleton<IPasswordValidator, PriorUsedPasswordValidator>()
                    .AddSingleton<IPasswordValidationService, CompositePasswordValidationService>()
                    .AddSingleton<IUserLoginValidator, UserLockedLoginValidator>()
                    .AddSingleton<IUserLoginValidator, LoginAttemptsLoginValidator>()
                    .AddSingleton<IUserLoginValidator, UserLockedRecentlyLoginValidator>()
                    .AddSingleton<IUserLoginValidator, SecretValidLoginValidator>()
                    .AddSingleton<IUserLoginValidationService, CompositeUserLoginValidationService>()
                    .AddSingleton<IAuthenticateService, DefaultAuthenticateService>()
                    .AddSingleton<IUserService, DynamoUserService>()
                    .AddSingleton<IUserTokenService, DefaultUserTokenService>()
                    .AddSingleton<IPasswordService, DynamoPasswordService>();

            // Data...
            services.AddDefaultAWSOptions(configuration.GetAWSOptions());

            if (string.IsNullOrEmpty(authApiConfiguration.Aws.Dynamo.ServiceUrl))
            {
                services.AddAWSService<IAmazonDynamoDB>();
            }
            else
            {
                var dynamoOptions = configuration.GetAWSOptions();

                dynamoOptions.DefaultClientConfig.ServiceURL = authApiConfiguration.Aws.Dynamo.ServiceUrl.Trim();

                services.AddAWSService<IAmazonDynamoDB>(dynamoOptions);
            }

            services.AddSingleton<IAuthDynProvider, AuthDynProvider>();
        }
    }
}
