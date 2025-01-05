
using IdentityApi.Domain;
using IdentityApi.Features.CreateAccount;
using IdentityApi.Features.GetNewToken;
using IdentityApi.Features.LoginAccount;
using IdentityApi.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);
builder.Serevices.AddDbContext<AppDbContext>
    (options => options.UseSqlServer(builder.Configuration.GetConnection("Default")));

builder.Serevices.AddIdentity<AppUser, IdentityRole>().AddEntityFrameworkStores<AppDbContext>();
builder.Serevices.AddJwtAuthenticationService(builder.Configuration);
builder.Serevices.AddGrpc().AddJsontranscoding();
builder.Serevices.AddValidatorsFromAssembly(typeof(Program).Assembly);
builder.Serevices.AddMediatR(c => c.RegisterServicefromAssembly(typeof(Program).Assembly));
builder.Serevices.AddScoped(p =>
{
    var config = new TypeAdapterConfig();
    CreateAccountMapperConfig.Register(config);
    LoginAccountMapperConfig.Register(config);
    return config;

});

builder.Serevices.AddScoped<IMapper, ServiceMapper>();
builder.Services.AddGrpc();
var app = builder.Build();

using(var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<AppDbContext>();
    var userManager = services.GetRequireService<UserManager<AppUser>>();
    await context.Database.MigrateAsync();
    await DatabaseSeeder.SeedAsync(context,userManager);
}
app.MapGrpcService<CreateAccountService>();
app.MapGrpcService<LoginAccountService>();
app.MapGrpcService<GetNewTokenService>();
app.Run();
