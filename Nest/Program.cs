using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Serilog;
using Serilog.Events;
using Nest.DAL;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configure the connection string and register MediaDbContext
var connectionString = builder.Configuration.GetConnectionString("MediaDbContextConnection")
    ?? throw new InvalidOperationException("Connection string 'MediaDbContextConnection' not found.");

builder.Services.AddDbContext<MediaDbContext>(options =>
{
    options.UseSqlite(connectionString);  // Use SQLite with the provided connection string
});

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true).AddEntityFrameworkStores<MediaDbContext>();

// Register IPictureRepository with its concrete implementation PictureRepository
builder.Services.AddScoped<IPictureRepository, PictureRepository>();
builder.Services.AddScoped<INoteRepository, NoteRepository>();
builder.Services.AddScoped<ICommentRepository, CommentRepository>();

builder.Services.AddRazorPages();
builder.Services.AddSession();

var app = builder.Build();
// Autentisering i program cs 
/*builder.Services.AddDefaultIdentity<IdentityUser>(options => 
    options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<DbContext>();


 */ 
 // DBInit.cs
 //await DBInit.SeedAsync(app);

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseStaticFiles();

app.UseAuthentication(); 
app.UseAuthorization();
app.MapRazorPages();

// Optional: Use a default controller route if needed
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"); 

// Start the app
app.Run();
