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

        static async Task Main(string[] args)
        {
            Console.Title = $"Axii Desktop Client - {computerName}";

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("");
            Console.WriteLine("         AXII DESKTOP - CLIENTE EXECUTOR ");
            Console.WriteLine("");
            Console.WriteLine($"Computador: {computerName.PadRight(43)} ");
            Console.WriteLine($"Usuário: {userName.PadRight(46)} ");
            Console.WriteLine("");
            Console.ResetColor();

            Console.Write("Digite a URL do JSON de configuração (ou Enter para usar padrão): ");
            string jsonUrl = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(jsonUrl))
            {
                jsonUrl = "https://raw.githubusercontent.com/Project-axii/Project-axii-gateway/refs/heads/main/sistema.json";
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Buscando configuração do servidor...");
            Console.ResetColor();

            string serverIp;
            try
            {
                serverIp = await GetServerUrlFromJson(jsonUrl);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Servidor encontrado: {serverIp}");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Erro ao buscar configuração: {ex.Message}");
                Console.ResetColor();
                Console.WriteLine("Pressione qualquer tecla para sair...");
                Console.ReadKey();
                return;
            }

            string serverUrl = $"{serverIp.TrimEnd('/')}/commandHub?computer={Uri.EscapeDataString(computerName)}";

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Configurando conexão...");
            Console.ResetColor();

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
                    TimeSpan.FromSeconds(10)
                })
                .Build();

            connection.Reconnecting += error =>
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"\n[{DateTime.Now:HH:mm:ss}] Reconectando ao servidor...");
                Console.ResetColor();
                return Task.CompletedTask;
            };

            connection.Reconnected += connectionId =>
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Reconectado com sucesso!");
                Console.ResetColor();
                return Task.CompletedTask;
            };

            connection.Closed += async error =>
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n[{DateTime.Now:HH:mm:ss}] Conexão perdida!");
                Console.ResetColor();

                await Task.Delay(5000);

                try
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Tentando reconectar...");
                    Console.ResetColor();
                    await connection.StartAsync();
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Falha na reconexão: {ex.Message}");
                    Console.ResetColor();
                }
            };

            connection.On<string>("ExecuteCommand", async (action) =>
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Comando recebido: {action}");
                Console.ResetColor();

                string result = await Task.Run(() => ExecuteBatScript(action));

                if (result.Contains("❌"))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"{result}");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"{result}");
                }
                Console.ResetColor();
            });

            try
            {
                await connection.StartAsync();

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("CONECTADO COM SUCESSO!");
                Console.WriteLine($"Servidor: {serverUrl.PadRight(46)} ");
                Console.WriteLine("Status: Aguardando comandos...");
                Console.ResetColor();

                Console.WriteLine("Pressione CTRL+C para desconectar e sair.");

                await Task.Delay(-1);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("ERRO DE CONEXÃO");
                Console.WriteLine($"{ex.Message.PadRight(58)} ");
                Console.WriteLine("Verifique se:");
                Console.WriteLine("1. O servidor está rodando");
                Console.WriteLine("2. A URL do JSON está correta");
                Console.WriteLine("3. O firewall não está bloqueando");
                Console.ResetColor();

                Console.WriteLine("Pressione qualquer tecla para sair...");
                Console.ReadKey();
            }
        }

        static async Task<string> GetServerUrlFromJson(string jsonUrl)
        {
            using (HttpClient client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(10);

                client.DefaultRequestHeaders.Add("ngrok-skip-browser-warning", "true");

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
                    CreateNoWindow = false,
                    WindowStyle = ProcessWindowStyle.Normal
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
    }
}