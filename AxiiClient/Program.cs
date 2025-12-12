using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;

namespace AxiiDesktopClient
{
    class Program
    {
        static HubConnection connection;
        static string computerName = Environment.MachineName;
        static string userName = Environment.UserName;
        static string configUrl = "https://raw.githubusercontent.com/Project-axii/Project-axii-gateway/refs/heads/main/sistema.json";
        static string logFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "axii_client.log");

        static async Task Main(string[] args)
        {
            await StartClient();
        }

        static async Task StartClient()
        {
            LogMessage("AXII DESKTOP CLIENT INICIADO");
            LogMessage($"Computador: {computerName}");
            LogMessage($"Usuário: {userName}");

            while (true)
            {
                try
                {
                    LogMessage("Buscando configuração do servidor...");

                    string serverIp = await GetServerUrlFromJson(configUrl);
                    LogMessage($"Servidor encontrado: {serverIp}");

                    string serverUrl = $"{serverIp.TrimEnd('/')}/commandHub?computer={Uri.EscapeDataString(computerName)}";

                    LogMessage("Configurando conexão...");

                    connection = new HubConnectionBuilder()
                        .WithUrl(serverUrl, options =>
                        {
                            options.Headers.Add("ngrok-skip-browser-warning", "true");
                        })
                        .WithAutomaticReconnect(new[]
                        {
                            TimeSpan.Zero,
                            TimeSpan.FromSeconds(2),
                            TimeSpan.FromSeconds(5),
                            TimeSpan.FromSeconds(10),
                            TimeSpan.FromSeconds(30)
                        })
                        .Build();

                    SetupConnectionHandlers();

                    await connection.StartAsync();

                    LogMessage("CONECTADO COM SUCESSO!");
                    LogMessage($"Servidor: {serverUrl}");
                    LogMessage("Status: Aguardando comandos...");

                    await Task.Delay(-1);
                }
                catch (Exception ex)
                {
                    LogMessage($"ERRO: {ex.Message}");
                    LogMessage("Tentando reconectar em 30 segundos...");

                    await Task.Delay(30000);
                }
            }
        }

        static void SetupConnectionHandlers()
        {
            connection.Reconnecting += error =>
            {
                LogMessage("Reconectando ao servidor...");
                return Task.CompletedTask;
            };

            connection.Reconnected += connectionId =>
            {
                LogMessage("Reconectado com sucesso!");
                return Task.CompletedTask;
            };

            connection.Closed += async error =>
            {
                LogMessage("Conexão perdida! Tentando reconectar...");

                await Task.Delay(5000);

                try
                {
                    LogMessage("Tentando reconectar...");
                    await connection.StartAsync();
                }
                catch (Exception ex)
                {
                    LogMessage($"Falha na reconexão: {ex.Message}");
                }
            };

            connection.On<string>("ExecuteCommand", async (action) =>
            {
                LogMessage($"▶ Comando recebido: {action}");

                string result = await Task.Run(() => ExecuteBatScript(action));
                LogMessage(result);
            });
        }

        static async Task<string> GetServerUrlFromJson(string jsonUrl)
        {
            using (HttpClient client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(15);
                client.DefaultRequestHeaders.Add("ngrok-skip-browser-warning", "true");
                client.DefaultRequestHeaders.Add("User-Agent", "AxiiDesktopClient/1.0");

                HttpResponseMessage response = await client.GetAsync(jsonUrl);
                response.EnsureSuccessStatusCode();

                string jsonContent = await response.Content.ReadAsStringAsync();

                using (JsonDocument doc = JsonDocument.Parse(jsonContent))
                {
                    JsonElement root = doc.RootElement;

                    if (root.TryGetProperty("status", out JsonElement statusElement))
                    {
                        string status = statusElement.GetString();
                        if (status != "success")
                        {
                            throw new Exception($"Status inválido no JSON: {status}");
                        }
                    }

                    if (root.TryGetProperty("link", out JsonElement linkElement))
                    {
                        string link = linkElement.GetString();
                        
                        if (string.IsNullOrWhiteSpace(link))
                        {
                            throw new Exception("Link vazio no JSON");
                        }

                        return link;
                    }
                    else
                    {
                        throw new Exception("Propriedade 'link' não encontrada no JSON");
                    }
                }
            }
        }

        static string ExecuteBatScript(string action)
        {
            string batFileName = $"axii_temp_{Guid.NewGuid()}.bat";
            string batContent = "";
            string description = "";

            switch (action.ToLower())
            {
                case "notepad":
                    batContent = "@echo off\nstart notepad.exe";
                    description = "Bloco de Notas aberto com sucesso!";
                    break;

                case "datetime":
                    batContent = "@echo off\ntitle Axii Desktop - Data e Hora\ncolor 0A\necho.\necho \necho            AXII DESKTOP - INFORMACOES DO SISTEMA\necho \necho.\necho  Computador: %COMPUTERNAME%\necho  Usuario: %USERNAME%\necho.\necho  Data: %date%\necho  Hora: %time%\necho.\necho \necho.\npause";
                    description = "Janela de Data/Hora exibida com sucesso!";
                    break;

                case "file":
                    batContent = "@echo off\necho  > created_file.txt\necho           ARQUIVO CRIADO PELO AXII DESKTOP SYSTEM >> created_file.txt\necho  >> created_file.txt\necho. >> created_file.txt\necho Data de Criacao: %date% %time% >> created_file.txt\necho Computador: %COMPUTERNAME% >> created_file.txt\necho Usuario: %USERNAME% >> created_file.txt\necho Sistema: %OS% >> created_file.txt\necho. >> created_file.txt\necho Este arquivo foi criado automaticamente pelo sistema de >> created_file.txt\necho controle remoto Axii Desktop. >> created_file.txt\necho. >> created_file.txt\necho  >> created_file.txt\ntitle Axii Desktop - Arquivo Criado\ncolor 0A\necho.\necho Arquivo 'created_file.txt' criado com sucesso!\necho.\necho Conteudo do arquivo:\necho.\ntype created_file.txt\necho.\npause";
                    description = "Arquivo 'created_file.txt' criado com sucesso!";
                    break;

                case "vscode":
                    batContent = "@echo off\nstart code";
                    description = "Visual Studio Code aberto com sucesso!";
                    break;

                case "visualstudio":
                    batContent = "@echo off\nstart devenv";
                    description = "Visual Studio aberto com sucesso!";
                    break;

                case "laragon":
                    batContent = "@echo off\nif exist \"C:\\laragon\\laragon.exe\" (\n    start \"\" \"C:\\laragon\\laragon.exe\"\n    echo Laragon iniciado!\n) else (\n    echo Laragon nao encontrado em C:\\laragon\\laragon.exe\n    pause\n)";
                    description = "Laragon aberto com sucesso!";
                    break;

                case "packettracer":
                    batContent = "@echo off\nif exist \"C:\\Program Files\\Cisco Packet Tracer 8.2\\bin\\PacketTracer.exe\" (\n    start \"\" \"C:\\Program Files\\Cisco Packet Tracer 8.2\\bin\\PacketTracer.exe\"\n) else if exist \"C:\\Program Files\\Cisco Packet Tracer\\bin\\PacketTracer.exe\" (\n    start \"\" \"C:\\Program Files\\Cisco Packet Tracer\\bin\\PacketTracer.exe\"\n) else if exist \"C:\\Program Files (x86)\\Cisco Packet Tracer\\bin\\PacketTracer.exe\" (\n    start \"\" \"C:\\Program Files (x86)\\Cisco Packet Tracer\\bin\\PacketTracer.exe\"\n) else (\n    echo Packet Tracer nao encontrado\n    pause\n)";
                    description = "Cisco Packet Tracer aberto com sucesso!";
                    break;

                case "ngrok":
                    batContent = "@echo off\ntitle Axii Desktop - Ngrok Tunnel\ncolor 0B\necho.\necho \necho              AXII DESKTOP - INICIANDO NGROK\necho \necho.\nngrok http 5000";
                    description = "Ngrok iniciado na porta 5000!";
                    break;

                default:
                    return $"Ação inválida: '{action}'";
            }

            try
            {
                File.WriteAllText(batFileName, batContent.Replace("\\n", Environment.NewLine));

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = batFileName,
                    UseShellExecute = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                using (Process process = Process.Start(psi))
                {
                    if (action.ToLower() == "datetime" || action.ToLower() == "file")
                    {
                        process?.WaitForExit();
                    }
                }

                Task.Run(async () =>
                {
                    await Task.Delay(3000);
                    try
                    {
                        if (File.Exists(batFileName))
                        {
                            File.Delete(batFileName);
                        }
                    }
                    catch { }
                });

                return description;
            }
            catch (Exception ex)
            {
                return $"Erro ao executar: {ex.Message}";
            }
        }

        static void LogMessage(string message)
        {
            try
            {
                string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\n";
                File.AppendAllText(logFile, logEntry);
            }
            catch { }
        }
    }
}