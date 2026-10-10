using Ipkiss.Api.Accounts;
using Ipkiss.Api.Http;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<Bank>();

var app = builder.Build();
app.MapBankEndpoints();
app.Run();

public partial class Program;
