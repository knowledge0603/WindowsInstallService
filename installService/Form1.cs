using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Windows.Forms;

namespace WindowsFormsApp2
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string strDirectory = System.AppDomain.CurrentDomain.BaseDirectory;
              Process proc = new Process();
              proc.StartInfo.CreateNoWindow = true;
              proc.StartInfo.FileName = "cmd.exe";
              proc.StartInfo.UseShellExecute = false;
              proc.StartInfo.RedirectStandardError = true;
              proc.StartInfo.RedirectStandardInput = true;
              proc.StartInfo.RedirectStandardOutput = true;
              proc.Start();
              proc.StandardInput.WriteLine("cd " + strDirectory + "baseDir\\frp");
              proc.StandardInput.WriteLine("frp.bat ");
              proc.StandardInput.WriteLine("exit");
              while (!proc.StandardOutput.EndOfStream)
              {
                  string line = proc.StandardOutput.ReadLine();
                  WriteLog(line,true);
              }
            //return true;

            startFrp();

        }
        #region start frp service
        private void startFrp(Object sender = null, EventArgs e = null)
        {
            Process frpProcess = null;
            frpProcess = new Process();
            frpProcess.StartInfo.FileName = System.AppDomain.CurrentDomain.BaseDirectory + "baseDir\\frp\\frpc.exe";
            frpProcess.StartInfo.Arguments = " -c "+ System.AppDomain.CurrentDomain.BaseDirectory + "baseDir\\frp\\frpc.ini";
            frpProcess.StartInfo.CreateNoWindow = true;
            frpProcess.StartInfo.UseShellExecute = false;
            // Guardian to restart
            frpProcess.EnableRaisingEvents = true;
            frpProcess.Exited += new EventHandler(startFrp);
            // The process output
            frpProcess.StartInfo.RedirectStandardOutput = true;
            frpProcess.StartInfo.RedirectStandardError = true;
            frpProcess.OutputDataReceived += new DataReceivedEventHandler(MyProcOutputHandlerData);
            frpProcess.ErrorDataReceived += new DataReceivedEventHandler(MyProcOutputHandlerError);
            frpProcess.Start();
            frpProcess.BeginOutputReadLine();
            frpProcess.BeginErrorReadLine();
        }
        #endregion

        #region process out put
        private void MyProcOutputHandlerData(object sendingProcess, DataReceivedEventArgs outLine)
        {
            if (!String.IsNullOrEmpty(outLine.Data))
            {
                WriteLog(outLine.Data, true);
                if (outLine.Data.ToString().Contains("start proxy success"))
                {
                    WriteFrpServiceStatus("[frpServiceStatus]\n\r runing=true");
                }
                if (outLine.Data.ToString().Contains("error: i/o deadline reached"))
                {
                    WriteFrpServiceStatus("[frpServiceStatus]\n\r runing=false");
                }
            }
        }
        private void MyProcOutputHandlerError(object sendingProcess, DataReceivedEventArgs outLine)
        {
            if (!String.IsNullOrEmpty(outLine.Data))
            {
                WriteLogError(outLine.Data, true);
                if (outLine.Data.ToString().Contains("start proxy success"))
                {
                    WriteFrpServiceStatus("[frpServiceStatus]\n\r runing=true");
                }
                if (outLine.Data.ToString().Contains("error: i/o deadline reached"))
                {
                    WriteFrpServiceStatus("[frpServiceStatus]\n\r runing=false");
                }
            }
        }
        #endregion

        #region write log
        private void WriteLog(string logStr, bool wTime = true)
        {
            using (System.IO.StreamWriter sw = new System.IO.StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "\\FrpWindowsServiceAutoService.log", true))
            {
                string timeStr = wTime == true ? DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss ") : "";
                sw.WriteLine(timeStr + logStr);
            }
        }
        private void WriteLogError(string logStr, bool wTime = true)
        {
            using (System.IO.StreamWriter sw = new System.IO.StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "\\FrpWindowsServiceAutoServiceError.log", true))
            {
                string timeStr = wTime == true ? DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss ") : "";
                sw.WriteLine(timeStr + logStr);
            }
        }
        #endregion

        #region write Frp ServiceStatus
        private void WriteFrpServiceStatus(string statusStr)
        {
            using (System.IO.StreamWriter sw = new System.IO.StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "\\FrpWindowsServiceStatus.ini", true))
            {
                sw.Write(statusStr);
            }
        }
        #endregion

        public void WriteToFile(string Message)
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + "\\Logs";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            string filepath = AppDomain.CurrentDomain.BaseDirectory + "\\Logs\\ServiceLog_form" + DateTime.Now.Date.ToShortDateString().Replace('/', '_') + ".txt";
            if (!File.Exists(filepath))
            {
                // Create a file to write to. 
                using (StreamWriter sw = File.CreateText(filepath))
                {
                    sw.WriteLine(Message);
                }
            }
            else
            {
                using (StreamWriter sw = File.AppendText(filepath))
                {
                    sw.WriteLine(Message);
                }
            }
        }
        string serviceName = "FrpWindowsService";
        string serviceFilePath = "G:\\work\\git\\WinformInstallService\\setup\\setup\\bin\\Debug\\baseDir\\frp" + "\\FrpWindowsService.exe";
        private void button2_Click(object sender, EventArgs e)
        {
            if (this.IsServiceExisted(serviceName))
            {
                this.UninstallService(serviceName);
            }
            this.InstallService(serviceFilePath) ;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (this.IsServiceExisted(serviceName)) this.ServiceStart(serviceName);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (this.IsServiceExisted(serviceName)) this.ServiceStop(serviceName);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (this.IsServiceExisted(serviceName))
            {
                this.ServiceStop(serviceName);
                this.UninstallService(serviceFilePath);
            }
        }
        private bool IsServiceExisted(string serviceName)
        {
            ServiceController[] services = ServiceController.GetServices();
            foreach (ServiceController sc in services)
            {
                if (sc.ServiceName.ToLower() == serviceName.ToLower())
                {
                    return true;
                }
            }
            return false;
        }
        //安装服务
        private void InstallService(string serviceFilePath)
        {
            using (AssemblyInstaller installer = new AssemblyInstaller())
            {
                installer.UseNewContext = true;
                installer.Path = serviceFilePath;
                IDictionary savedState = new Hashtable();
                installer.Install(savedState);
                installer.Commit(savedState);
            }
        }
        //卸载服务
        private void UninstallService(string serviceFilePath)
        {
            using (AssemblyInstaller installer = new AssemblyInstaller())
            {
                installer.UseNewContext = true;
                installer.Path = serviceFilePath;
                installer.Uninstall(null);
            }
        }
        //启动服务
        private void ServiceStart(string serviceName)
        {
            using (ServiceController control = new ServiceController(serviceName))
            {
                if (control.Status == ServiceControllerStatus.Stopped)
                {
                    control.Start();
                }
            }
        }

        //停止服务
        private void ServiceStop(string serviceName)
        {
            using (ServiceController control = new ServiceController(serviceName))
            {
                if (control.Status == ServiceControllerStatus.Running)
                {
                    control.Stop();
                }
            }
        }
    }
}
