﻿using MongoDB.Bson;
using MongoDB.Driver;
using Octokit;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml;


namespace ssi
{
    public partial class MainHandler
    {


        #region DATABASELOGIC

        private void databaseConnect()
        {
            Action EmptyDelegate = delegate () { };
            control.ShadowBoxText.Text = "Connecting to Database...";
            control.ShadowBox.Visibility = Visibility.Visible;
            control.UpdateLayout();
            control.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);

            bool isConnected = DatabaseHandler.Connect();

            if (isConnected)
            {
                if (!DatabaseHandler.ChangeDatabase(Properties.Settings.Default.DatabaseName))
                {
                    Properties.Settings.Default.DatabaseName = null;
                    Properties.Settings.Default.Save();
                }                
            }
            else
            {
                MessageTools.Warning("Unable to connect to database, please check your settings");
                Properties.Settings.Default.DatabaseAutoLogin = false;
                Properties.Settings.Default.Save();                           
            }

            updateNavigator();
           
            control.ShadowBox.Visibility = Visibility.Collapsed;
            control.ShadowBoxText.Text = "Loading Data...";
        }

        private void DatabaseConnectMenu_Click(object sender, RoutedEventArgs e)
        {
            databaseConnect();
        }

       

        private void databaseManageDBs()
        {           
            DatabaseAdminManageDBWindow dialog = new DatabaseAdminManageDBWindow();
            showDialogClearWorkspace(dialog);
        }

        private void databaseManageUsers()
        {            
            DatabaseAdminManageUsersWindow dialog = new DatabaseAdminManageUsersWindow();
            showDialogClearWorkspace(dialog);
        }
        private void databaseManageSessions()
        {
            DatabaseAdminManageSessionsWindow dialog = new DatabaseAdminManageSessionsWindow();
            showDialogClearWorkspace(dialog);
        }

        private void databaseManageAnnotations()
        {
            DatabaseAdminManageAnnotationsWindow dialog = new DatabaseAdminManageAnnotationsWindow();
            showDialogClearWorkspace(dialog);
        }
        private void databaseLoadSession()
        {
            

            DatabaseAnnoMainWindow dialog = new DatabaseAnnoMainWindow();
            if(showDialogClearWorkspace(dialog) && (dialog.DialogResult == true))
            {

                Action EmptyDelegate = delegate () { };
                control.ShadowBox.Visibility = Visibility.Visible;
                control.UpdateLayout();
                control.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);

                System.Collections.IList annotations = dialog.Annotations();           
                if (annotations != null && annotations.Count > 0)
                {
                    List<AnnoList> annoLists = DatabaseHandler.LoadSession(annotations);
                    if (annoLists != null)
                    {
                        foreach (AnnoList annoList in annoLists)
                        {
                            addAnnoTierFromList(annoList);
                        }
                    }
                }

                control.ShadowBox.Visibility = Visibility.Collapsed;

                List<string> streams = dialog.SelectedStreams();
                databaseSessionStreams = streams;

                if (streams != null && streams.Count > 0)
                {
                    List<string> streamsAll = new List<string>();
                    foreach (string stream in streams)
                    {
                        if (stream.EndsWith("stream"))
                        {
                            streamsAll.Add(stream + "~");
                        }
                        streamsAll.Add(stream);

                    }

                    try
                    {
                        if (filesToDownload != null)
                        {
                            filesToDownload.Clear();
                        }

                        MainHandler.NumberOfAllCurrentDownloads = streamsAll.Count;

                        foreach (string stream in streamsAll)
                        {
                            string localPath = Properties.Settings.Default.DatabaseDirectory + "\\" + DatabaseHandler.DatabaseName + "\\" + DatabaseHandler.SessionName + "\\" + stream;
                            string url = "";
                            bool requiresAuth = false;

                            DatabaseDBMeta meta = new DatabaseDBMeta()
                            {
                                Name = DatabaseHandler.DatabaseName
                            };
                            if (!DatabaseHandler.GetDBMeta(ref meta))
                            {
                                continue;
                            }
                            if (meta.Server == "")
                            {
                                continue;
                            }
                           
                            //In case we host our files on nextcloud, the file format is special. For now we only allow self-hosted, but in the future we add an option for nextcloud in general.
                            if(meta.Server.Contains("https://hcm-lab.de/cloud"))
                            {
                                url = meta.Server + "/download?path=%2F" + DatabaseHandler.DatabaseName + "%2F" + DatabaseHandler.SessionName + "&files=" + stream;
                            }
                            else
                            { 
                                url = meta.Server + '/' + DatabaseHandler.SessionName + '/' + stream;
                                requiresAuth = meta.ServerAuth;
                            }

                            string[] split = url.Split(':');
                            string connection = split[0];                        
                        
                            Directory.CreateDirectory(Path.GetDirectoryName(localPath));

                            if (connection == "sftp")
                            {                            
                                SFTP(url, localPath);
                            }
                            else if (connection == "http" || connection == "https" && requiresAuth == false)
                            {                            
                                httpGet(url, localPath);
                            }
                            else if (connection == "http" || connection == "https" && requiresAuth == true)
                            {
                                httpPost(url, localPath);
                            }
                        }
                    }
                    catch
                    {
                        MessageBox.Show("Make sure ip, login and password are correct", "Connection to database not possible");
                    }
                }
            }
        }

        public void ReloadAnnoTierFromDatabase(AnnoTier tier, bool loadBackup)
        {
            if (tier == null || tier.AnnoList == null)
            {
                return;
            }

            if (loadBackup && tier.AnnoList.Source.Database.DataBackupOID == AnnoSource.DatabaseSource.ZERO)
            {                
                MessageTools.Warning("No backup exists");
                return;
            }

            Action EmptyDelegate = delegate () { };
            control.ShadowBoxText.Text = "Reloading Annotation";
            control.ShadowBox.Visibility = Visibility.Visible;
            control.UpdateLayout();
            control.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);

            DatabaseAnnotation annotation = new DatabaseAnnotation();
            annotation.Role = tier.AnnoList.Meta.Role;
            annotation.Scheme = tier.AnnoList.Scheme.Name;
            annotation.AnnotatorFullName = tier.AnnoList.Meta.AnnotatorFullName;
            annotation.Annotator = tier.AnnoList.Meta.Annotator;
            annotation.Session = DatabaseHandler.SessionName;

            AnnoList annoList = DatabaseHandler.LoadAnnoList(annotation, loadBackup);
            double maxdur = 0;

            if (annoList != null && annoList.Count > 0 && annoList.Scheme.Type == AnnoScheme.TYPE.DISCRETE)
            {
                maxdur = annoList[annoList.Count - 1].Stop;

                setAnnoList(annoList);
                tier.Children.Clear();
                tier.AnnoList.Clear();
                tier.segments.Clear();
                tier.AnnoList = annoList;

                foreach (AnnoListItem item in annoList)
                {
                    tier.AddSegment(item);
                }

                tier.TimeRangeChanged(Time);
                updateTimeRange(maxdur);

                tier.AnnoList.HasChanged = false;          
            }

            control.ShadowBox.Visibility = Visibility.Collapsed;
        }

        private void addNewAnnotationDatabase()
        {
            if (Time.TotalDuration > 0)
            {              
                string annoScheme = DatabaseHandler.SelectScheme();
                if (annoScheme == null)
                {
                    return;
                }

                string role = DatabaseHandler.SelectRole();
                if (role == null)
                {
                    return;
                }

                AnnoScheme scheme = DatabaseHandler.GetAnnotationScheme(annoScheme);
                if (scheme == null)
                {
                    return;
                }
                scheme.Labels.Add(new AnnoScheme.Label("GARBAGE", Colors.Black));

                ObjectId annotatid = DatabaseHandler.GetObjectID(DatabaseDefinitionCollections.Annotators, "name", Properties.Settings.Default.MongoDBUser);
                string annotator = Properties.Settings.Default.MongoDBUser;
                string annotatorFullName = DatabaseHandler.FetchDBRef(DatabaseDefinitionCollections.Annotators, "fullname", annotatid);

                AnnoList annoList;
                if (DatabaseHandler.AnnotationExists(annotator, DatabaseHandler.SessionName, role, scheme.Name))
                {
                    DatabaseAnnotation annotation = new DatabaseAnnotation()
                    {
                        Annotator = annotator,
                        Session = DatabaseHandler.SessionName,
                        Role = role,
                        Scheme = scheme.Name
                    };
                    annoList = DatabaseHandler.LoadAnnoList(annotation, false);
                    annoList.HasChanged = false;
                }
                else
                {
                    annoList = new AnnoList();
                    annoList.Meta.Role = role;
                    annoList.Meta.Annotator = annotator;
                    annoList.Meta.AnnotatorFullName = annotatorFullName;
                    annoList.Scheme = scheme;
                    annoList.Source.StoreToDatabase = true;
                    annoList.Source.Database.Session = DatabaseHandler.SessionName;
                    annoList.HasChanged = true;
                    
                }

                addAnnoTier(annoList);
                control.annoListControl.editComboBox.SelectedIndex = 0;
            }
            else
            {
                MessageTools.Warning("Nothing to annotate, load some data first.");
            }
        }

        #endregion DATABASELOGIC


        #region EVENTHANDLERS

        private void databaseLoadSession_Click(object sender, RoutedEventArgs e)
        {
            databaseLoadSession();
        }

        private void databaseManageDBs_Click(object sender, RoutedEventArgs e)
        {
            databaseManageDBs();
        }

        private void databaseManageUsers_Click(object sender, RoutedEventArgs e)
        {
            databaseManageUsers();
        }

        private void databaseManageSessions_Click(object sender, RoutedEventArgs e)
        {
            databaseManageSessions();
        }

        private void databaseManageAnnotations_Click(object sender, RoutedEventArgs e)
        {
            databaseManageAnnotations();
        }

        private void databaseCMLMergeAnnotations_Click(object sender, RoutedEventArgs e)
        {
            DatabaseAnnoMergeWindow window = new DatabaseAnnoMergeWindow();
            window.ShowDialog();

            if(window.DialogResult == true)
            { 

                AnnoList rms = window.RMS();
                AnnoList median = window.Mean();
                AnnoList merge = window.Merge();
                if (rms != null)
                {
                    rms.Save();
                }
                if (median != null)
                {
                    median.Save();
                }

                if (merge != null)
                {
                    merge.Save();
                }

            }
        }



        #endregion EVENTHANDLERS



        public static string Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        public static string Decode(string base64EncodedData)
        {
            //try catch is if password is still in old format. deprecated in the future
            try
            {
                var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
                return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
            }
            catch
            {
                return base64EncodedData;
            }
        }

    }

}
