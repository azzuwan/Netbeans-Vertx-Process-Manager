using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;

namespace VertxProcessManager
{
    class Program
    {

        public static bool isRunning(int pid)
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

        static void Main(string[] args)
        {

            var vertxArgs = String.Join(" ", args);

            //Mimic Vertx batch file that comes with the installation
            var vertxHome = @"D:\Downloads\Vertx-3.2.0\vert.x-3.2.0-full";
            //var vertxClasspath = vertxHome + @"\conf;" + vertxHome + @"\lib";            
            var vertxClasspath = "";

            foreach( var file in Directory.GetFiles(vertxHome + "\\lib"))
            {
                vertxClasspath += ";" + Path.GetFullPath(file);
            }

            var vertxMods = Environment.GetEnvironmentVariable("VERTX_MODS");
            var vertxSycnAgent = Environment.GetEnvironmentVariable("VERTX_SYNC_AGENT");
            var vertxJulConfig = Environment.GetEnvironmentVariable("VERTX_JUL_CONFIG");            
            
            var vertxClusterMgrFactory = Environment.GetEnvironmentVariable("VERTX_CLUSTERMANAGERFACTORY");
            
            if(String.IsNullOrEmpty(vertxJulConfig))
            {
                vertxJulConfig = vertxHome + @"\conf\logging.properties";
            }

            if(String.IsNullOrEmpty(vertxClusterMgrFactory))
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
            
            var javaCmd = "\"" +jdkHome + "\\bin\\java.exe\"";
            var mainClass = "io.vertx.core.Launcher";
            var arguments =  jvmOpts + " "
                + vertxSycnAgent + " "
                + vertxOpts + " -Dvertx.cli.usage.prefix=vertx"
                + " -Djava.util.logging.config.file=" + vertxJulConfig
                + " -Dvertx.home=" + vertxHome
                + " -Dvertx.clusterManagerFactory=" + vertxClusterMgrFactory
                + " -cp \"" + classpath + " ;" + vertxClasspath + " \""
                + " " + mainClass
                +" " + vertxArgs;            
            
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

                    Console.WriteLine("PID: " + process.Id.ToString() );
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

             //Console.ReadKey();

        }
    }
}
