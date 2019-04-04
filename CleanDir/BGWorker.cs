using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.ComponentModel;

namespace DFGenerator
{
    public partial class MainWindow
    {

        /// <summary>
        /// On completed do the appropriate task
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void m_oWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // The background process is complete. We need to inspect
            // our response to see if an error occurred, a cancel was
            // requested or if we completed successfully.  
            if (e.Cancelled)
            {
                Output.Text = "Task Cancelled.";
            }

            // Check to see if an error occurred in the background process.

            else if (e.Error != null)
            {
                Output.Text = "Error while performing background operation.";
            }
            else
            {
                // Everything completed normally.
                //Extract the log file name
                string errFileName = e.Result.ToString();
                Output.Text = "Task Completed. Check log file " + errFileName + " for errors.";
            }

            //Change the status of the buttons on the UI accordingly
            btnStart.IsEnabled = true;
            btnCancel.IsEnabled = false;
        }

        /// <summary>
        /// Notification is performed here to the progress bar
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void m_oWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {

            // This function fires on the UI thread so it's safe to edit
            // the UI control directly, no funny business with Control.Invoke :)
            // Update the progressBar with the integer supplied to us from the
            // ReportProgress() function.  
            progBar.Value = e.ProgressPercentage;
            Output.Text = "Processing......" + progBar.Value.ToString() + "%";
        }

        /// <summary>
        /// Time consuming operations go here </br>
        /// i.e. Database operations,Reporting
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void m_oWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            List<object> argumentList = e.Argument as List<object>;
            string logFileName = (string)argumentList[0];
            char replaceChar = (char)argumentList[1];

            // Get a list of invalid file characters.
            char[] invalidFileChars = Path.GetInvalidFileNameChars();

            // Get a list of invalid path characters.
            char[] invalidPathChars = Path.GetInvalidPathChars();

            // This list will store generated files to ensure that no duplicate files are generated
            List<string> filePathList = new List<string>();

            string fileName = "", filePath = "";

            int counter = 0;
            bool duplicateFile;  // True when there are multiple files with the same name (Path + FileName)
 
            // Geherate the name of the log file for errors. Delete it if it already exiss
            string errFileName = Path.Combine(Path.GetDirectoryName(logFileName), DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".log");
            // Push the file name to the RunWorkerCompletedEventArgs
            e.Result = errFileName;
            // Check if file already exists. If yes, delete it.     
            if (File.Exists(errFileName))
            {
                File.Delete(errFileName);
            }
            // Create a file     
            StreamWriter fs = new StreamWriter(errFileName);

            logFileName = logFileName.Trim();
            string line;

            // Get the number of lines in the logfile - this will be used to compute the progress bar
            var totLines = File.ReadLines(@logFileName).Count();

            // Read the file and process it one line at a time.  
            System.IO.StreamReader file = new System.IO.StreamReader(@logFileName);
            while ((line = file.ReadLine()) != null)
            {
                string orgLine = line;
                 m_oWorker.ReportProgress(counter++ * 100 / totLines);

                // Get the file path. Cater for a situation in which the file path contains invalid characters
                try
                {
                    line = CleanUp(line, invalidPathChars, replaceChar);
                    filePath = System.IO.Path.GetDirectoryName(line);
                }
                catch (Exception ex)
                {
                    fs.Write(string.Format("REM Line {0}: Unable to cal;culate directory from \"{1}\". THIS FILE WILL NOT BE PROCESSED\n\n",
                       counter.ToString(), orgLine));
                    continue;
                }

                // Comment any changes to the file path so that the user will know where files were redistributed.
                if (orgLine.Substring(0, filePath.Length) == filePath)
                {
                    // As the directory has not changed, the file name should be the remaining part. Clean it up.
                    fileName = orgLine.Substring(filePath.Length + 1);
                    fileName = CleanUp(fileName, invalidFileChars, replaceChar);
                }
                else
                {
                    fs.Write(string.Format("REM Line {0}: The source directory has changed from \"{1}\" to \"{2}\"\n",
                        counter.ToString(), orgLine, filePath));

                    // try to extract the file name from the source line (orgLine)
                    try
                    {
                        fileName = System.IO.Path.GetFileName(orgLine);
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            // try extracting from the line that has been cleaned to get the directory path
                            fileName = System.IO.Path.GetFileName(line);
                        }
                        catch (Exception ex2)
                        {
                            // clean up invalid file characters and try again
                            line = CleanUp(line, invalidFileChars, replaceChar);
                            try
                            {
                                fileName = System.IO.Path.GetFileName(line);
                            }
                            catch (Exception ex3)
                            {
                                // generate a new filename
                                fileName = DateTime.Now.Ticks.ToString();
                            }
                        }
                    }
                }

                // if the generated file has the same name as an existing make it unique
                string chkfilePath = Path.Combine(filePath, fileName);
                duplicateFile = filePathList.Contains(chkfilePath);
                if (duplicateFile)
                {
                    filePath = Path.Combine(filePath, DateTime.Now.Ticks.ToString() + Path.GetExtension(fileName));
                    fs.Write(string.Format("REM Line {0}: A file called \"{1}\" already exists. Generating a unique file name\n",
                        counter.ToString(), chkfilePath));
                }
                else
                {
                    filePath = chkfilePath;
                }

                // Only process the file if there was a file name change
                if (filePath != orgLine)
                {
                    fs.Write(string.Format("REN \"{0}\" \"{1}\"    REM Line {2}\n\n",
                         orgLine, filePath, counter.ToString()));
                }

                // Add the generated file to the list to avoid duplicates
                filePathList.Add(filePath);

                // Periodically check if a cancellation request is pending.
                // If the user clicks cancel the line
                // m_AsyncWorker.CancelAsync(); if ran above.  This
                // sets the CancellationPending to true.
                // You must check this flag in here and react to it.
                // We react to it by setting e.Cancel to true and leaving
                if (m_oWorker.CancellationPending)
                {
                    // Set the e.Cancel flag so that the WorkerCompleted event
                    // knows that the process was cancelled.
                    e.Cancel = true;
                    m_oWorker.ReportProgress(0);
                    return;
                }

            }

            // Close the file stream
            fs.Flush();
            fs.Close();

            //Report 100% completion on operation completed
            m_oWorker.ReportProgress(100);
        }

    }
}
