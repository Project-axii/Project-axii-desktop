using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Management;
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
        private HubConnection? connection;
        private string computerName = Environment.MachineName;
        private string userName = Environment.UserName;
        private string defaultConfigPath = "axii_default_config.txt";

        private Panel headerPanel = null!;
        private Label lblTitle = null!;
        private Label lblSubtitle = null!;
        private Panel infoPanel = null!;
        private Label lblStatus = null!;
        private Panel contentPanel = null!;

        // Monitoramento de Performance
        private PerformanceMonitor performanceMonitor;
        private Label lblCpuUsage;
        private Label lblRamUsage;
        private ProgressBar pbCpu;
        private ProgressBar pbRam;
        private Panel statsPanel;

        public MainForm()
        {
            InitializeComponents();
            InitializePerformanceMonitoring();
        }

        private void InitializeComponents()
        {
            this.Text = "Axii Desktop Client";
            this.Size = new Size(1200, 800);
            this.MinimumSize = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(10, 25, 47);
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.Resize += MainForm_Resize;

            // Header
            headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = Color.FromArgb(15, 30, 60)
            };

            // Logo e Título
            Label lblLogo = new Label
            {
                Text = "⬛",
                Location = new Point(30, 20),
                Size = new Size(40, 40),
                Font = new Font("Segoe UI", 24),
                ForeColor = Color.FromArgb(100, 200, 255),
                BackColor = Color.Transparent
            };

            lblTitle = new Label
            {
                Text = "AXII DESKTOP",
                Location = new Point(80, 25),
                Size = new Size(300, 35),
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 200, 255),
                BackColor = Color.Transparent
            };

            lblSubtitle = new Label
            {
                Text = "Cliente Executor",
                Location = new Point(1050, 30),
                Size = new Size(120, 25),
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(150, 170, 200),
                BackColor = Color.Transparent
            };

            headerPanel.Controls.AddRange(new Control[] { lblLogo, lblTitle, lblSubtitle });

            // Painel de Informações
            infoPanel = new Panel
            {
                Location = new Point(40, 120),
                Size = new Size(this.ClientSize.Width - 80, 100),
                BackColor = Color.FromArgb(20, 40, 70),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            infoPanel.Paint += (s, e) => DrawRoundedRectangle(e.Graphics, infoPanel.ClientRectangle, 10, Color.FromArgb(20, 40, 70));

            Label lblComputerIcon = new Label
            {
                Text = "💻",
                Location = new Point(30, 30),
                Size = new Size(40, 40),
                Font = new Font("Segoe UI", 20),
                BackColor = Color.Transparent
            };

            Label lblComputerLabel = new Label
            {
                Text = "Computador",
                Location = new Point(80, 25),
                Size = new Size(150, 20),
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(120, 140, 180),
                BackColor = Color.Transparent
            };

            Label lblComputerName = new Label
            {
                Text = computerName,
                Location = new Point(80, 45),
                Size = new Size(400, 30),
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent
            };

            Label lblUserIcon = new Label
            {
                Text = "👤",
                Location = new Point(600, 30),
                Size = new Size(40, 40),
                Font = new Font("Segoe UI", 20),
                BackColor = Color.Transparent
            };

            Label lblUserLabel = new Label
            {
                Text = "Usuário",
                Location = new Point(650, 25),
                Size = new Size(150, 20),
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(120, 140, 180),
                BackColor = Color.Transparent
            };

            Label lblUserName = new Label
            {
                Text = userName,
                Location = new Point(650, 45),
                Size = new Size(400, 30),
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent
            };

            infoPanel.Controls.AddRange(new Control[] { 
                lblComputerIcon, lblComputerLabel, lblComputerName,
                lblUserIcon, lblUserLabel, lblUserName 
            });

            // Título da seção
            Label lblSectionTitle = new Label
            {
                Text = "Configuração de Conexão",
                Location = new Point(40, 340),
                Size = new Size(400, 30),
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };

            // Painel de conteúdo
            contentPanel = new Panel
            {
                Location = new Point(40, 390),
                Size = new Size(this.ClientSize.Width - 80, this.ClientSize.Height - 510),
                BackColor = Color.Transparent,
                AutoScroll = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };

            // Botões de opção
            CreateOptionButton(contentPanel, "🔗", "Usar URL do GitHub", 
                "Conectar usando repositório do GitHub", 0, BtnGitHub_Click);

            string defaultUrl = LoadDefaultUrl();
            string defaultStatus = string.IsNullOrWhiteSpace(defaultUrl) ? "Não configurado" : defaultUrl;
            CreateOptionButton(contentPanel, "⚙️", "Usar Configuração Padrão", 
                defaultStatus, 1, BtnDefault_Click, string.IsNullOrWhiteSpace(defaultUrl));

            CreateOptionButton(contentPanel, "🌐", "Digitar URL Personalizada", 
                "Configurar URL customizada manualmente", 2, BtnCustom_Click);

            CreateOptionButton(contentPanel, "⚙️", "Definir URL Padrão", 
                "Configurar URL padrão do sistema", 3, BtnSetDefault_Click);

            // Status
            Label lblStatusLabel = new Label
            {
                Text = "Status",
                Location = new Point(40, this.ClientSize.Height - 100),
                Size = new Size(100, 20),
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(120, 140, 180),
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };

            lblStatus = new Label
            {
                Text = "Aguardando seleção...",
                Location = new Point(40, this.ClientSize.Height - 75),
                Size = new Size(this.ClientSize.Width - 80, 30),
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            this.Controls.AddRange(new Control[] { 
                headerPanel, infoPanel, lblSectionTitle, contentPanel, lblStatusLabel, lblStatus 
            });

            UpdateDefaultButtonStatus();
        }

        private void InitializePerformanceMonitoring()
        {
            // Criar painel de estatísticas
            statsPanel = new Panel
            {
                Location = new Point(40, 240),
                Size = new Size(this.ClientSize.Width - 80, 80),
                BackColor = Color.FromArgb(20, 40, 70),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            statsPanel.Paint += (s, e) => DrawRoundedRectangle(e.Graphics, statsPanel.ClientRectangle, 10, Color.FromArgb(20, 40, 70));

            // CPU Label
            Label lblCpuLabel = new Label
            {
                Text = "CPU",
                Location = new Point(30, 15),
                Size = new Size(80, 20),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 200, 255),
                BackColor = Color.Transparent
            };

            lblCpuUsage = new Label
            {
                Text = "0%",
                Location = new Point(120, 15),
                Size = new Size(100, 20),
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.White,
                BackColor = Color.Transparent
            };

            pbCpu = new ProgressBar
            {
                Location = new Point(230, 15),
                Size = new Size(this.ClientSize.Width - 300, 20),
                Style = ProgressBarStyle.Continuous,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            // RAM Label
            Label lblRamLabel = new Label
            {
                Text = "RAM",
                Location = new Point(30, 45),
                Size = new Size(80, 20),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 200, 255),
                BackColor = Color.Transparent
            };

            lblRamUsage = new Label
            {
                Text = "0 MB / 0 MB",
                Location = new Point(120, 45),
                Size = new Size(400, 20),
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.White,
                BackColor = Color.Transparent
            };

            pbRam = new ProgressBar
            {
                Location = new Point(230, 45),
                Size = new Size(this.ClientSize.Width - 300, 20),
                Style = ProgressBarStyle.Continuous,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            statsPanel.Controls.AddRange(new Control[] { 
                lblCpuLabel, lblCpuUsage, pbCpu,
                lblRamLabel, lblRamUsage, pbRam
            });

            this.Controls.Add(statsPanel);

            // Inicializar monitor de performance
            performanceMonitor = new PerformanceMonitor();
            performanceMonitor.OnPerformanceUpdate += PerformanceMonitor_OnUpdate;
            performanceMonitor.StartMonitoring(1000); // Atualiza a cada 1 segundo
        }

        private void PerformanceMonitor_OnUpdate(object sender, PerformanceData data)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdatePerformanceUI(data)));
            }
            else
            {
                UpdatePerformanceUI(data);
            }
        }

        private void UpdatePerformanceUI(PerformanceData data)
        {
            // Atualizar CPU
            lblCpuUsage.Text = data.CpuUsageFormatted;
            pbCpu.Value = Math.Min((int)data.CpuUsagePercent, 100);

            if (data.CpuUsagePercent > 80)
                lblCpuUsage.ForeColor = Color.FromArgb(255, 100, 100);
            else if (data.CpuUsagePercent > 50)
                lblCpuUsage.ForeColor = Color.FromArgb(255, 200, 100);
            else
                lblCpuUsage.ForeColor = Color.White;

            // Atualizar RAM
            lblRamUsage.Text = data.RamUsageFormatted;
            pbRam.Value = Math.Min((int)data.RamUsagePercent, 100);

            if (data.RamUsagePercent > 80)
                lblRamUsage.ForeColor = Color.FromArgb(255, 100, 100);
            else if (data.RamUsagePercent > 50)
                lblRamUsage.ForeColor = Color.FromArgb(255, 200, 100);
            else
                lblRamUsage.ForeColor = Color.White;
        }

        private void MainForm_Resize(object? sender, EventArgs e)
        {
            if (contentPanel != null)
            {
                contentPanel.Size = new Size(this.ClientSize.Width - 80, this.ClientSize.Height - 510);
                ResizeOptionButtons();
            }

            if (infoPanel != null)
            {
                infoPanel.Size = new Size(this.ClientSize.Width - 80, 100);
            }

            if (statsPanel != null)
            {
                statsPanel.Size = new Size(this.ClientSize.Width - 80, 80);
                if (pbCpu != null)
                    pbCpu.Size = new Size(this.ClientSize.Width - 300, 20);
                if (pbRam != null)
                    pbRam.Size = new Size(this.ClientSize.Width - 300, 20);
            }

            if (lblStatus != null)
            {
                lblStatus.Location = new Point(40, this.ClientSize.Height - 75);
                lblStatus.Size = new Size(this.ClientSize.Width - 80, 30);
            }

            foreach (Control ctrl in this.Controls)
            {
                if (ctrl is Label lbl && lbl.Text == "Status")
                {
                    lbl.Location = new Point(40, this.ClientSize.Height - 100);
                }
            }
        }

        private void ResizeOptionButtons()
        {
            if (contentPanel == null) return;

            foreach (Control ctrl in contentPanel.Controls)
            {
                if (ctrl is Panel panel)
                {
                    panel.Size = new Size(contentPanel.ClientSize.Width - 20, 85);
                }
            }
        }

        private Panel CreateOptionButton(Panel parent, string icon, string title, string subtitle, int index, EventHandler clickEvent, bool disabled = false)
        {
            Panel btnPanel = new Panel
            {
                Location = new Point(0, index * 95),
                Size = new Size(parent.ClientSize.Width - 20, 85),
                BackColor = disabled ? Color.FromArgb(25, 35, 55) : Color.FromArgb(20, 40, 70),
                Cursor = disabled ? Cursors.Default : Cursors.Hand,
                Tag = new { Title = title, Subtitle = subtitle, Disabled = disabled }
            };

            if (!disabled)
            {
                btnPanel.Click += clickEvent;
                btnPanel.MouseEnter += (s, e) => btnPanel.BackColor = Color.FromArgb(30, 50, 85);
                btnPanel.MouseLeave += (s, e) => btnPanel.BackColor = Color.FromArgb(20, 40, 70);
            }

            btnPanel.Paint += (s, e) => DrawRoundedRectangle(e.Graphics, btnPanel.ClientRectangle, 10, btnPanel.BackColor);

            Label lblIcon = new Label
            {
                Text = icon,
                Location = new Point(25, 20),
                Size = new Size(45, 45),
                Font = new Font("Segoe UI", 24),
                BackColor = Color.Transparent,
                ForeColor = disabled ? Color.FromArgb(80, 90, 110) : Color.FromArgb(100, 200, 255)
            };

            Label lblTitle = new Label
            {
                Text = title,
                Location = new Point(85, 18),
                Size = new Size(900, 28),
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = disabled ? Color.FromArgb(100, 110, 130) : Color.White,
                BackColor = Color.Transparent
            };

            Label lblSubtitle = new Label
            {
                Text = subtitle,
                Location = new Point(85, 45),
                Size = new Size(900, 22),
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(120, 140, 180),
                BackColor = Color.Transparent
            };

            Label lblArrow = new Label
            {
                Text = "›",
                Location = new Point(1060, 25),
                Size = new Size(30, 35),
                Font = new Font("Segoe UI", 24),
                ForeColor = Color.FromArgb(120, 140, 180),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter
            };

            btnPanel.Controls.AddRange(new Control[] { lblIcon, lblTitle, lblSubtitle, lblArrow });

            if (!disabled)
            {
                foreach (Control ctrl in btnPanel.Controls)
                {
                    ctrl.Click += clickEvent;
                    ctrl.MouseEnter += (s, e) => btnPanel.BackColor = Color.FromArgb(30, 50, 85);
                    ctrl.MouseLeave += (s, e) => btnPanel.BackColor = Color.FromArgb(20, 40, 70);
                }
            }

            parent.Controls.Add(btnPanel);
            return btnPanel;
        }

        private void DrawRoundedRectangle(Graphics graphics, Rectangle bounds, int cornerRadius, Color fillColor)
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath path = GetRoundedRect(bounds, cornerRadius))
            using (SolidBrush brush = new SolidBrush(fillColor))
            {
                graphics.FillPath(brush, path);
            }
        }

        private GraphicsPath GetRoundedRect(Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            Size size = new Size(diameter, diameter);
            Rectangle arc = new Rectangle(bounds.Location, size);
            GraphicsPath path = new GraphicsPath();

            if (radius == 0)
            {
                path.AddRectangle(bounds);
                return path;
            }

            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();

            return path;
        }

        private void UpdateDefaultButtonStatus()
        {
            string defaultUrl = LoadDefaultUrl();
            
            foreach (Control ctrl in contentPanel.Controls)
            {
                if (ctrl is Panel panel && panel.Tag != null)
                {
                    dynamic tag = panel.Tag;
                    if (tag.Title == "Usar Configuração Padrão")
                    {
                        bool hasDefault = !string.IsNullOrWhiteSpace(defaultUrl);
                        panel.BackColor = hasDefault ? Color.FromArgb(20, 40, 70) : Color.FromArgb(25, 35, 55);
                        panel.Cursor = hasDefault ? Cursors.Hand : Cursors.Default;
                        
                        foreach (Control child in panel.Controls)
                        {
                            if (child is Label lbl)
                            {
                                if (lbl.Text.Contains("Usar Configuração"))
                                {
                                    lbl.ForeColor = hasDefault ? Color.White : Color.FromArgb(100, 110, 130);
                                }
                                else if (lbl.Text == "⚙️")
                                {
                                    lbl.ForeColor = hasDefault ? Color.FromArgb(100, 200, 255) : Color.FromArgb(80, 90, 110);
                                }
                                else if (lbl.Text != "›")
                                {
                                    lbl.Text = hasDefault ? defaultUrl : "Não configurado";
                                }
                            }
                        }
                    }
                }
            }
        }

        private async void BtnGitHub_Click(object? sender, EventArgs e)
        {
            string url = "https://raw.githubusercontent.com/Project-axii/Project-axii-gateway/refs/heads/main/sistema_desk.json";
            await ConnectToServer(url);
        }

        private async void BtnDefault_Click(object? sender, EventArgs e)
        {
            string url = LoadDefaultUrl();
            if (string.IsNullOrWhiteSpace(url))
            {
                MessageBox.Show("Nenhuma URL padrão configurada!", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            await ConnectToServer(url);
        }

        private async void BtnCustom_Click(object? sender, EventArgs e)
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

        private void BtnSetDefault_Click(object? sender, EventArgs e)
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
            UpdateStatus("Buscando configuração do servidor...");

            try
            {
                string serverIp = await GetServerUrlFromJson(jsonUrl);
                UpdateStatus($"Conectando ao servidor: {serverIp}");

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

                UpdateStatus("Conectado! Aguardando comandos...");
            }
            catch (Exception ex)
            {
                UpdateStatus($"Erro de conexão: {ex.Message}");
                MessageBox.Show($"Erro ao conectar:\n{ex.Message}", "Erro de Conexão", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SetupConnectionEvents()
        {
            connection.Reconnecting += error =>
            {
                this.Invoke((MethodInvoker)delegate
                {
                    UpdateStatus("Reconectando ao servidor...");
                });
                return Task.CompletedTask;
            };

            connection.Reconnected += connectionId =>
            {
                this.Invoke((MethodInvoker)delegate
                {
                    UpdateStatus("Reconectado com sucesso!");
                });
                return Task.CompletedTask;
            };

            connection.Closed += async error =>
            {
                this.Invoke((MethodInvoker)delegate
                {
                    UpdateStatus("Conexão perdida!");
                });
                
                await Task.CompletedTask;
            };

            connection.On<string>("ExecuteCommand", async (action) =>
            {
                this.Invoke((MethodInvoker)delegate
                {
                    UpdateStatus($"Executando comando: {action}");
                });

                string result = await Task.Run(() => ExecuteBatScript(action));

                this.Invoke((MethodInvoker)delegate
                {
                    UpdateStatus($"Comando executado: {result}");
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
                    description = "Bloco de Notas aberto";
                    break;

                case "datetime":
                    batContent = "@echo off\ntitle Axii Desktop - Data e Hora\ncolor 0A\necho.\necho \necho            AXII DESKTOP - INFORMACOES DO SISTEMA\necho \necho.\necho  Computador: %COMPUTERNAME%\necho  Usuario: %USERNAME%\necho.\necho  Data: %date%\necho  Hora: %time%\necho.\necho \necho.\npause";
                    description = "Data/Hora exibida";
                    break;

                case "file":
                    batContent = "@echo off\necho  > created_file.txt\necho           ARQUIVO CRIADO PELO AXII DESKTOP SYSTEM >> created_file.txt\necho  >> created_file.txt\necho. >> created_file.txt\necho Data de Criacao: %date% %time% >> created_file.txt\necho Computador: %COMPUTERNAME% >> created_file.txt\necho Usuario: %USERNAME% >> created_file.txt\necho Sistema: %OS% >> created_file.txt\necho. >> created_file.txt\necho Este arquivo foi criado automaticamente pelo sistema de >> created_file.txt\necho controle remoto Axii Desktop. >> created_file.txt\necho. >> created_file.txt\necho  >> created_file.txt\ntitle Axii Desktop - Arquivo Criado\ncolor 0A\necho.\necho Arquivo 'created_file.txt' criado com sucesso!\necho.\necho Conteudo do arquivo:\necho.\ntype created_file.txt\necho.\npause";
                    description = "Arquivo criado";
                    break;

                case "vscode":
                    batContent = "@echo off\nstart code";
                    description = "VS Code aberto";
                    break;

                case "visualstudio":
                    batContent = "@echo off\nstart devenv";
                    description = "Visual Studio aberto";
                    break;

                case "laragon":
                    batContent = "@echo off\nif exist \"C:\\laragon\\laragon.exe\" (\n    start \"\" \"C:\\laragon\\laragon.exe\"\n    echo Laragon iniciado!\n) else (\n    echo Laragon nao encontrado em C:\\laragon\\laragon.exe\n    pause\n)";
                    description = "Laragon aberto";
                    break;

                case "packettracer":
                    batContent = "@echo off\nif exist \"C:\\Program Files\\Cisco Packet Tracer 8.2\\bin\\PacketTracer.exe\" (\n    start \"\" \"C:\\Program Files\\Cisco Packet Tracer 8.2\\bin\\PacketTracer.exe\"\n) else if exist \"C:\\Program Files\\Cisco Packet Tracer\\bin\\PacketTracer.exe\" (\n    start \"\" \"C:\\Program Files\\Cisco Packet Tracer\\bin\\PacketTracer.exe\"\n) else if exist \"C:\\Program Files (x86)\\Cisco Packet Tracer\\bin\\PacketTracer.exe\" (\n    start \"\" \"C:\\Program Files (x86)\\Cisco Packet Tracer\\bin\\PacketTracer.exe\"\n) else (\n    echo Packet Tracer nao encontrado\n    pause\n)";
                    description = "Packet Tracer aberto";
                    break;

                case "ngrok":
                    batContent = "@echo off\ntitle Axii Desktop - Ngrok Tunnel\ncolor 0B\necho.\necho \necho              AXII DESKTOP - INICIANDO NGROK\necho \necho.\nngrok http 5000";
                    description = "Ngrok iniciado";
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
                return $"Erro: {ex.Message}";
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
                MessageBox.Show($"Erro ao salvar: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateStatus(string message)
        {
            lblStatus.Text = message;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            performanceMonitor?.StopMonitoring();
            performanceMonitor?.Dispose();
            base.OnFormClosing(e);
        }
    }
    
    public class PerformanceMonitor
    {
        private PerformanceCounter cpuCounter;
        private PerformanceCounter ramCounter;
        private System.Windows.Forms.Timer updateTimer;

        public event EventHandler<PerformanceData> OnPerformanceUpdate;

        public PerformanceMonitor()
        {
            cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            ramCounter = new PerformanceCounter("Memory", "Available MBytes");
            cpuCounter.NextValue();
        }

        public void StartMonitoring(int intervalMs = 1000)
        {
            updateTimer = new System.Windows.Forms.Timer();
            updateTimer.Interval = intervalMs;
            updateTimer.Tick += async (s, e) => await UpdatePerformanceAsync();
            updateTimer.Start();
        }

        public void StopMonitoring()
        {
            updateTimer?.Stop();
        }

        private async Task UpdatePerformanceAsync()
        {
            await Task.Run(() =>
            {
                var data = GetCurrentPerformance();
                OnPerformanceUpdate?.Invoke(this, data);
            });
        }

        public PerformanceData GetCurrentPerformance()
        {
            float cpuUsage = cpuCounter.NextValue();
            float availableRamMB = ramCounter.NextValue();
            float totalRamMB = GetTotalPhysicalMemoryMB();
            float usedRamMB = totalRamMB - availableRamMB;
            float ramUsagePercent = (usedRamMB / totalRamMB) * 100;

            return new PerformanceData
            {
                CpuUsagePercent = Math.Round(cpuUsage, 2),
                RamUsedMB = Math.Round(usedRamMB, 2),
                RamAvailableMB = Math.Round(availableRamMB, 2),
                RamTotalMB = Math.Round(totalRamMB, 2),
                RamUsagePercent = Math.Round(ramUsagePercent, 2)
            };
        }

        private float GetTotalPhysicalMemoryMB()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        long totalBytes = Convert.ToInt64(obj["TotalPhysicalMemory"]);
                        return totalBytes / (1024f * 1024f);
                    }
                }
            }
            catch
            {
                return 8192;
            }
            return 0;
        }

        public void Dispose()
        {
            updateTimer?.Dispose();
            cpuCounter?.Dispose();
            ramCounter?.Dispose();
        }
        public async Task SendToServer(HubConnection connection)
        {
            var data = GetCurrentPerformance();
            await connection.InvokeAsync("UpdatePerformanceStats", 
                data.CpuUsagePercent, 
                data.RamUsagePercent, 
                data.RamUsedMB, 
                data.RamTotalMB);
        }
    }

    public class PerformanceData
    {
        public double CpuUsagePercent { get; set; }
        public double RamUsedMB { get; set; }
        public double RamAvailableMB { get; set; }
        public double RamTotalMB { get; set; }
        public double RamUsagePercent { get; set; }

        public string CpuUsageFormatted => $"{CpuUsagePercent:F1}%";
        public string RamUsageFormatted => $"{RamUsedMB:F0} MB / {RamTotalMB:F0} MB ({RamUsagePercent:F1}%)";
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
            this.Size = new Size(600, 220);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(15, 30, 60);

            lblPrompt = new Label
            {
                Text = prompt,
                Location = new Point(30, 30),
                Size = new Size(520, 30),
                Font = new Font("Segoe UI", 11),
                ForeColor = Color.White,
                BackColor = Color.Transparent
            };

            txtInput = new TextBox
            {
                Location = new Point(30, 75),
                Size = new Size(520, 35),
                Font = new Font("Segoe UI", 11),
                BackColor = Color.FromArgb(25, 45, 75),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            btnOk = new Button
            {
                Text = "Confirmar",
                Location = new Point(340, 135),
                Size = new Size(100, 40),
                BackColor = Color.FromArgb(0, 120, 212),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                DialogResult = DialogResult.OK,
                Cursor = Cursors.Hand
            };
            btnOk.FlatAppearance.BorderSize = 0;

            btnCancel = new Button
            {
                Text = "Cancelar",
                Location = new Point(450, 135),
                Size = new Size(100, 40),
                BackColor = Color.FromArgb(60, 70, 90),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                DialogResult = DialogResult.Cancel,
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderSize = 0;

            this.Controls.AddRange(new Control[] { lblPrompt, txtInput, btnOk, btnCancel });
            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;
        }
    }
}