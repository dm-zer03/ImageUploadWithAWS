using Amazon;
using Amazon.S3;
using FileUpload.API.Models;
using FileUpload.API.Services;

var builder = WebApplication.CreateBuilder(args);

// ─── AWS S3 Configuration ────────────────────────────────────────────────────
builder.Services.Configure<AwsSettings>(builder.Configuration.GetSection("AWS"));

var awsSection = builder.Configuration.GetSection("AWS");
var accessKey = awsSection["AccessKey"]!;
var secretKey = awsSection["SecretKey"]!;
var region = awsSection["Region"]!;

builder.Services.AddSingleton<IAmazonS3>(_ =>
    new AmazonS3Client(accessKey, secretKey, RegionEndpoint.GetBySystemName(region))
);

// ─── App Services ─────────────────────────────────────────────────────────────
builder.Services.AddScoped<IS3Service, S3Service>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ─── CORS for Angular ─────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAngular");
app.UseAuthorization();
app.MapControllers();

app.Run();
