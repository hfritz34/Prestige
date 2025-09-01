using System.Security.Claims;
using System.IO.Compression;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.Azure.Cosmos;
using System.Text.Json;
using Prestige.Api.Auth;
using Prestige.Api.Data;
using Prestige.Api.Domain;
using Prestige.Api.Endpoints.Prestige;
using Prestige.Api.Endpoints.Profile;
using Prestige.Api.Endpoints.Spotify;
using Prestige.Api.Endpoints.Rating;
using Prestige.Api.Endpoints.Library;

// using Prestige.Api.Endpoints.AlbumEndpoints;
// using Prestige.Api.Endpoints.ArtistEndpoints;
// using Prestige.Api.Endpoints.TrackEndpoints;
using Prestige.Api.Endpoints.UserEndpoints;
using Prestige.Api.Endpoints.FriendshipEndpoints;
using Prestige.Api.Services;
using AspNetCoreRateLimit;
using Hangfire;
using Hangfire.Redis.StackExchange;
using Hangfire.SqlServer;

namespace Prestige.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            AddApiExplorer(builder);
            AddSwaggerGen(builder);
            AddResponseCompression(builder);
            AddCaching(builder);
            AddRateLimiting(builder);
            AddHangfire(builder);
            AddDbContext(builder);
            AddCosmosDB(builder);
            AddServices(builder);
            AddControllers(builder);
            AddCorsPolicy(builder);
            AddAuthentication(builder);
            AddAuthorization(builder);
            AddCurrentUser(builder);

            RunApp(builder);
        }

        private static void AddApiExplorer(WebApplicationBuilder builder)
        {
            builder.Services.AddEndpointsApiExplorer();
        }

        private static void AddResponseCompression(WebApplicationBuilder builder)
        {
            builder.Services.AddResponseCompression(options =>
            {
                options.EnableForHttps = true;
                options.Providers.Add<BrotliCompressionProvider>();
                options.Providers.Add<GzipCompressionProvider>();
                options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
                    new[] { "application/json", "text/json", "application/xml", "text/xml" });
            });

            builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
            {
                options.Level = CompressionLevel.Optimal;
            });

            builder.Services.Configure<GzipCompressionProviderOptions>(options =>
            {
                options.Level = CompressionLevel.Optimal;
            });
        }

        private static string ResolveSqlConnectionString(IConfiguration configuration)
        {
            var value = configuration.GetConnectionString("AZURE_SQL_CONNECTIONSTRING");
            if (string.IsNullOrWhiteSpace(value))
            {
                // Fallback to flat env var/app setting if not provided under ConnectionStrings
                value = configuration["AZURE_SQL_CONNECTIONSTRING"];
            }
            return value ?? string.Empty;
        }

        private static void AddCaching(WebApplicationBuilder builder)
        {
            var redisConnection = builder.Configuration.GetConnectionString("Redis");
            
            if (!string.IsNullOrEmpty(redisConnection))
            {
                // Use Redis for distributed caching
                builder.Services.AddStackExchangeRedisCache(options =>
                {
                    options.Configuration = redisConnection;
                    options.InstanceName = "Prestige";
                });
            }
            else
            {
                // Fallback to in-memory caching for development
                builder.Services.AddDistributedMemoryCache();
            }
            
            // Register the cache service
            builder.Services.AddScoped<PrestigeCacheService>();
        }

        private static void AddRateLimiting(WebApplicationBuilder builder)
        {
            // Configure rate limiting for memory cache
            builder.Services.AddMemoryCache();
            
            // Configure IP rate limiting
            builder.Services.Configure<IpRateLimitOptions>(options =>
            {
                options.EnableEndpointRateLimiting = true;
                options.StackBlockedRequests = false;
                options.HttpStatusCode = 429;
                options.RealIpHeader = "X-Real-IP";
                options.ClientIdHeader = "X-ClientId";
                options.GeneralRules = new List<RateLimitRule>
                {
                    // Rating endpoints - 30 requests per minute
                    new RateLimitRule
                    {
                        Endpoint = "POST:/api/rating/*",
                        Period = "1m",
                        Limit = 30
                    },
                    new RateLimitRule
                    {
                        Endpoint = "PUT:/api/rating/*",
                        Period = "1m",
                        Limit = 30
                    },
                    new RateLimitRule
                    {
                        Endpoint = "DELETE:/api/rating/*",
                        Period = "1m",
                        Limit = 30
                    },
                    // Library endpoints - 100 requests per minute
                    new RateLimitRule
                    {
                        Endpoint = "GET:/api/library/*",
                        Period = "1m",
                        Limit = 100
                    },
                    new RateLimitRule
                    {
                        Endpoint = "POST:/api/library/*",
                        Period = "1m",
                        Limit = 100
                    },
                    // Spotify endpoints - 60 requests per minute
                    new RateLimitRule
                    {
                        Endpoint = "*:/api/spotify/*",
                        Period = "1m",
                        Limit = 60
                    },
                    // General API limit - 300 requests per minute
                    new RateLimitRule
                    {
                        Endpoint = "*",
                        Period = "1m",
                        Limit = 300
                    }
                };
                options.QuotaExceededResponse = new QuotaExceededResponse
                {
                    Content = "{{\"error\":\"Too many requests. Please try again later.\",\"retryAfter\":\"{1}\"}}",
                    ContentType = "application/json",
                    StatusCode = 429
                };
            });
            
            // Configure client-specific rate limiting
            builder.Services.Configure<ClientRateLimitOptions>(options =>
            {
                options.EnableEndpointRateLimiting = true;
                options.StackBlockedRequests = false;
                options.HttpStatusCode = 429;
                options.ClientIdHeader = "X-ClientId";
            });
            
            // Add rate limit stores
            builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
            builder.Services.AddSingleton<IClientPolicyStore, MemoryCacheClientPolicyStore>();
            builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
            builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
            builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
        }

        private static void AddHangfire(WebApplicationBuilder builder)
        {
            // Feature flag to disable Hangfire from configuration without code changes
            var disableHangfire = builder.Configuration["DisableHangfire"];
            if (!string.IsNullOrEmpty(disableHangfire) && disableHangfire.Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // Try to use Redis first, fall back to SQL if Redis is not configured
            var redisConnection = builder.Configuration.GetConnectionString("Redis");
            
            if (!string.IsNullOrEmpty(redisConnection))
            {
                // Use Redis for Hangfire storage (more cost-effective than SQL)
                builder.Services.AddHangfire(config =>
                {
                    config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                          .UseSimpleAssemblyNameTypeSerializer()
                          .UseRecommendedSerializerSettings()
                          .UseRedisStorage(redisConnection, new RedisStorageOptions
                          {
                              Prefix = "hangfire:",
                              InvisibilityTimeout = TimeSpan.FromMinutes(5),
                              ExpiryCheckInterval = TimeSpan.FromHours(1),
                              DeletedListSize = 1000,
                              SucceededListSize = 1000
                          });
                });
            }
            else
            {
                // Fall back to SQL Server if Redis is not available
                var connectionString = ResolveSqlConnectionString(builder.Configuration);

                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    // Skip Hangfire initialization if no storage is configured
                    return;
                }
                    
                // Configure Hangfire with SQL Server (legacy - consider migrating to Redis)
                builder.Services.AddHangfire(config =>
                {
                    config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                          .UseSimpleAssemblyNameTypeSerializer()
                          .UseRecommendedSerializerSettings()
                          .UseSqlServerStorage(connectionString);
                });
            }
            
            // Add Hangfire server
            builder.Services.AddHangfireServer(options =>
            {
                options.WorkerCount = Environment.ProcessorCount * 2;
                options.Queues = new[] { "critical", "default", "background" };
            });
        }

        private static void AddSwaggerGen(WebApplicationBuilder builder)
        {
            builder.Services.AddSwaggerGen(opt =>
            {
                opt.SwaggerDoc("v1", new OpenApiInfo { Title = "PrestigeApi", Version = "v1" });
                opt.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    In = ParameterLocation.Header,
                    Description = "Please enter token",
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    BearerFormat = "JWT",
                    Scheme = "bearer"
                });

                opt.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type=ReferenceType.SecurityScheme,
                                Id="Bearer"
                            }
                        },
                        new string[]{}
                    }
                });
            });
        }

        private static void AddDbContext(WebApplicationBuilder builder)
        {
            var connection = ResolveSqlConnectionString(builder.Configuration);

            if (!string.IsNullOrWhiteSpace(connection))
            {
                // Use connection pooling for better performance
                builder.Services.AddDbContextPool<PrestigeContext>(options =>
                    options.UseSqlServer(connection, sqlOptions =>
                    {
                        sqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 3,
                            maxRetryDelay: TimeSpan.FromSeconds(30),
                            errorNumbersToAdd: null);
                        sqlOptions.CommandTimeout(30);
                    })
                    .EnableSensitiveDataLogging(builder.Environment.IsDevelopment()),
                    poolSize: 128);
            }
            else if (builder.Environment.IsDevelopment())
            {
                // Fallback to SQLite for local development when SQL connection is not configured
                builder.Services.AddDbContextPool<PrestigeContext>(options =>
                    options.UseSqlite("Data Source=prestige_dev.db")
                           .EnableSensitiveDataLogging(true),
                    poolSize: 128);
            }
            else
            {
                throw new InvalidOperationException("Database connection string is missing. Set 'ConnectionStrings:AZURE_SQL_CONNECTIONSTRING' or environment variable 'ConnectionStrings__AZURE_SQL_CONNECTIONSTRING' (or 'AZURE_SQL_CONNECTIONSTRING').");
            }
        }

        private static void AddCosmosDB(WebApplicationBuilder builder)
        {
            var cosmosConnectionString = builder.Configuration.GetConnectionString("CosmosDB");
            if (string.IsNullOrWhiteSpace(cosmosConnectionString))
            {
                // Skip CosmosDB if not configured - recently updated will be empty
                builder.Services.AddSingleton<CosmosClient>(_ => null!);
                return;
            }

            var cosmosClientOptions = new CosmosClientOptions
            {
                ConnectionMode = ConnectionMode.Gateway,
                LimitToEndpoint = false
            };

            builder.Services.AddSingleton<CosmosClient>(sp =>
                new CosmosClient(cosmosConnectionString, cosmosClientOptions));
        }

        private static void AddServices(WebApplicationBuilder builder)
        {
            builder.Services.AddScoped<UserServices>();
            builder.Services.AddScoped<SpotifyServices>();
            builder.Services.AddScoped<ProfileServices>();
            builder.Services.AddScoped<FriendshipService>();
            builder.Services.AddScoped<PrestigeServices>();
            builder.Services.AddScoped<RatingServices>();
            builder.Services.AddScoped<Endpoints.Search.SearchServices>();
            builder.Services.AddScoped<LibraryServices>();
            builder.Services.AddScoped<RatingBackgroundJobs>();
            builder.Services.AddScoped<RecentlyPlayedCosmosService>();
        }

        private static void AddControllers(WebApplicationBuilder builder)
        {
            builder.Services.AddControllers();
        }

        private static void AddCorsPolicy(WebApplicationBuilder builder)
        {
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                    policy
                        .WithOrigins("http://localhost:5173")
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials());
            });
        }

        private static void AddAuthentication(WebApplicationBuilder builder)
        {
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Authority = $"https://{builder.Configuration["Auth0:Domain"]}";
                    options.Audience = builder.Configuration["Auth0:Audience"];
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        NameClaimType = ClaimTypes.NameIdentifier,
                        ValidateIssuerSigningKey = true,
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidIssuer = $"https://{builder.Configuration["Auth0:Domain"]}/",
                        ValidAudience = builder.Configuration["Auth0:Audience"]
                    };
                    options.Events = new JwtBearerEvents
                    {
                        OnAuthenticationFailed = context =>
                        {
                            Console.WriteLine($"OnAuthenticationFailed: {context.Exception.Message}");
                            return Task.CompletedTask;
                        },
                        OnTokenValidated = context =>
                        {
                            Console.WriteLine($"Token validated for user: {context.Principal?.Identity?.Name}");
                            return Task.CompletedTask;
                        }
                    };
                });
        }

        private static void AddAuthorization(WebApplicationBuilder builder)
        {
            builder.Services.AddSingleton<IAuthorizationHandler, HasScopeHandler>();
        }

        private static void AddCurrentUser(WebApplicationBuilder builder)
        {
            builder.Services.AddTransient<IHttpContextAccessor, HttpContextAccessor>();
            builder.Services.AddTransient(sp =>
            {
                var accessor = sp.GetRequiredService<IHttpContextAccessor>();
                var user = accessor?.HttpContext?.User;
                return user ?? throw new InvalidOperationException("User not found");
            });
        }

        private static void RunApp(WebApplicationBuilder builder)
        {
            var app = builder.Build();

            // Add response compression middleware
            app.UseResponseCompression();
            
            // Add rate limiting middleware
            app.UseIpRateLimiting();

            // Add health check before other middleware
            app.MapGet("/api/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
               .AllowAnonymous()
               .WithName("HealthCheck");

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
                app.UseCors("AllowAll");
                
                // Add Hangfire dashboard for development
                app.UseHangfireDashboard("/hangfire", new DashboardOptions
                {
                    Authorization = new[] { new HangfireAuthorizationFilter() }
                });
            }
            else 
            {
                app.UseCors("AllowAll");
            }

            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers().RequireAuthorization();

            // Schedule recurring background jobs only if Hangfire is configured (i.e., SQL connection exists)
            var disableHangfire = app.Configuration["DisableHangfire"];
            if ((string.IsNullOrEmpty(disableHangfire) || !disableHangfire.Equals("true", StringComparison.OrdinalIgnoreCase))
                && !string.IsNullOrWhiteSpace(ResolveSqlConnectionString(app.Configuration)))
            {
                RecurringJob.AddOrUpdate<RatingBackgroundJobs>(
                    "cleanup-old-comparisons",
                    x => x.CleanupOldComparisonsAsync(),
                    Cron.Daily(2)); // Run daily at 2 AM

                RecurringJob.AddOrUpdate<RatingBackgroundJobs>(
                    "optimize-database",
                    x => x.OptimizeDatabaseAsync(),
                    Cron.Weekly(DayOfWeek.Sunday, 3)); // Run weekly on Sunday at 3 AM
            }

            app.Run();
        }
    }
}
