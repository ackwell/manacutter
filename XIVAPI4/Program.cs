var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// TODO: configurable data path
var lumina = new Lumina.GameData("C:\\Program Files (x86)\\SquareEnix\\FINAL FANTASY XIV - A Realm Reborn\\game\\sqpack");
builder.Services.AddSingleton(lumina);

builder.Services.AddReaders();

builder.Services.AddSheetDefinitionProviders(builder.Configuration);

// TODO: Make this more granular.
builder.Services.AddCors(options => {
	options.AddDefaultPolicy(builder => builder.AllowAnyOrigin().AllowAnyHeader());
});

var app = builder.Build();

if (builder.Environment.IsDevelopment()) {
	app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();

app.UseCors();

app.MapControllers();

app.Run();
