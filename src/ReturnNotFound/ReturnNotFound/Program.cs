using KissLog.AspNetCore;
using KissLog.CloudListeners.Auth;
using KissLog.CloudListeners.RequestLogsListener;
using KissLog.Formatters;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Console;

using ReturnNotFound.Components.Account;
using ReturnNotFound.Data;
using ReturnNotFound.LogConfigurations;

using Serilog;

namespace ReturnNotFound;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var hostEnvironment = builder.Environment;

        builder
            .Configuration.SetBasePath(hostEnvironment.ContentRootPath)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile(
                $"appsettings.{hostEnvironment.EnvironmentName}.json",
                optional: true,
                reloadOnChange: true
            )
            .AddEnvironmentVariables();

        if (hostEnvironment.IsDevelopment())
        {
            builder.Configuration.AddUserSecrets<Program>(optional: true);
        }
        var loggingSection = builder.Configuration.GetSection("Logging");
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .AddConfiguration(loggingSection)
                .AddSimpleConsole()
                .AddConsoleFormatter<CsvLogFormatterConfiguration, ConsoleFormatterOptions>();
        });

        var logger = loggerFactory.CreateLogger<Program>();
        Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(builder.Configuration).CreateLogger();
        builder.Host.UseSerilog();
        builder.Services.AddLogging(provider =>
        {
            provider
                .AddKissLog(options =>
                {
                    options.Formatter = (FormatterArgs args) =>
                    {
                        if (args.Exception == null)
                            return args.DefaultValue;

                        var exceptionStr = new ExceptionFormatter().Format(args.Exception, args.Logger);
                        return string.Join(Environment.NewLine, new[] { args.DefaultValue, exceptionStr });
                    };
                });
        });
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents()
            .AddInteractiveWebAssemblyComponents();
        builder.Services.AddFluentUIComponents();

        builder.Services.AddCascadingAuthenticationState();
        builder.Services.AddScoped<IdentityUserAccessor>();
        builder.Services.AddScoped<IdentityRedirectManager>();
        builder.Services.AddScoped<AuthenticationStateProvider, PersistingRevalidatingAuthenticationStateProvider>();

        builder.Services.AddAuthentication(options =>
        {
            options.DefaultScheme = IdentityConstants.ApplicationScheme;
            options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
        })
            .AddIdentityCookies();

        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Cadeia de conexão 'DefaultConnection' não encontrada.");
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite(connectionString));
        builder.Services.AddDatabaseDeveloperPageExceptionFilter();

        builder.Services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseWebAssemblyDebugging();
            app.UseMigrationsEndPoint();
        }
        else
        {
            //app.UseExceptionHandler("/Error");
            //app.UseHsts();
            app.UseWebAssemblyDebugging();
            app.UseMigrationsEndPoint();
            app.UseDeveloperExceptionPage();
        }

        app.UseHttpsRedirection();

        app.UseStaticFiles();
        app.UseAntiforgery();

        app.MapRazorComponents<ReturnNotFound.Components.App>()
            .AddInteractiveServerRenderMode()
            .AddInteractiveWebAssemblyRenderMode()
            .AddAdditionalAssemblies(typeof(Counter).Assembly);

        app.MapAdditionalIdentityEndpoints();
        app.UseKissLogMiddleware(options =>
        {
            options.Listeners.Add(new RequestLogsApiListener(new Application(
                builder.Configuration["KissLog.OrganizationId"],
                builder.Configuration["KissLog.ApplicationId"])
            )
            {
                ApiUrl = builder.Configuration["KissLog.ApiUrl"]
            });
        });
        app.Run();
    }
}