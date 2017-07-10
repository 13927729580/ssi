﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ssi
{
    public partial class MainHandler
    {
        private void databaseCMLExtractFeatures_Click(object sender, RoutedEventArgs e)
        {
            DatabaseCMLExtractFeaturesWindow dialog = new DatabaseCMLExtractFeaturesWindow(this);
            dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            dialog.ShowDialog();

        }

        private void databaseCMLTrain_Click(object sender, RoutedEventArgs e)
        {
            DatabaseCMLTrainAndPredictWindow dialog = new DatabaseCMLTrainAndPredictWindow(this, DatabaseCMLTrainAndPredictWindow.Mode.TRAIN);
            showDialogClearWorkspace(dialog);

        }

        private void databaseCMLPredict_Click(object sender, RoutedEventArgs e)
        {
            DatabaseCMLTrainAndPredictWindow dialog = new DatabaseCMLTrainAndPredictWindow(this, DatabaseCMLTrainAndPredictWindow.Mode.PREDICT);
            showDialogClearWorkspace(dialog);

        }

        private void databaseCMLCompleteStep()
        {
            saveSelectedAnno(true);
            DatabaseCMLTrainAndPredictWindow dialog = new DatabaseCMLTrainAndPredictWindow(this, DatabaseCMLTrainAndPredictWindow.Mode.COMPLETE);
            dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            dialog.ShowDialog();
        }

        private void databaseCMLCompleteStep_Click(object sender, RoutedEventArgs e)
        {
            databaseCMLCompleteStep();
        }


        public string CMLExtractFeature(string chainPath, int nParallel, string fromPath, string toPath, string frameStep, string leftContext, string rightContext)
        {
            string result = "";
            string logPath = AppDomain.CurrentDomain.BaseDirectory + "\\cml-extract.log";

            File.Delete(logPath);

            try
            {
                string arguments = "-list " +
                    " -parallel " + nParallel +
                    " -step " + frameStep +
                    " -left " + leftContext +
                    " -right " + rightContext +
                    " -log " + logPath + " " +
                    chainPath + " " +
                    fromPath + " " +
                    toPath;

                Process process = new Process();
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.WindowStyle = ProcessWindowStyle.Normal;
                startInfo.FileName = AppDomain.CurrentDomain.BaseDirectory + "\\xmlchain.exe";
                startInfo.Arguments = arguments;
                result += "\n-------------------------------------------\r\n" + startInfo.FileName + startInfo.Arguments + "\n-------------------------------------------\r\n";
                process.StartInfo = startInfo;
                process.Start();
                process.WaitForExit();
                result += File.ReadAllText(logPath);    
            }
            catch (Exception ex)
            {
                MessageTools.Error(ex.ToString());
            }

            return result;
        }

        public string CMLTrainModel(string templatePath, string trainerPath, string datapath, string server, string username, string password, string database, string sessions, string scheme, string roles, string annotator, string stream, string leftContext, string rightContext, string balance, bool complete)
        {
            string result = "";

            string[] split = server.Split(':');
            string ip = split[0];
            string port = split[1];
            string logPath = AppDomain.CurrentDomain.BaseDirectory + "\\cml-train.log";

            File.Delete(logPath);

            try
            {
                string options_no_pass = "-left " + leftContext +
                        " -right " + rightContext +
                        " -balance " + balance +
                        " -username " + username +                        
                        " -list " + sessions +
                        (complete ? " -cooperative" : "") +
                        " -log " + logPath;
                string options = options_no_pass + " -password " + password;
                string arguments = "\"" + datapath + "\\" + database + "\" " +
                        ip + " " +
                        port + " " +
                        database + " " +
                        roles + " " +
                        scheme + " " +
                        annotator + " " +
                        "\"" + stream + "\" " +
                        "\"" + templatePath + "\" " +
                        "\"" + trainerPath + "\"";

                Process process = new Process();
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.WindowStyle = ProcessWindowStyle.Normal;
                startInfo.FileName = AppDomain.CurrentDomain.BaseDirectory + "\\cmltrain.exe";
                startInfo.Arguments = "--train " + options + " " + arguments;
                result += "\n-------------------------------------------\r\n" + startInfo.FileName + " --train " + options_no_pass + " " + arguments + "\n-------------------------------------------\r\n";
                process.StartInfo = startInfo;
                process.Start();
                process.WaitForExit();
                result += File.ReadAllText(logPath);
            }
            catch (Exception ex)
            {
                MessageTools.Error(ex.ToString());
            }

            return result;
        }

        public string CMLPredictAnnos(string trainerPath, 
            string datapath, 
            string server, 
            string username, 
            string password, 
            string database, 
            string sessions, 
            string scheme, 
            string roles, 
            string annotator, 
            string stream, 
            string leftContext, 
            string rightContext,
            double confidence,
            double minGap,
            double minDur,
            bool complete)
        {
            string result = "";

            string[] split = server.Split(':');
            string ip = split[0];
            string port = split[1];
            string logPath = AppDomain.CurrentDomain.BaseDirectory + "\\cml-predict.log";

            File.Delete(logPath);

            try
            {
                string options_no_pass = "-left " + leftContext +
                        " -right " + rightContext +
                        " -confidence " + confidence +
                        " -mingap " + minGap +
                        " -mindur " + minDur +
                        " -username " + username +
                        " -list " + sessions +
                        " -finished" +
                        ( complete ? " -cooperative" : "" ) +
                        " -log " + logPath;
                string options = options_no_pass + " -password " + password;
                string arguments = "\"" + datapath + "\\" + database + "\" " +
                        ip + " " +
                        port + " " +
                        database + " " +
                        roles + " " +
                        scheme + " " +
                        annotator + " " +
                        "\"" + stream + "\" " +
                        "\"" + trainerPath + "\"";

                Process process = new Process();
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.WindowStyle = ProcessWindowStyle.Normal;
                startInfo.FileName = AppDomain.CurrentDomain.BaseDirectory + "\\cmltrain.exe";
                startInfo.Arguments = "--forward " + options + " " + arguments;
                result += "\n-------------------------------------------\r\n" + startInfo.FileName + " --forward " + options_no_pass + " " + arguments + "\n-------------------------------------------\r\n";
                process.StartInfo = startInfo;
                process.Start();
                process.WaitForExit();
                result += File.ReadAllText(logPath);
            }
            catch (Exception ex)
            {
                MessageTools.Error(ex.ToString());
            }

            return result;
        }        

    }
}
