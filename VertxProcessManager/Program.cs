using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Threading;
using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Protocol;

namespace VertxProcessManager
{
    class Manager
    {

        AppServer server = new AppServer();

        public Manager(string[] args)
        {
            Args = args;

            server.NewSessionConnected += new SessionHandler<AppSession>(NewSessionHandler);
            server.NewRequestReceived += new RequestHandler<AppSession, StringRequestInfo>(RequestHandler);
        }

        private void RequestHandler(AppSession session, StringRequestInfo requestInfo)
        {
            switch (requestInfo.Key.ToUpper())
            {
                case ("START"):
                    session.Send("START COMMAND RECEIVED!");
                    Start();
                    break;

                case ("STOP"):
                    session.Send("STOP COMMAND RECEIVED!");
                    Stop();
                    break;

                case ("RESTART"):

                    session.Send("RESTART COMMAND RECEIVED!");
                    Restart();
                    break;
            }
        }

        private void NewSessionHandler(AppSession session)
        {
            session.Send("You are connected to Vertx Process Manager");
        }
        

        public void Run()
        {
            var vertxArgs = String.Join(" ", Args);

            //Mimic Vertx batch file that comes with the installation
            var vertxHome = @"D:\Downloads\Vertx-3.2.0\vert.x-3.2.0-full";
                     
            var vertxClasspath = "";

            foreach (var file in Directory.GetFiles(vertxHome + "\\lib"))
            {
                vertxClasspath += ";" + Path.GetFullPath(file);
            }

            var vertxMods = Environment.GetEnvironmentVariable("VERTX_MODS");
            var vertxSycnAgent = Environment.GetEnvironmentVariable("VERTX_SYNC_AGENT");
            var vertxJulConfig = Environment.GetEnvironmentVariable("VERTX_JUL_CONFIG");

            var vertxClusterMgrFactory = Environment.GetEnvironmentVariable("VERTX_CLUSTERMANAGERFACTORY");

            if (String.IsNullOrEmpty(vertxJulConfig))
            {
                vertxJulConfig = vertxHome + @"\conf\logging.properties";
            }

            if (String.IsNullOrEmpty(vertxClusterMgrFactory))
            {
                vertxClusterMgrFactory = "io.vertx.spi.cluster.impl.hazelcast.HazelcastClusterManagerFactory";
            }



            var vertxOpts = Environment.GetEnvironmentVariable("VERTX_OPTS");
            var path = Environment.GetEnvironmentVariable("PATH");
            var classpath = Environment.GetEnvironmentVariable("CLASSPATH");
            var jdkHome = Environment.GetEnvironmentVariable("JDK_HOME");
            var jvmOpts = Environment.GetEnvironmentVariable("JVM_OPTS");
            var jmxOpts = Environment.GetEnvironmentVariable("JMX_OPTS");
            var javaOpts = Environment.GetEnvironmentVariable("JAVA_OPTS");

            //Set environment variables
            Environment.SetEnvironmentVariable("PATH", path + ";" + vertxHome + "\\bin");
            Environment.SetEnvironmentVariable("CLASSPATH", classpath + ";" + vertxClasspath);

            var javaCmd = "\"" + jdkHome + "\\bin\\java.exe\"";
            var mainClass = "io.vertx.core.Launcher";
            var arguments = jvmOpts + " "
                + vertxSycnAgent + " "
                + vertxOpts + " -Dvertx.cli.usage.prefix=vertx"
                + " -Djava.util.logging.config.file=" + vertxJulConfig
                + " -Dvertx.home=" + vertxHome
                + " -Dvertx.clusterManagerFactory=" + vertxClusterMgrFactory
                + " -cp \"" + classpath + " ;" + vertxClasspath + " \""
                + " " + mainClass
                + " " + vertxArgs;

            //Console.WriteLine("ARGUMENTS: " + arguments);

            var process = new Process();
            process.StartInfo.FileName = javaCmd;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.OutputDataReceived += (sender, output) => Console.WriteLine(output.Data);
            process.ErrorDataReceived += (sender, output) => Console.WriteLine(output.Data);


            if (File.Exists("vertx.pid"))
            {
                Console.WriteLine("PID file exists!");
            }
            else
            {
                process.Start();


                if (process.Id > 0)
                {
                    Pid = process.Id;
                    
                    Console.WriteLine("PID: " + process.Id.ToString());
                    using (StreamWriter writer = new StreamWriter("vertx.pid"))
                    {
                        writer.WriteLine(process.Id.ToString());
                    }

                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    process.WaitForExit();

                }
                else
                {
                    Console.WriteLine("Process not started");
                }

            }
        }


        public string[] Args
        {
            get;
            set;
        }

        public int Pid
        {
            get;
            set;
        }

        public bool IsRunning
        {
            get
            {
                return GetStatus(Pid);
            }
            set{}
        }


        public bool GetStatus(int pid)
        {
            var process = Process.GetProcessById(pid);
            if (process.Id == pid)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public void Start()
        {
            Console.WriteLine("Starting Vertx instance...");
            Thread t = new Thread(new ThreadStart(Run));
            t.Start();
        }

        public void Restart()
        {
            Console.WriteLine("Restarting Vertx instance...");
            if(IsRunning)
            {
                var process = Process.GetProcessById(Pid);

                try
                {
                    process.Kill();
                    File.Delete("vertx.pid");
                    Start();
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            else
            {
                
                try
                {   
                    Console.WriteLine("No instance is available to be restarted, starting a new instance");
                    File.Delete("vertx.pid");
                    Start();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        public void Stop()
        {
            if (IsRunning)
            {
                var process = Process.GetProcessById(Pid);

                try
                {
                    Console.WriteLine("Stopping Vertx instance: " + Pid);
                    process.Kill();
                    File.Delete("vertx.pid");
                    var proc1 = Process.GetProcessById(+Pid);
                    if(proc1.HasExited)
                    {
                        Console.WriteLine("Instance stopped");
                    }
                    
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            else
            {
                Console.WriteLine("No Vertx instance running");
            }
        }

        public void TestTcpCmd()
        {
            server.Setup(2012);
            server.Start();
            


        }

        static void Main(string[] args)
        {
            Manager m = new Manager(args);
            m.TestTcpCmd();
            //m.Start();
            //Console.Write("Waiting... ");
            //for (int i = 1; i < 31; i++ )
            //{
            //    Thread.Sleep(1000);
            //    Console.Write(" " + i + "...");
            //}

            //m.Stop();

            Console.ReadKey();
                
            
        }
    }
}
