<div align="center">

<img src="https://lfcostldktmoevensqdj.supabase.co/storage/v1/object/public/axii/white-logo.svg" alt="AXII Logo" width="120" />

# AXII — Aplicativo Desktop

**Componente desktop do sistema AXII para automação de salas de aula, desenvolvido como Trabalho de Conclusão de Curso do Curso Técnico em Informática da ETEC de Mauá.**

[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-12-239120?logo=csharp&logoColor=white)](https://learn.microsoft.com/dotnet/csharp/)
[![Windows](https://img.shields.io/badge/Windows-suportado-0078D4?logo=windows&logoColor=white)](https://microsoft.com/windows)
[![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-9.0-512BD4)](https://learn.microsoft.com/aspnet/core/)
[![SignalR](https://img.shields.io/badge/SignalR-tempo_real-512BD4)](https://learn.microsoft.com/aspnet/core/signalr/)
[![Licença MIT](https://img.shields.io/badge/licença-MIT-blue)](LICENSE)

</div>

---

## Sobre o Projeto

O **AXII Desktop** é o componente que conecta o sistema AXII diretamente aos computadores das salas de aula. Ele é composto por **dois programas distintos**:

| Programa | Para quem? | O que faz? |
|---|---|---|
| **AxiiServer** | Professor / Administrador | Servidor de controle com painel web para enviar comandos a todas as máquinas da sala |
| **AxiiClient** | Alunos / Máquinas da sala | Cliente executor instalado em cada PC; recebe e executa os comandos do servidor |

A comunicação entre os dois é feita via **SignalR (WebSocket)**, garantindo entrega de comandos em tempo real e sincronização instantânea.

O sistema AXII completo é composto por:
- **Desktop** (este repositório) — controle direto dos computadores da sala
- **Web** — painel de controle via navegador
- **Mobile** — controle e monitoramento pelo celular

---

## Arquitetura

```
┌───────────────────────────────────────────────┐
│            AxiiServer (Professor)              │
│  ┌─────────────────────────────────────────┐  │
│  │  Interface Web (React + Tailwind CSS)   │  │
│  │  → Painel de controle (localhost:5000)  │  │
│  │  → Monitor de performance (/monitor)    │  │
│  └────────────────┬────────────────────────┘  │
│                   │  REST + SignalR Hub        │
└───────────────────┼───────────────────────────┘
                    │  WebSocket (SignalR)
        ┌───────────┼───────────┐
        ↓           ↓           ↓
┌──────────┐ ┌──────────┐ ┌──────────┐
│AxiiClient│ │AxiiClient│ │AxiiClient│
│   PC 1   │ │   PC 2   │ │   PC N   │
└──────────┘ └──────────┘ └──────────┘
  (Alunos)     (Alunos)     (Alunos)
```

---

## Funcionalidades

### AxiiServer — Servidor de Controle

**Painel Web** (`http://localhost:5000`):
- Interface React exibindo todos os computadores conectados em tempo real
- Envio de comandos simultâneos para **todos os PCs da sala** com um clique
- Log de execução em tempo real com registro de cada ação (sucesso, erro, aviso)
- Indicador ao vivo de quantos computadores estão conectados
- Acessibilidade integrada via **VLibras** (Língua Brasileira de Sinais)

**Monitor de Performance** (`http://localhost:5000/monitor`):
- Monitoramento em tempo real de **CPU** e **RAM** de cada computador conectado
- Barras de progresso por máquina, atualizadas a cada 2 segundos via SignalR
- Identificação de cada PC pelo nome da máquina (`COMPUTERNAME`)

**API REST:**

| Método | Rota | Descrição |
|---|---|---|
| `POST` | `/broadcast` | Envia um comando para todos os clientes conectados |
| `GET` | `/clients` | Lista todos os computadores conectados |
| `GET` | `/performance` | Retorna dados de CPU/RAM de cada cliente |
| `GET` | `/status` | Status do servidor (online/offline, total de clientes) |
| `GET` | `/` | Painel de controle (HTML) |
| `GET` | `/monitor` | Monitor de performance (HTML) |

---

### AxiiClient — Cliente Executor

Aplicação **Windows Forms** instalada em cada PC da sala. Ao receber um comando do servidor, executa o script `.bat` correspondente localmente.

**Comandos disponíveis:**

| Comando | Aplicativo | Ação executada |
|---|---|---|
| `notepad` | Bloco de Notas | Abre o `notepad.exe` |
| `datetime` | Data e Hora | Exibe data, hora, usuário e nome do PC em console |
| `file` | Criar Arquivo | Cria `created_file.txt` com informações do sistema |
| `vscode` | VS Code | Executa `code` (VS Code no PATH) |
| `visualstudio` | Visual Studio | Executa `devenv` (Visual Studio) |
| `laragon` | Laragon | Abre `C:\laragon\laragon.exe` |
| `packettracer` | Packet Tracer | Abre o Cisco Packet Tracer (busca em múltiplos caminhos) |
| `ngrok` | Ngrok | Inicia `ngrok http 5000` no terminal |

**Modos de conexão ao servidor:**

| Opção | Descrição |
|---|---|
| 🔗 **URL do GitHub** | Lê a URL do servidor em `sistema_desk.json` (gateway do projeto no GitHub) |
| ⚙️ **Configuração Padrão** | Usa uma URL salva anteriormente em `axii_default_config.txt` |
| 🌐 **URL Personalizada** | Permite digitar qualquer URL manualmente |
| ⚙️ **Definir URL Padrão** | Salva uma URL como padrão para uso futuro |

**Monitoramento local:**
- Exibe uso de **CPU** e **RAM** em tempo real na janela do cliente
- Envia os dados de performance ao servidor a cada 2 segundos
- Suporte a **reconexão automática** com tentativas em 0s, 2s, 5s e 10s

---

## Tecnologias

### AxiiServer

| Tecnologia | Versão | Uso |
|---|---|---|
| [ASP.NET Core](https://learn.microsoft.com/aspnet/core/) | .NET 9 | Servidor HTTP e roteamento |
| [SignalR](https://learn.microsoft.com/aspnet/core/signalr/) | 1.2.0 | Comunicação em tempo real via WebSocket |
| [React](https://react.dev/) | 18 (CDN) | Interface web do painel de controle |
| [Tailwind CSS](https://tailwindcss.com/) | CDN | Estilização da interface web |

### AxiiClient

| Tecnologia | Versão | Uso |
|---|---|---|
| [Windows Forms](https://learn.microsoft.com/dotnet/desktop/winforms/) | .NET 9 | Interface gráfica do cliente |
| [SignalR Client](https://www.nuget.org/packages/Microsoft.AspNetCore.SignalR.Client/) | 10.0.0 | Conexão com o servidor em tempo real |
| [System.Management](https://www.nuget.org/packages/System.Management/) | 10.0.1 | Leitura de dados de CPU e RAM via WMI |

---

## Estrutura do Projeto

```
Project-axii-desktop/
├── Project-axii-desktop.sln      # Solução com os dois projetos
│
├── AxiiServer/                    # Servidor de controle (ASP.NET Core)
│   ├── Program.cs                 # Hub SignalR + endpoints REST
│   ├── index.html                 # Painel web (React + Tailwind)
│   ├── monitor.html               # Monitor de performance em tempo real
│   ├── appsettings.json           # Configurações do servidor
│   ├── AxiiServer.csproj
│   └── AxiiServer.sln
│
├── AxiiClient/                    # Cliente executor (Windows Forms)
│   ├── Program.cs                 # Interface WinForms + cliente SignalR
│   ├── AxiiClient.csproj
│   └── AxiiClient.sln
│
└── inno scripts/                  # Scripts do instalador (Inno Setup)
    ├── script AxiiServer.iss      # Instalador do servidor (v1.0.3)
    └── script AxiiClient.iss      # Instalador do cliente
```

---

## Como rodar localmente

### Pré-requisitos

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9) instalado
- Windows (obrigatório para o AxiiClient — Windows Forms)
- Os PCs do servidor e dos clientes precisam estar na mesma rede

---

### Executar o AxiiServer (computador do professor)

```bash
# Clone o repositório
git clone https://github.com/Project-axii/Project-axii-desktop.git
cd Project-axii-desktop/AxiiServer

# Restaurar dependências e executar
dotnet run
```

O servidor estará disponível em:
- **Painel de controle:** `http://localhost:5000`
- **Monitor de performance:** `http://localhost:5000/monitor`
- **IP da rede local:** exibido no console ao iniciar (ex: `http://192.168.1.100:5000`)

---

### Executar o AxiiClient (computadores dos alunos)

```bash
cd Project-axii-desktop/AxiiClient

# Restaurar dependências e executar
dotnet run
```

Na janela que abrir, escolha o modo de conexão:
1. **Usar URL do GitHub** — recomendado se o gateway estiver configurado
2. **Digitar URL Personalizada** — informe o endereço IP do servidor (ex: `http://192.168.1.100:5000`)

---

### Build de produção

```bash
# Publicar o AxiiServer
cd AxiiServer
dotnet publish -c Release -r win-x64 --self-contained

# Publicar o AxiiClient
cd AxiiClient
dotnet publish -c Release -r win-x64 --self-contained
```

---

## Instaladores

O projeto inclui scripts para geração de instaladores `.exe` usando o **Inno Setup** (somente Windows):

| Instalador | Versão | Descrição |
|---|---|---|
| `instalador AxiiServer.exe` | 1.0.3 | Instala o servidor no computador do professor |
| `instalador AxiiClient.exe` | — | Instala o cliente nos computadores dos alunos |

Para gerar os instaladores, abra os arquivos `.iss` na pasta `inno scripts/` com o [Inno Setup Compiler](https://jrsoftware.org/isinfo.php) e compile.

> Os instaladores são gerados para **Windows 64-bit** (x64 e Windows 11 ARM).

---

## Descoberta automática do servidor

O AxiiClient descobre o endereço do servidor via um arquivo JSON hospedado no repositório gateway do projeto:

```
https://raw.githubusercontent.com/Project-axii/Project-axii-gateway/refs/heads/main/sistema_desk.json
```

Esse arquivo retorna o IP atual do servidor em tempo de execução, eliminando a necessidade de configuração manual em cada máquina.

---

## 📄 Licença

Este projeto está licenciado sob a [Licença MIT](LICENSE).
