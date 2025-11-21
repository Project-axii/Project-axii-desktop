using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddSignalR();

var app = builder.Build();

// Servir arquivos estáticos (HTML, CSS, JS, imagens)
var currentDirectory = Directory.GetCurrentDirectory();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(currentDirectory),
    RequestPath = ""
});

app.UseCors();
app.UseRouting();

app.MapHub<CommandHub>("/commandHub");

// Endpoint principal - servir o HTML
app.MapGet("/", () => Results.Content(File.ReadAllText("index.html"), "text/html"));

// Endpoint para broadcast de comandos para todos os PCs
app.MapPost("/broadcast", async (HttpContext context, IHubContext<CommandHub> hubContext) =>
{
    using var reader = new StreamReader(context.Request.Body);
    string body = await reader.ReadToEndAsync();

    // Remover aspas do JSON
    string action = body.Replace("\"", "").Trim();

    int clientCount = CommandHub.ConnectedClients.Count;

    if (clientCount == 0)
    {
        return Results.Ok(new
        {
            success = false,
            message = "Nenhum computador conectado!",
            clients = 0
        });
    }

    // Enviar comando para TODOS os clientes
    await hubContext.Clients.All.SendAsync("ExecuteCommand", action);

    return Results.Ok(new
    {
        success = true,
        message = $"Comando '{action}' enviado para {clientCount} computador(es)",
        clients = clientCount
    });
});

// Endpoint para listar clientes conectados
app.MapGet("/clients", () =>
{
    return Results.Ok(new
    {
        count = CommandHub.ConnectedClients.Count,
        clients = CommandHub.ConnectedClients.Select(c => new
        {
            id = c.Key,
            name = c.Value,
            connected = true
        }).ToList()
    });
});

// Endpoint de status do servidor
app.MapGet("/status", () =>
{
    return Results.Ok(new
    {
        status = "online",
        clients = CommandHub.ConnectedClients.Count,
        uptime = DateTime.Now.ToString("HH:mm:ss")
    });
});

Console.Clear();
Console.WriteLine("AXII DESKTOP - SERVIDOR MULTI-PC");
Console.WriteLine($"Acesse: http://{GetLocalIPAddress()}:5000");
Console.WriteLine("Interface: http://localhost:5000║");
Console.WriteLine();

app.Run("http://0.0.0.0:5000");

string GetLocalIPAddress()
{
    var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
    foreach (var ip in host.AddressList)
    {
        if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            return ip.ToString();
        }
    }
    return "localhost";
}

public class CommandHub : Hub
{
    public static Dictionary<string, string> ConnectedClients = new Dictionary<string, string>();

    public override async Task OnConnectedAsync()
    {
        string computerName = Context.GetHttpContext()?.Request.Query["computer"].ToString() ?? "PC-Desconhecido";
        ConnectedClients[Context.ConnectionId] = computerName;

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"✅ [{DateTime.Now:HH:mm:ss}] Cliente conectado: {computerName} (ID: {Context.ConnectionId})");
        Console.WriteLine($"   Total de clientes: {ConnectedClients.Count}");
        Console.ResetColor();

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception exception)
    {
        if (ConnectedClients.TryGetValue(Context.ConnectionId, out string computerName))
        {
            ConnectedClients.Remove(Context.ConnectionId);

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ [{DateTime.Now:HH:mm:ss}] Cliente desconectado: {computerName}");
            Console.WriteLine($"   Total de clientes: {ConnectedClients.Count}");
            Console.ResetColor();
        }

        await base.OnDisconnectedAsync(exception);
    }
}