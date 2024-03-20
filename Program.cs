var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddSingleton<VisitService>();
builder.Services.AddTransient<WakeOnLanServersBuilder>();
builder.Services.Configure<WakeOnLanSettings>(builder.Configuration.GetSection("WakeOnLan"));
builder.Services.AddLogging(loggingBuilder =>
{
    loggingBuilder.AddConfiguration(builder.Configuration.GetSection("Logging"));
    loggingBuilder.AddFile(AppContext.BaseDirectory);
});

builder.Services.AddHostedService<NetworkFinderWorkerService>();
builder.Services.AddHostedService<TcpListenerWorkerService>();
builder.Services.AddHostedService<PingWorkerService>();

var app = builder.Build();

app.UseVisitor();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller}/{action=Index}/{id?}");

app.UseAuthorization();

app.MapRazorPages();

app.Run();
