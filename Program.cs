using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

namespace MiniAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddEndpointsApiExplorer();

            var secretKey = "supergeheimer!Schluessel12345_superlang";
            var seckey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(secretKey));

            builder.Services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = "MeinApiServer",
                        ValidAudience = "MeinApiClient",
                        IssuerSigningKey = seckey
                    };
                });

            builder.Services.AddAuthorization();

            var app = builder.Build();

            var products = new List<Product>
            {
                new Product(1, "Laptop", 999.99m, 10),
                new Product(2, "Smartphone", 499.99m, 25),
                new Product(3, "Tablet", 299.99m, 15),
                new Product(4, "Monitor", 199.99m, 8)
            };

            int nextPID = 5;
            string apiKey = "test1234";

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapPost("/login", (LoginRequest req) =>
            {
                if (req.Username == "user" && req.Password == "password")
                {
                    var claims = new[]
                    {
                        new Claim(ClaimTypes.Name, req.Username),
                        new Claim(ClaimTypes.Role, "User"),
                        new Claim("FIA", "46")
                    };

                    var credentials = new SigningCredentials(seckey, SecurityAlgorithms.HmacSha256);
                    var token = new JwtSecurityToken(
                        issuer: "MeinApiServer",
                        audience: "MeinApiClient",
                        claims: claims,
                        expires: DateTime.Now.AddMinutes(60),
                        signingCredentials: credentials);

                    var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
                    return Results.Ok(new { token = tokenString });
                }

                return Results.Unauthorized();
            });

            app.MapGet("/products", () => Results.Ok(products));

            app.MapGet("/products/{id}", (int id, string Key) =>
            {
                if (Key == apiKey)
                {
                    var product = products.FirstOrDefault(p => p.PID == id);
                    return product is not null ? Results.Ok(product) : Results.NotFound();
                }
                else
                {
                    return Results.Unauthorized();
                }
            });

            app.MapPost("/products", (Product newProduct) =>
            {
                var product = newProduct with { PID = nextPID };
                products.Add(product);
                nextPID++;
                return Results.Created($"/products/{product.PID}", product);
            });

            app.MapPost("/products/batch", (List<Product> newProducts) =>
            {
                var createdProducts = new List<Product>();
                foreach (var newProduct in newProducts)
                {
                    var product = newProduct with { PID = nextPID };
                    products.Add(product);
                    createdProducts.Add(product);
                    nextPID++;
                }
                return Results.Created("/products/batch", createdProducts);
            });

            app.MapPut("/products/{id}", (int id, Product updatedProduct) =>
            {
                var index = products.FindIndex(p => p.PID == id);
                if (index == -1)
                {
                    return Results.NotFound();
                }
                var product = updatedProduct with { PID = id };
                products[index] = product;
                return Results.Ok(product);
            });

            app.MapDelete("/products/{id}", (int id) =>
            {
                var index = products.FindIndex(p => p.PID == id);
                if (index == -1)
                {
                    return Results.NotFound();
                }
                products.RemoveAt(index);
                return Results.NoContent();
            });

            app.MapGet("/privat" , () => Results.Ok("Geheimer Inhalt nur für Authentifizierte!"))
               .RequireAuthorization();






            app.Run();
        }

        public record LoginRequest(string Username, string Password);
        record Product(int PID, string Name, decimal Price, int Stock);
    }
}
