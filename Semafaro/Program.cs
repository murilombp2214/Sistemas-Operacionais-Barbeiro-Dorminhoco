using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Threading;
using System.Threading.Tasks;
using System.IO.MemoryMappedFiles;
using System.IO;
using System.Xml.Serialization;
using System.Runtime.ConstrainedExecution;
using System.Linq;

namespace Semafaro
{

    public struct Barbearia
    {
        //Id dos processos clientes
        public int Cli1;

        //caso fure 
        public int CliErro;

        //id dos processos dos barbeiros
        public int IdBarbeiro1;

        public bool Barbeiro1Disponivel;
    }
    class Program
    {
        enum Dorminhoco
        {
            Barbeiro,
            Cliente
        }

        private static MySemafaro Semafaro;
        private static MySharedMemory MemoriaCompartilhada;
        private static int qtdMaxSemafaro = 3;
        private static Queue<Process> ProcessosClientes = new Queue<Process>();
        private static List<Process> ProcessosBarbeiros = new List<Process>();

        private static string fileName = @"E:\Faculdade\SO 2\Semafaro\Semafaro\bin\Debug\netcoreapp3.1\Semafaro.exe";
        static void Escrever(string value)
        {
            string name = $"Eita/" + Guid.NewGuid().ToString() + ".txt";
            if (!File.Exists(name))
            {
                File.Create(name).Close();
            }

            using (StreamWriter writer = new StreamWriter(name))
            {
                writer.WriteLine(DateTime.Now.ToString() + " - " + value);
            }
        }


        static void Main(string[] args)
        {
            //inicializa memoria compartilhada
            MemoriaCompartilhada = new MySharedMemory();

            if (args.Length > 0) //processo filho
            {
                if (args[0] == Dorminhoco.Barbeiro.ToString())
                {
                    ExecuteBarbeiro();
                }
                else
                {
                    if (args[0] == Dorminhoco.Cliente.ToString())
                    {
                        ExecuteCliente();

                    }
                    else
                    {
                        throw new Exception("Opção invalida");
                    }
                }
            }
            else //processo pai
            {
                //iniciando semafaro
                Semafaro = new MySemafaro(qtdMaxSemafaro);

                //Cria pipe de memoria
                MemoriaCompartilhada.CreatePipe();


                //seta a memoria compartihada com os dados default
                SetarDadosDefaultNaMemoria();

                //Cria os barbeiros
                Process b1 = CrieBarbeiro();

                //Escreve o id do processo do barbeiro na memoria compartilhada
                MemoriaCompartilhada.OpenPipe();
                Barbearia loja;
                var acessor = MemoriaCompartilhada.Acessor;
                acessor.Read(0, out loja);
                loja.IdBarbeiro1 = b1.Id;
                acessor.Write(0, ref loja);

                //observador
                Task.Run(() =>
                {
                    while (true)
                    {
                        //Abre a memoria compartilhada'
                        MemoriaCompartilhada.OpenPipe();

                        Barbearia loja;

                        //Lê da memoria comapartilhada
                        var acessor = MemoriaCompartilhada.Acessor;
                        acessor.Read(0, out loja);

                        if (loja.Barbeiro1Disponivel) //se o barbeiro esta disponivel
                        {
                            if (ProcessosClientes.Count > 0)
                            {
                                ProcessosClientes.Dequeue();
                                if (loja.Barbeiro1Disponivel)
                                {
                                    acessor.Read(0, out loja);
                                    loja.Barbeiro1Disponivel = false;
                                    acessor.Write(0, ref loja);
                                    Semafaro.Down();
                                    acessor.Write(0, ref loja);
                                }
                            }
                        }
                    }
                });

                MenuPrincipal();

            }
        }

        /// <summary>
        /// Coloca os dados padrões
        /// </summary>
        private static void SetarDadosDefaultNaMemoria()
        {
            Barbearia loja;
            MemoriaCompartilhada.OpenPipe();
            var acessor = MemoriaCompartilhada.Acessor;
            acessor.Read(0, out loja);
            loja.Barbeiro1Disponivel = true;
            loja.Cli1 = 0;
            loja.CliErro = 0;
            acessor.Write(0, ref loja);
        }

        /// <summary>
        /// Executa cliente
        /// </summary>
        private static void ExecuteCliente()
        {
            //Abre a memoria compartilhada
            MemoriaCompartilhada.OpenPipe();
            Barbearia cliente;
            //Lê da memoria comapartilhada
            var acessor = MemoriaCompartilhada.Acessor;
            acessor.Read(0, out cliente);

            if (cliente.Cli1 == 0)
            {
                cliente.Cli1 = Process.GetCurrentProcess().Id; //barbeiro um vai pegar
            }
            else
            {
                cliente.CliErro = Process.GetCurrentProcess().Id; //O cliente foi embora pois não pode ser atendido
            }

            //Devolve a memoria compartilhada
            acessor.Write(0, ref cliente);
            Console.ReadKey();
        }

        /// <summary>
        /// Executa barbeiro
        /// </summary>
        private static void ExecuteBarbeiro()
        {
            //Observador
            Task.Run(() =>
            {
                while (true)
                {
                    try
                    {
                        //Abre a memoria compartilhada
                        MemoriaCompartilhada.OpenPipe();
                        Barbearia barbeiro;
                        //Lê da memoria comapartilhada
                        var acessor = MemoriaCompartilhada.Acessor;
                        acessor.Read(0, out barbeiro);
                        if (barbeiro.Cli1 != 0)
                        {
                            Console.WriteLine("\n\n\nExecutando cliente {1} por barbeiro {0}", Process.GetCurrentProcess().Id, barbeiro.Cli1);
                            Console.WriteLine("Informe mais um cliente");

                            Thread.Sleep(1000 * 10); //corta o cabelo em 10 segundos

                            Console.WriteLine("\n\n\nFim da execução do cliente {1} por barbeiro {0}", Process.GetCurrentProcess().Id, barbeiro.Cli1);

                            barbeiro.Cli1 = 0; //termina o corte e o cliente vai embora

                            barbeiro.Barbeiro1Disponivel = true; //avisa que esta disponivel



                            //Devolve a memoria compartilhada
                            acessor.Write(0, ref barbeiro);
                            acessor.Flush();
                        }
                    }
                    catch
                    {

                    }

                }

            });
            Console.ReadKey();
        }

        private static void MostrarDados()
        {
            //mostra os dados atuais
            Console.WriteLine("********************************");
            Console.WriteLine("Quantidade de clientes na fila: {0}", ProcessosClientes.Count);
            Console.WriteLine("Quantidade de barbeiros: {0}", 1);

            if (!ProcessosClientes.Any())
            {
                Console.WriteLine("Barbeiro dormindo...");
            }

            Console.WriteLine("Tamanho do semafaro: {0}", qtdMaxSemafaro);
            Console.WriteLine("********************************\n\n");

        }

        private static void MenuPrincipal()
        {
            while (true)
            {
                Console.Write(">>");
                string command = Console.ReadLine();

                if (command == "ver-dados")
                {
                    Task.Run(() => MostrarDados());
                }
                else
                {
                    if (command == "limpar")
                    {
                        Task.Run(() => Console.Clear());
                    }
                    else
                    {
                        CrieCliente(command);
                    }
                }

            }
        }

        /// <summary>
        /// Cria o processo do cliente e coloca no semafaro
        /// </summary>
        /// <param name="nomeCliente"></param>
        private static void CrieCliente(string nomeCliente)
        {
            //caso o cliente chegue e não há lugar para sentar ele vai embora
            if (ProcessosClientes.Count >= qtdMaxSemafaro)
            {
                Console.WriteLine("o Cliente {0} foi embora pois não avia lugar de esperar", nomeCliente);
                return;
            }

            if (string.IsNullOrEmpty(nomeCliente))
            {
                Console.WriteLine("Nome do cliente não pode ser vazio");
                return;
            }

            Console.WriteLine("Criando processo do cliente '{0}'", nomeCliente);

            //cria um processo 
            var processo = new Process();
            processo.StartInfo.FileName = fileName;

            //passa um argumento para a criação do processo informado que o mesmo deve se trabalhar como um cliente
            processo.StartInfo.ArgumentList.Add(Dorminhoco.Cliente.ToString());

            //exibe mensagem de informação que o processo foi criado
            Console.WriteLine("Cliente {0} criado ", nomeCliente);
            ProcessosClientes.Enqueue(processo);
            Semafaro.Up(() =>
            {
                processo.Start();
                processo.WaitForExit();
                processo.Close();
            });
        }

        /// <summary>
        /// Cria o processo do barbeiro
        /// </summary>
        /// <returns></returns>
        private static Process CrieBarbeiro()
        {
            //cria um processo
            var processo = new Process();
            processo.StartInfo.FileName = fileName;
            processo.EnableRaisingEvents = true;

            //passa um argumento para a criação do processo informado que o mesmo deve se trabalhar como um barbeiro
            processo.StartInfo.ArgumentList.Add(Dorminhoco.Barbeiro.ToString());

            //starta o processo
            processo.Start();

            //exibe mensagem de informação que o processo foi criado
            Console.WriteLine("Barbeiro {0} criado", processo.Id);
            ProcessosBarbeiros.Add(processo);
            return processo;
        }

    }

}






