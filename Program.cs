using TyresStorage.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

//  Зарегистрируем репозиторий в DI-контейнере
builder.Services.AddSingleton<IDeviceRepository, JsonDeviceRepository>();

// регистрируем IHttpClientFactory
builder.Services.AddHttpClient();

// builder.Services.AddScoped<IDeviceHttpClient, DeviceHttpClient>();
builder.Services.AddTransient<IDeviceHttpClient, DeviceHttpClient>(); //  реальный - при публикации надо раскомментить
//builder.Services.AddTransient<IDeviceHttpClient, FakeDeviceHttpClient>(); // используем заглушку

// Региструем сервис который проверяет все устройства каждые 30 секунд.
builder.Services.AddHostedService<SchedulingService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();
