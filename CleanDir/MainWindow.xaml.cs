using System;
using System.IO;
using System.Collections.Generic;
using System.Windows;
using System.ComponentModel;
using System.Reflection;
using System.Deployment.Application;

namespace DFGenerator
{
    // ACB - 201903
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private BackgroundWorker m_oWorker;

        // Invalid file characters will be replaced by this character
        private const char replaceChar = '_';

        public MainWindow()
        {
            InitializeComponent();


            // Populate the product version number
            txtVersion.Text = GetDFGenVer();

            txbNotes.Text = "The directory log file should be created using the following command [dir & lt; &lt; source path&gt; &gt; / a:-D / s / B > [filename.log]]." +
               Environment.NewLine +
               "Invalid characters will be replaced by the character [ " + replaceChar + " ]. If a situation exists in which duplicate file names will result within a particular directory, the utility automatically generates unqiue files names." +
               Environment.NewLine +
               "A log file is generated within the same folder as the directory log file." +
               Environment.NewLine +
               "This log file contains batch file commands to rename the files as well as remarks linking to the source log file. The file will need to be renamed to make it executable.";

                m_oWorker = new BackgroundWorker();

            // Create a background worker thread that ReportsProgress &
            // SupportsCancellation
            // Hook up the appropriate events.
            m_oWorker.DoWork += new DoWorkEventHandler(m_oWorker_DoWork);
            m_oWorker.ProgressChanged += new ProgressChangedEventHandler
                    (m_oWorker_ProgressChanged);
            m_oWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler
                    (m_oWorker_RunWorkerCompleted);
            m_oWorker.WorkerReportsProgress = true;
            m_oWorker.WorkerSupportsCancellation = true;
        }
 
        private void BtnOpen_Click(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog 
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            // Set filter for file extension and default file extension 
            dlg.DefaultExt = ".log";
            dlg.Filter = "LOG Files (*.log)|*.log|All Files (*.*)|*.*";


            // Display OpenFileDialog by calling ShowDialog method 
            Nullable<bool> result = dlg.ShowDialog();


            // Get the selected file name and display in a TextBox 
            if (result == true)
            {
                // Open document 
                string filename = dlg.FileName;
                txtLogFile.Text = filename;
            }
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtLogFile.Text))
            {
                System.Windows.MessageBox.Show("Specify the log file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            //Change the status of the buttons on the UI accordingly
            //The start button is disabled as soon as the background operation is started
            //The Cancel button is enabled so that the user can stop the operation 
            //at any point of time during the execution
            btnStart.IsEnabled = false;
            btnOpen.IsEnabled = false;
            btnCancel.IsEnabled = true;

            List<object> arguments = new List<object>();
            arguments.Add(txtLogFile.Text.Trim());
            arguments.Add(replaceChar);

            if (m_oWorker.IsBusy != true)
            {
                m_oWorker.RunWorkerAsync(arguments);
            }
            
        }
        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (m_oWorker.IsBusy)
            {

                // Notify the worker thread that a cancel has been requested.
                // The cancel will not actually happen until the thread in the
                // DoWork checks the m_oWorker.CancellationPending flag. 

                m_oWorker.CancelAsync();
            }
        }

        private string GetDFGenVer()
        {
                return ApplicationDeployment.IsNetworkDeployed
                       ? ApplicationDeployment.CurrentDeployment.CurrentVersion.ToString()
                       : Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }

        private string CleanUp(String inStr, char[] invalidChars, char replaceChar)
        {
            int match = inStr.IndexOfAny(invalidChars);
            while (match != -1)
            {
               inStr = inStr.Replace(inStr[match], replaceChar);
                match = inStr.IndexOfAny(invalidChars, match);
            }

            return inStr;

        }
    }
}
