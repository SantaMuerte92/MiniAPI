using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace MiniAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddEndpointsApiExplorer();

            var app = builder.Build();

            //JWT Konfiguration 
            // Geheimer Schlüssel für die JWT-Signierung
            var secretKey= "supergeheimer!Schluessel12345"; // Sollte lang und komplex sein

            //Symetrische Sicherheitschlüssel erstellen
            var seckey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(secretKey));

            //Authentifizierungsdienste hinzufügen
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options=>
                {
                    //Token-Validierungsparameter konfigurieren
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        //Aussteller validieren
                        ValidateIssuer = false,
                        //Zielgruppen validieren
                        ValidateAudience = false,
                        //Gültigkeitsdauer validieren
                        ValidateLifetime = true,
                        //Signaturschlüssel validieren
                        ValidateIssuerSigningKey = true,
                        //Signaturschlüssel festlegen
                        IssuerSigningKey = seckey
                    };
                });



            builder.Services.AddEndpointsApiExplorer();




                            //In-Memory-Datenbank (Liste von Produkten)
                            var products = new List<Product>
            {
                new Product(1, "Laptop", 999.99m, 10),
                new Product(2, "Smartphone", 499.99m, 25),
                new Product(3, "Tablet", 299.99m, 15),
                new Product(4, "Monitor", 199.99m, 8)
            
            };

            int nextPID = 5;
            string apiKey = "test1234";


            //Get /products - Alle Produkte abrufen
            app.MapGet("/products", () => Results.Ok(products));

            //Get /products/{id} - Produkt nach ID abrufen
            app.MapGet("/products/{id}", (int id, string Key) =>
            {
                if (Key == apiKey)
                {
                    var product = products.FirstOrDefault(p => p.PID == id);
                    return product is not null ? Results.Ok(product) : Results.NotFound();
                }
                else { 
                    return Results.Unauthorized();
                }
            });

            //Post /products - Neues Produkt erstellen
            app.MapPost("/products", (Product newProduct) =>
            {
                var product = newProduct with {PID = nextPID };
                products.Add(product);
                nextPID++;
                return Results.Created($"/products/{product.PID}", product);
            });

            //Post /products/batch - Mehrere Produkte erstellen
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

            //PUT /products/ID - Produkt aktualisieren
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

            //DELETE /products/ID - Produkt löschen
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







            app.Run();
        }

        record Product(int PID, string Name, decimal Price, int Stock);


    }
}
