using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
namespace BatGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Escolha uma opção para gerar e executar um arquivo .bat:");
            Console.WriteLine("1. Abrir o Bloco de Notas");
            Console.WriteLine("2. Exibir data e hora atuais");
            Console.WriteLine("3. Criar um arquivo de texto");
            Console.WriteLine("4. Sair");
            string choice = Console.ReadLine();
            string batFileName = "temp_script.bat";
            string batContent = "";
            switch (choice)
            {
                case "1":
                    batContent = "@echo off\nstart notepad.exe";
                    break;
                case "2":
                    batContent = "@echo off\necho Data e Hora Atuais: %date% %time%\npause";
                    break;
                case "3":
                    batContent = "@echo off\necho Este é um arquivo de texto criado pelo .bat > created_file.txt\necho Arquivo created_file.txt criado com sucesso!\npause";
                    break;
                case "4":
                    Console.WriteLine("Saindo...");
                    return;
                default:
                    Console.WriteLine("Opção inválida.");
                    return;
            }
            try
            {
                File.WriteAllText(batFileName, batContent.Replace("\n", Environment.NewLine));
                Console.WriteLine($"Arquivo \'{batFileName}\' criado com sucesso.");
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = batFileName;
                psi.UseShellExecute = true;
                Console.WriteLine($"Executando \'{batFileName}\'...");
                using (Process process = Process.Start(psi))
                {
                    process.WaitForExit(); 
                }
                Console.WriteLine($"Execução de \'{batFileName}\' concluída.");
                File.Delete(batFileName);
                Console.WriteLine($"Arquivo \'{batFileName}\' deletado com sucesso.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ocorreu um erro: {ex.Message}");
            }
            Console.WriteLine("Pressione qualquer tecla para finalizar.");
            Console.ReadKey();
        }
    }
}