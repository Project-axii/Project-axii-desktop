using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace BatGeneratorServer
{
    class Program
    {
        static HttpListener listener;

        static async Task Main(string[] args)
        {
            listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:5000/");
            listener.Start();
            Console.WriteLine("🚀 Servidor C# rodando em http://localhost:5000");
            Console.WriteLine("📂 Abra o arquivo 'index.html' no navegador");

            while (true)
            {
                var context = await listener.GetContextAsync();
                _ = ProcessRequest(context);
            }
        }

        static async Task ProcessRequest(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            response.AddHeader("Access-Control-Allow-Origin", "*");
            response.AddHeader("Access-Control-Allow-Methods", "POST, GET, OPTIONS");
            response.AddHeader("Access-Control-Allow-Headers", "Content-Type");

            if (request.HttpMethod == "OPTIONS")
            {
                response.StatusCode = 200;
                response.Close();
                return;
            }

            if (request.HttpMethod == "POST" && request.Url.AbsolutePath == "/execute")
            {
                using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                {
                    string body = await reader.ReadToEndAsync();
                    string action = body.Replace("\"", "").Trim();

                    string result = ExecuteBatScript(action);

                    byte[] buffer = Encoding.UTF8.GetBytes(result);
                    response.ContentLength64 = buffer.Length;
                    response.ContentType = "application/json";
                    await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                }
            }

            response.Close();
        }

        static string ExecuteBatScript(string action)
        {
            string batFileName = "temp_script.bat";
            string batContent = "";
            string description = "";

            switch (action)
            {
                case "notepad":
                    batContent = "@echo off\nstart notepad.exe";
                    description = "Bloco de Notas aberto";
                    break;
                case "datetime":
                    batContent = "@echo off\necho Data e Hora Atuais: %date% %time%\npause";
                    description = "Janela de Data/Hora exibida";
                    break;
                case "file":
                    batContent = "@echo off\necho Este é um arquivo de texto criado pelo .bat > created_file.txt\necho Arquivo created_file.txt criado com sucesso!\npause";
                    description = "Arquivo created_file.txt criado";
                    break;
                case "vscode":
                    batContent = "@echo off\nstart code";
                    description = "Visual Studio Code aberto";
                    break;
                case "visualstudio":
                    batContent = "@echo off\nstart devenv";
                    description = "Visual Studio aberto";
                    break;
                case "laragon":
                    batContent = "@echo off\nstart \"\" \"C:\\laragon\\laragon.exe\"";
                    description = "Laragon aberto";
                    break;
                case "packettracer":
                    batContent = "@echo off\nstart \"\" \"C:\\ProgramData\\Microsoft\\Windows\\Start Menu\\Programs\\PacketTracer6.lnk";
                    description = "Cisco Packet Tracer aberto";
                    break;
                case "ngrok":
                    batContent = "@echo off\nstart cmd /k ngrok http 80";
                    description = "Ngrok iniciado (porta 80)";
                    break;
                default:
                    return "{\"success\": false, \"message\": \"Ação inválida\"}";
            }

            try
            {
                File.WriteAllText(batFileName, batContent.Replace("\\n", Environment.NewLine));

                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = batFileName;
                psi.UseShellExecute = true;

                using (Process process = Process.Start(psi))
                {
                    process.WaitForExit();
                }

                if (File.Exists(batFileName))
                {
                    File.Delete(batFileName);
                }

                return $"{{\"success\": true, \"message\": \"{description}\"}}";
            }
            catch (Exception ex)
            {
                return $"{{\"success\": false, \"message\": \"Erro: {ex.Message}\"}}";
            }
        }
    }
}