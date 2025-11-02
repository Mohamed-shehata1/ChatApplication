using Microsoft.EntityFrameworkCore;
using MyChatApp.Data;
using MyChatApp.Models;
using MyChatApp.Services;

namespace MyChatApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            // Configuration
            builder.Services.Configure<OpenAiOptions>(builder.Configuration.GetSection("OpenAI"));

            // DbContext - SQLite
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=chat.db";
            builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));

            // HttpClient for OpenAI
            builder.Services.AddHttpClient<OpenAiService>();

            // Application services
            builder.Services.AddScoped<ChatService>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            // Ensure database created
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.EnsureCreated();
            }

            app.UseRouting();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Chat}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
