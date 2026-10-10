using Ipkiss.Api.Accounts;
using Ipkiss.Api.Http;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<Bank>();
builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = false);

var app = builder.Build();
app.UseStatusCodePages("text/plain; charset=utf-8", "0");
app.MapBankEndpoints();
app.Run();

public partial class Program;
