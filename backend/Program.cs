using backend.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHttpClient<EmbeddingService>();
builder.Services.AddHttpClient<ChatService>();
builder.Services.AddSingleton<QdrantService>();
builder.Services.AddSingleton<ChunkingService>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

app.UseCors();
app.MapControllers();
app.Run();
