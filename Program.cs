using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using RaceEvent.Data;
using RaceEvent.Models.Interface;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(connectionString));
}

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();


builder.Services.AddScoped<IProfileImageService, ProfileImageService>();
builder.Services.AddSingleton<RaceEvent.Services.IBlobStorageService, RaceEvent.Services.BlobStorageService>();

builder.Services.AddControllersWithViews()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
 
    {
     c.SwaggerDoc("v1", new OpenApiInfo
     {
        Title = "RaceEvent API",
        Version = "v1",
        Description = "API for managing events, categories, enrolments and results."
    });

    c.DocInclusionPredicate((docName, api) =>
        api.RelativePath != null && api.RelativePath.StartsWith("api/"));

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFile));
    });

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    if (db.Database.IsRelational())
    db.Database.Migrate();
    else
    db.Database.EnsureCreated(); //in-memory database used by test

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var roles = new[] { "Admin", "Organiser", "Participant" };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    string email = "admin@Example.com";
    string password = "Mkhalipho@123";

    if (await userManager.FindByEmailAsync(email) == null)
    {
        var user = new ApplicationUser();

        user.UserName = email;
        user.Email = email;
        user.EmailConfirmed = true;

        await userManager.CreateAsync(user, password);
        await userManager.AddToRoleAsync(user, "Admin");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
public partial class Program { } // for integration testing
