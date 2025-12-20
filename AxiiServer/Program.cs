using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Concurrent;
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

var currentDirectory = Directory.GetCurrentDirectory();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(currentDirectory),
    RequestPath = ""
});

app.UseCors();
app.UseRouting();

app.MapHub<CommandHub>("/commandHub");

app.MapGet("/", () => Results.Content(File.ReadAllText("index.html"), "text/html"));
app.MapGet("/monitor", () => Results.Content(File.ReadAllText("monitor.html"), "text/html"));

app.MapPost("/broadcast", async (HttpContext context, IHubContext<CommandHub> hubContext) =>
{
    using var reader = new StreamReader(context.Request.Body);
    string body = await reader.ReadToEndAsync();

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

    await hubContext.Clients.All.SendAsync("ExecuteCommand", action);

    return Results.Ok(new
    {
        success = true,
        message = $"Comando '{action}' enviado para {clientCount} computador(es)",
        clients = clientCount
    });
});

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

app.MapGet("/performance", () =>
{
    var performanceData = CommandHub.PerformanceStats
        .Select(kvp => new
        {
            connectionId = kvp.Key,
            computerName = CommandHub.ConnectedClients.ContainsKey(kvp.Key) 
                ? CommandHub.ConnectedClients[kvp.Key] 
                : "Desconhecido",
            cpuUsage = kvp.Value.CpuUsagePercent,
            ramUsage = kvp.Value.RamUsagePercent,
            ramUsed = kvp.Value.RamUsedMB,
            ramTotal = kvp.Value.RamTotalMB,
            lastUpdate = kvp.Value.LastUpdate
        })
        .OrderBy(x => x.computerName)
        .ToList();

    return Results.Ok(new
    {
        success = true,
        count = performanceData.Count,
        clients = performanceData
    });
});

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
Console.WriteLine("AXII DESKTOP");
Console.WriteLine($"Acesse: http://{GetLocalIPAddress()}:5000");
Console.WriteLine("Interface: http://localhost:5000");
Console.WriteLine("Monitor: http://localhost:5000/monitor");
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
    public static ConcurrentDictionary<string, string> ConnectedClients = new();
    public static ConcurrentDictionary<string, PerformanceInfo> PerformanceStats = new();

    public override async Task OnConnectedAsync()
    {
        string computerName = Context.GetHttpContext()?.Request.Query["computer"].ToString() ?? "PC-Desconhecido";
        ConnectedClients[Context.ConnectionId] = computerName;

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Cliente conectado: {computerName} (ID: {Context.ConnectionId})");
        Console.WriteLine($"   Total de clientes: {ConnectedClients.Count}");
        Console.ResetColor();

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception exception)
    {
        if (ConnectedClients.TryRemove(Context.ConnectionId, out string computerName))
        {
            PerformanceStats.TryRemove(Context.ConnectionId, out _);

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Cliente desconectado: {computerName}");
            Console.WriteLine($"   Total de clientes: {ConnectedClients.Count}");
            Console.ResetColor();
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task UpdatePerformanceStats(double cpuUsage, double ramUsagePercent, double ramUsedMB, double ramTotalMB)
    {
        var perfInfo = new PerformanceInfo
        {
            CpuUsagePercent = cpuUsage,
            RamUsagePercent = ramUsagePercent,
            RamUsedMB = ramUsedMB,
            RamTotalMB = ramTotalMB,
            LastUpdate = DateTime.Now
        };

        PerformanceStats[Context.ConnectionId] = perfInfo;

        // Notificar todos os clientes conectados ao monitor
        await Clients.All.SendAsync("PerformanceUpdate", Context.ConnectionId, perfInfo);
    }
}

public class PerformanceInfo
{
    public double CpuUsagePercent { get; set; }
    public double RamUsagePercent { get; set; }
    public double RamUsedMB { get; set; }
    public double RamTotalMB { get; set; }
    public DateTime LastUpdate { get; set; }
}