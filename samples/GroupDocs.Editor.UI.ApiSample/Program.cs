using GroupDocs.Editor.UI.Api.Extensions;
using GroupDocs.Editor.UI.Api.Services.Implementation;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddEditorControllers();
builder.Services.AddEditorSwagger(opt =>
{
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    opt.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
});
builder.Services.AddEditor<LocalStorage>(builder.Configuration);
builder.Services.AddCors(p => p.AddPolicy("corsApp", policy =>
{
    policy.WithOrigins("*").AllowAnyMethod().AllowAnyHeader().AllowAnyOrigin();
}));
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("corsApp");
app.UseAuthorization();

app.MapControllers();

app.Run();
