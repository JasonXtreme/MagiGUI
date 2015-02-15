using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MagiGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public class Stratum
        {
            public string Name {get; set;}
            public string Value { get; set; }
            public Stratum(string name, string value)
            {
                this.Name = name;
                this.Value = value;

            }
        }

        public class Miner
        {
            public string Name { get; set; }
            public string Value { get; set; }
            public Miner(string name, string value)
            {
                this.Name = name;
                this.Value = value;

            }
        }


        private string isRunning = "Start";
        public string IsRunning { get { return isRunning; } set { isRunning = value; NotifyPropertyChanged("IsRunning"); } }

        private string username = "Worker";
        public string UserName { get { return username; } set { username = value; NotifyPropertyChanged("UserName"); } }

        private string password = "Password";
        public string Password { get { return password; } set { password = value; NotifyPropertyChanged("Password"); } }


        private int selectedNumCores = getNumOfProcessors();
        public int SelectedNumCores { get { return selectedNumCores; } set { selectedNumCores = value; NotifyPropertyChanged("SelectedNumCores"); } }

        private Stratum selectedStratum = new Stratum("temp","temp");
        public Stratum SelectedStratum { get { return selectedStratum; } set { selectedStratum = value; NotifyPropertyChanged("SelectedStratum"); } }
        
        private Miner selectedMiner = new Miner("temp","temp");
        public Miner SelectedMiner { get { return selectedMiner; } set { selectedMiner = value; NotifyPropertyChanged("SelectedMiner"); } }
        public ObservableCollection<int> CoresToSelect {get;set;}

        private System.Diagnostics.Process minerProcess;
        public List<Stratum> StratumList {get; set;}
        public List<Miner> MinerList { get; set; }


        public MainWindow()
        {
            this.Closed+=MainWindow_Closed;
            CoresToSelect = new ObservableCollection<int>();
            for (int i = 1; i <= getNumOfProcessors(); i++)
            {
                CoresToSelect.Add(i);
            }

            StratumList = new List<Stratum>();
            string MainDir = AppDomain.CurrentDomain.BaseDirectory;
            if (!System.IO.File.Exists(MainDir + @"PoolList.txt"))
            {
                MessageBox.Show("No valid pool list found, please add a pool list file (Pools.txt)");
                Application.Current.Shutdown();
            }
            else
            {
                GetMiners();

                using (StreamReader sr = new StreamReader(MainDir + @"PoolList.txt"))
                {
                    
                    String line = sr.ReadLine();
                    while (!(string.IsNullOrWhiteSpace(line)))
                    {
                        Stratum s = new Stratum(line.Split(' ')[0], line.Split(' ')[1].Trim());
                        StratumList.Add(s);
                        line = sr.ReadLine();
                    }
                    sr.Close();
                }


                UserName = Properties.Settings.Default.WorkerName;
                Password = Properties.Settings.Default.WorkerPassword;
                int MaxCores = getNumOfProcessors();
                if (Properties.Settings.Default.NumberOfCores <= MaxCores)
                    SelectedNumCores = Properties.Settings.Default.NumberOfCores;
                else
                    SelectedNumCores = 1;
                if (SelectedNumCores == -1)
                    SelectedNumCores = MaxCores;

                string stratumName = Properties.Settings.Default.Stratum;
                if (StratumList.Exists(x => x.Name == stratumName)) {
                    SelectedStratum = StratumList.Find(x => x.Name == stratumName);
                }
                else if (StratumList.Count > 0) {
                    SelectedStratum = StratumList.First();
                }

                string minerName = Properties.Settings.Default.Miner;
                if (MinerList.Exists(x => x.Name == minerName))
                {
                    SelectedMiner = MinerList.Find(x => x.Name == minerName);
                }
                else if (MinerList.Count > 0)
                {
                    SelectedMiner = MinerList.First();
                }



                this.DataContext = this;
                InitializeComponent();
                                
            }
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            SaveSettings();
        }

        private void SaveSettings()
        {
            Properties.Settings.Default.WorkerName = UserName;
            Properties.Settings.Default.WorkerPassword = Password;
            Properties.Settings.Default.Stratum = SelectedStratum.Name;
            Properties.Settings.Default.Miner = SelectedMiner.Name;
            Properties.Settings.Default.NumberOfCores = SelectedNumCores;
            Properties.Settings.Default.Save();

        }



        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string prop)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
            }
        }

        public static int getNumOfProcessors()
        {
            int coreCount = 0;
            foreach (var item in new System.Management.ManagementObjectSearcher("Select * from Win32_ComputerSystem").Get())
            {
                coreCount += int.Parse(item["NumberOfLogicalProcessors"].ToString());
            }
            return coreCount;
        }


        private void btnGAS_click(object sender, RoutedEventArgs e)
        {
            if (IsRunning == "Start")
            {
                if (minerProcess != null)
                    minerProcess.Kill();
                minerProcess = new System.Diagnostics.Process();

                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = SelectedMiner.Value;
                startInfo.Arguments = "-a m7mhash -o "+selectedStratum.Value+" -u "+UserName+" -p " +Password + " -t "+SelectedNumCores.ToString();
                //startInfo.RedirectStandardOutput = true;
                //startInfo.UseShellExecute = false;
                
                //minerProcess.OutputDataReceived += minerProcess_OutputDataReceived;
                minerProcess.EnableRaisingEvents = true;
               
                minerProcess.StartInfo = startInfo;
                minerProcess.Exited += minerProcess_Exited;
                minerProcess.Start();
                //minerProcess.BeginOutputReadLine();
                IsRunning = "Stop";
            }
            else {
                minerProcess.Kill();
                minerProcess = null;
                IsRunning = "Start";
            }
        }

        void minerProcess_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            MessageBox.Show(e.Data);
        }

        void minerProcess_Exited(object sender, EventArgs e)
        {
            IsRunning="Start";
            
            minerProcess = null;
        }

       
        private void GetMiners() {
            MinerList = new List<Miner>();

            string MainDir = AppDomain.CurrentDomain.BaseDirectory+@"Miners";
            string[] authorList = Directory.GetDirectories(MainDir, "*", SearchOption.TopDirectoryOnly);
            foreach (string str in authorList){
                
                string nameStart = System.IO.Path.GetFileName(str);
                string name = nameStart + " ";
                string[] versionList = Directory.GetDirectories(str, "*", SearchOption.TopDirectoryOnly);
                foreach (string MinerV in versionList) {
                    string minerName = name + System.IO.Path.GetFileName(MinerV);
                    Miner tmp = new Miner(minerName, MinerV + @"\minerd.exe");
                    MinerList.Add(tmp);
                }
            }
        }
    }
}
