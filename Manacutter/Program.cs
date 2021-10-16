var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services
	.AddDefinitions(builder.Configuration)
	.AddREST()
	.AddGraphQL()
	.AddReaders();

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
