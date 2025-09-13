using HybridCast_ServerRelay.Services;
using HybridCast_ServerRelay.Storage;
using HybridCast_ServerRelay.Utility;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHostedService<CleanEmptyRoomsService>();
builder.Services.AddSingleton<IRoomStorage, RoomStorage>();

var app = builder.Build();

var webSocketOptions = new WebSocketOptions
{
    KeepAliveTimeout = TimeSpan.FromSeconds(30),
    KeepAliveInterval = TimeSpan.FromMinutes(2)
};

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseWebSockets(webSocketOptions);
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllers();


//string output = System.Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { Id = Guid.NewGuid().ToString(), Name = "HybridCast" }) + "::EndJson::" + RandomUtility.GenerateRoomCode(64) ) );
//Console.WriteLine(output);

app.Run();
