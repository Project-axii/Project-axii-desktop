using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.AspNetCore.SignalR.Client;

namespace AxiiDesktopClient
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    public class MainForm : Form
    {
        private HubConnection connection;
        private string computerName = Environment.MachineName;
        private string userName = Environment.UserName;
        private string defaultConfigPath = "axii_default_config.txt";

        private Button btnGitHub;
        private Button btnDefault;
        private Button btnCustom;
        private Button btnSetDefault;
        private Button btnExit;
        private Label lblTitle;
        private Label lblComputer;
        private Label lblUser;
        private Label lblStatus;
        private TextBox txtLog;
        private Panel panelTop;
        private Panel panelButtons;

        public MainForm()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            this.Text = $"Axii Desktop Client - {computerName}";
            this.Size = new Size(600, 550);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(30, 30, 30);

            // Panel Superior
            panelTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 120,
                BackColor = Color.FromArgb(20, 20, 20)
            };

            lblTitle = new Label
            {
                Text = "AXII DESKTOP - CLIENTE EXECUTOR",
                Location = new Point(20, 15),
                Size = new Size(560, 30),
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.Cyan,
                TextAlign = ContentAlignment.MiddleCenter
            };

            lblComputer = new Label
            {
                Text = $"Computador: {computerName}",
                Location = new Point(20, 55),
                Size = new Size(560, 25),
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.White
            };

            lblUser = new Label
            {
                Text = $"Usuário: {userName}",
                Location = new Point(20, 80),
                Size = new Size(560, 25),
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.White
            };

            panelTop.Controls.AddRange(new Control[] { lblTitle, lblComputer, lblUser });

            // Panel de Botões
            panelButtons = new Panel
            {
                Location = new Point(20, 140),
                Size = new Size(540, 220),
                BackColor = Color.FromArgb(40, 40, 40)
            };

            btnGitHub = CreateButton("Usar URL do GitHub", new Point(20, 20), Color.FromArgb(0, 120, 212));
            btnGitHub.Click += BtnGitHub_Click;

            btnDefault = CreateButton("Usar Configuração Padrão", new Point(20, 65), Color.FromArgb(0, 150, 0));
            btnDefault.Click += BtnDefault_Click;

            btnCustom = CreateButton("Digitar URL Personalizada", new Point(20, 110), Color.FromArgb(200, 120, 0));
            btnCustom.Click += BtnCustom_Click;

            btnSetDefault = CreateButton("Definir URL Padrão", new Point(20, 155), Color.FromArgb(120, 80, 200));
            btnSetDefault.Click += BtnSetDefault_Click;

            panelButtons.Controls.AddRange(new Control[] { btnGitHub, btnDefault, btnCustom, btnSetDefault });

            // Status Label
            lblStatus = new Label
            {
                Text = "Status: Aguardando seleção...",
                Location = new Point(20, 370),
                Size = new Size(560, 25),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.Yellow,
                TextAlign = ContentAlignment.MiddleLeft
            };

            // Log TextBox
            txtLog = new TextBox
            {
                Location = new Point(20, 405),
                Size = new Size(540, 80),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.FromArgb(20, 20, 20),
                ForeColor = Color.LightGray,
                Font = new Font("Consolas", 9),
                ReadOnly = true
            };

            // Botão Sair
            btnExit = new Button
            {
                Text = "Sair",
                Location = new Point(470, 495),
                Size = new Size(90, 35),
                BackColor = Color.FromArgb(180, 0, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnExit.FlatAppearance.BorderSize = 0;
            btnExit.Click += (s, e) => Application.Exit();

            this.Controls.AddRange(new Control[] { panelTop, panelButtons, lblStatus, txtLog, btnExit });

            UpdateDefaultButtonStatus();
        }

        private Button CreateButton(string text, Point location, Color color)
        {
            Button btn = new Button
            {
                Text = text,
                Location = location,
                Size = new Size(500, 35),
                BackColor = color,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private void UpdateDefaultButtonStatus()
        {
            string defaultUrl = LoadDefaultUrl();
            if (string.IsNullOrWhiteSpace(defaultUrl))
            {
                btnDefault.Enabled = false;
                btnDefault.BackColor = Color.FromArgb(80, 80, 80);
                btnDefault.Text = "Usar Configuração Padrão (Não configurado)";
            }
            else
            {
                btnDefault.Enabled = true;
                btnDefault.BackColor = Color.FromArgb(0, 150, 0);
                btnDefault.Text = "Usar Configuração Padrão";
            }
        }

        private async void BtnGitHub_Click(object sender, EventArgs e)
        {
            string url = "https://raw.githubusercontent.com/Project-axii/Project-axii-gateway/refs/heads/main/sistema.json";
            await ConnectToServer(url);
        }

        private async void BtnDefault_Click(object sender, EventArgs e)
        {
            string url = LoadDefaultUrl();
            if (string.IsNullOrWhiteSpace(url))
            {
                MessageBox.Show("Nenhuma URL padrão configurada!", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            await ConnectToServer(url);
        }

        private async void BtnCustom_Click(object sender, EventArgs e)
        {
            using (var inputForm = new InputForm("Digite a URL do JSON de configuração:"))
            {
                if (inputForm.ShowDialog() == DialogResult.OK)
                {
                    string url = inputForm.InputText;
                    if (!string.IsNullOrWhiteSpace(url))
                    {
                        await ConnectToServer(url);
                    }
                }
            }
        }

        private void BtnSetDefault_Click(object sender, EventArgs e)
        {
            using (var inputForm = new InputForm("Digite a URL que deseja salvar como padrão:"))
            {
                if (inputForm.ShowDialog() == DialogResult.OK)
                {
                    string url = inputForm.InputText;
                    if (!string.IsNullOrWhiteSpace(url))
                    {
                        SaveDefaultUrl(url);
                        MessageBox.Show("URL padrão salva com sucesso!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        UpdateDefaultButtonStatus();
                    }
                }
            }
        }

        private async Task ConnectToServer(string jsonUrl)
        {
            DisableButtons();
            UpdateStatus("Buscando configuração do servidor...", Color.Yellow);
            AppendLog($"[{DateTime.Now:HH:mm:ss}] Conectando usando: {jsonUrl}");

            try
            {
                string serverIp = await GetServerUrlFromJson(jsonUrl);
                UpdateStatus($"Servidor encontrado: {serverIp}", Color.Lime);
                AppendLog($"[{DateTime.Now:HH:mm:ss}] Servidor encontrado: {serverIp}");

                string serverUrl = $"{serverIp.TrimEnd('/')}/commandHub?computer={Uri.EscapeDataString(computerName)}";

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

                SetupConnectionEvents();

                await connection.StartAsync();

                UpdateStatus("CONECTADO COM SUCESSO!", Color.Lime);
                AppendLog($"[{DateTime.Now:HH:mm:ss}] Conectado com sucesso!");
                AppendLog($"[{DateTime.Now:HH:mm:ss}] Status: Aguardando comandos...");
            }
            catch (Exception ex)
            {
                UpdateStatus("ERRO DE CONEXÃO", Color.Red);
                AppendLog($"[{DateTime.Now:HH:mm:ss}] ERRO: {ex.Message}");
                MessageBox.Show($"Erro ao conectar:\n{ex.Message}", "Erro de Conexão", MessageBoxButtons.OK, MessageBoxIcon.Error);
                EnableButtons();
            }
        }

        private void SetupConnectionEvents()
        {
            connection.Reconnecting += error =>
            {
                this.Invoke((MethodInvoker)delegate
                {
                    UpdateStatus("Reconectando ao servidor...", Color.Yellow);
                    AppendLog($"[{DateTime.Now:HH:mm:ss}] Reconectando...");
                });
                return Task.CompletedTask;
            };

            connection.Reconnected += connectionId =>
            {
                this.Invoke((MethodInvoker)delegate
                {
                    UpdateStatus("Reconectado com sucesso!", Color.Lime);
                    AppendLog($"[{DateTime.Now:HH:mm:ss}] Reconectado com sucesso!");
                });
                return Task.CompletedTask;
            };

            connection.Closed += async error =>
            {
                this.Invoke((MethodInvoker)delegate
                {
                    UpdateStatus("Conexão perdida!", Color.Red);
                    AppendLog($"[{DateTime.Now:HH:mm:ss}] Conexão perdida!");
                    EnableButtons();
                });
            };

            connection.On<string>("ExecuteCommand", async (action) =>
            {
                this.Invoke((MethodInvoker)delegate
                {
                    AppendLog($"[{DateTime.Now:HH:mm:ss}] Comando recebido: {action}");
                });

                string result = await Task.Run(() => ExecuteBatScript(action));

                this.Invoke((MethodInvoker)delegate
                {
                    AppendLog($"[{DateTime.Now:HH:mm:ss}] {result}");
                });
            });
        }

        private async Task<string> GetServerUrlFromJson(string jsonUrl)
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

        private string ExecuteBatScript(string action)
        {
            string batFileName = $"axii_temp_{Guid.NewGuid()}.bat";
            string batContent = "";
            string description = "";

            switch (action.ToLower())
            {
                case "notepad":
                    batContent = "@echo off\nstart notepad.exe";
                    description = "✓ Bloco de Notas aberto com sucesso!";
                    break;

                case "datetime":
                    batContent = "@echo off\ntitle Axii Desktop - Data e Hora\ncolor 0A\necho.\necho \necho            AXII DESKTOP - INFORMACOES DO SISTEMA\necho \necho.\necho  Computador: %COMPUTERNAME%\necho  Usuario: %USERNAME%\necho.\necho  Data: %date%\necho  Hora: %time%\necho.\necho \necho.\npause";
                    description = "✓ Janela de Data/Hora exibida com sucesso!";
                    break;

                case "file":
                    batContent = "@echo off\necho  > created_file.txt\necho           ARQUIVO CRIADO PELO AXII DESKTOP SYSTEM >> created_file.txt\necho  >> created_file.txt\necho. >> created_file.txt\necho Data de Criacao: %date% %time% >> created_file.txt\necho Computador: %COMPUTERNAME% >> created_file.txt\necho Usuario: %USERNAME% >> created_file.txt\necho Sistema: %OS% >> created_file.txt\necho. >> created_file.txt\necho Este arquivo foi criado automaticamente pelo sistema de >> created_file.txt\necho controle remoto Axii Desktop. >> created_file.txt\necho. >> created_file.txt\necho  >> created_file.txt\ntitle Axii Desktop - Arquivo Criado\ncolor 0A\necho.\necho Arquivo 'created_file.txt' criado com sucesso!\necho.\necho Conteudo do arquivo:\necho.\ntype created_file.txt\necho.\npause";
                    description = "✓ Arquivo 'created_file.txt' criado com sucesso!";
                    break;

                case "vscode":
                    batContent = "@echo off\nstart code";
                    description = "✓ Visual Studio Code aberto com sucesso!";
                    break;

                case "visualstudio":
                    batContent = "@echo off\nstart devenv";
                    description = "✓ Visual Studio aberto com sucesso!";
                    break;

                case "laragon":
                    batContent = "@echo off\nif exist \"C:\\laragon\\laragon.exe\" (\n    start \"\" \"C:\\laragon\\laragon.exe\"\n    echo Laragon iniciado!\n) else (\n    echo Laragon nao encontrado em C:\\laragon\\laragon.exe\n    pause\n)";
                    description = "✓ Laragon aberto com sucesso!";
                    break;

                case "packettracer":
                    batContent = "@echo off\nif exist \"C:\\Program Files\\Cisco Packet Tracer 8.2\\bin\\PacketTracer.exe\" (\n    start \"\" \"C:\\Program Files\\Cisco Packet Tracer 8.2\\bin\\PacketTracer.exe\"\n) else if exist \"C:\\Program Files\\Cisco Packet Tracer\\bin\\PacketTracer.exe\" (\n    start \"\" \"C:\\Program Files\\Cisco Packet Tracer\\bin\\PacketTracer.exe\"\n) else if exist \"C:\\Program Files (x86)\\Cisco Packet Tracer\\bin\\PacketTracer.exe\" (\n    start \"\" \"C:\\Program Files (x86)\\Cisco Packet Tracer\\bin\\PacketTracer.exe\"\n) else (\n    echo Packet Tracer nao encontrado\n    pause\n)";
                    description = "✓ Cisco Packet Tracer aberto com sucesso!";
                    break;

                case "ngrok":
                    batContent = "@echo off\ntitle Axii Desktop - Ngrok Tunnel\ncolor 0B\necho.\necho \necho              AXII DESKTOP - INICIANDO NGROK\necho \necho.\nngrok http 5000";
                    description = "✓ Ngrok iniciado na porta 5000!";
                    break;

                default:
                    return $"❌ Ação inválida: '{action}'";
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
                return $"❌ Erro ao executar: {ex.Message}";
            }
        }

        private string LoadDefaultUrl()
        {
            try
            {
                if (File.Exists(defaultConfigPath))
                {
                    return File.ReadAllText(defaultConfigPath).Trim();
                }
            }
            catch { }

            return string.Empty;
        }

        private void SaveDefaultUrl(string url)
        {
            try
            {
                File.WriteAllText(defaultConfigPath, url);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao salvar configuração: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateStatus(string message, Color color)
        {
            lblStatus.Text = $"Status: {message}";
            lblStatus.ForeColor = color;
        }

        private void AppendLog(string message)
        {
            txtLog.AppendText(message + Environment.NewLine);
        }

        private void DisableButtons()
        {
            btnGitHub.Enabled = false;
            btnDefault.Enabled = false;
            btnCustom.Enabled = false;
            btnSetDefault.Enabled = false;
        }

        private void EnableButtons()
        {
            btnGitHub.Enabled = true;
            btnCustom.Enabled = true;
            btnSetDefault.Enabled = true;
            UpdateDefaultButtonStatus();
        }
    }

    public class InputForm : Form
    {
        private TextBox txtInput;
        private Button btnOk;
        private Button btnCancel;
        private Label lblPrompt;

        public string InputText => txtInput.Text;

        public InputForm(string prompt)
        {
            this.Text = "Axii Desktop Client";
            this.Size = new Size(500, 180);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(30, 30, 30);

            lblPrompt = new Label
            {
                Text = prompt,
                Location = new Point(20, 20),
                Size = new Size(440, 30),
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.White
            };

            txtInput = new TextBox
            {
                Location = new Point(20, 60),
                Size = new Size(440, 30),
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = Color.White
            };

            btnOk = new Button
            {
                Text = "OK",
                Location = new Point(280, 100),
                Size = new Size(90, 35),
                BackColor = Color.FromArgb(0, 120, 212),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                DialogResult = DialogResult.OK
            };
            btnOk.FlatAppearance.BorderSize = 0;

            btnCancel = new Button
            {
                Text = "Cancelar",
                Location = new Point(380, 100),
                Size = new Size(90, 35),
                BackColor = Color.FromArgb(120, 120, 120),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                DialogResult = DialogResult.Cancel
            };
            btnCancel.FlatAppearance.BorderSize = 0;

            this.Controls.AddRange(new Control[] { lblPrompt, txtInput, btnOk, btnCancel });
            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;
        }
    }
}