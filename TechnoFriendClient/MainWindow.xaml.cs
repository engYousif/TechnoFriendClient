using System;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.IO.Compression;
using System.IO;
using System.Threading;
using System.Linq;
using System.Data.SqlClient;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Win32;
using System.Security.Cryptography;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Diagnostics;

namespace TechnoFriendClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        Thread doWorkThread = null;
        Thread doBackupThread = null;

        NotifyIcon ni = new NotifyIcon();
        string path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        DataClasses1DataContext d = new DataClasses1DataContext();
        DataClasses1DataContext d2 = new DataClasses1DataContext();

        RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

        public MainWindow()
        {
            InitializeComponent();
            try
            {
                //FileInfo _file1 = new FileInfo("tfclient.mdf");
                //FileInfo _file2 = new FileInfo("tfclient_log.ldf");
                //if (_file1.Attributes == FileAttributes.ReadOnly)
                //    _file1.Attributes = FileAttributes.Normal;

                //AddFileSecurity("tfclient.mdf", FileSystemRights.FullControl, AccessControlType.Allow);
                //if (_file2.Attributes == FileAttributes.ReadOnly)
                //    _file2.Attributes = FileAttributes.Normal;
                doWorkThread = new Thread(new ThreadStart(DoWork));
                //doBackupThread = new Thread(new ThreadStart(dobackup));
                doWorkThread.Start();
            }
            catch (Exception k) { System.Windows.Forms.MessageBox.Show(k.Message); }
        }


        
        public static void AddFileSecurity(string fileName,
           FileSystemRights rights, AccessControlType controlType)
        {

            // Get a FileSecurity object that represents the
            // current security settings.
            FileSecurity fSecurity = File.GetAccessControl(fileName);


            // Add the FileSystemAccessRule to the security settings.
            fSecurity.AddAccessRule(new FileSystemAccessRule(@"Everyone", FileSystemRights.FullControl, AccessControlType.Allow));

            // Set the new access settings.
            File.SetAccessControl(fileName, fSecurity);
        }


        public void DoWork()
        {
            try
            {
                for (; ; )
                {
                    #region Do work
                    Thread.Sleep(1000);
                    //Get all files in the folder
                    List<string> files = Directory.GetFiles(path + "\\Backup").ToList<string>();
                    //Remove the backup files path from the list that it will not be consodered in the dates comparison
                    if (files.FindIndex(a => a.Contains("eBackup.zip")) >= 0)
                        files.RemoveAt(files.FindIndex(a => a.Contains("eBackup.zip")));



                    //Get total numebr (this will indicate if a file deleted)
                    int filesCount = files.Count();

                    //Get the last modification date, to be give in to the log
                    long lastModTime = File.GetLastWriteTime(files[0]).Ticks;
                    for (int i = 1; i < files.Count(); i++)
                    {
                        if (lastModTime < File.GetLastWriteTime(files[i]).Ticks)
                            lastModTime = File.GetLastWriteTime(files[i]).Ticks;
                    }

                    //Conditions are
                    //First condition, if no eBackup.zip exists, do backup
                    //Second condition, If new modification in files happened, do backup
                    //Third condition, files count changed, do backup
                    //Fourth condition, for the first time, do backup

                    //Create backup.zip in case of non of the conditions met, to be uploaded to server
                    //First condition
                    if (File.Exists(path + "\\Backup\\eBackup.zip") == false)
                    {
                        dobackup();
                        //doBackupThread.Start();
                    }

                    //Check if this modification is new
                    //Second condition
                    //here logs are already exist
                    if ((from k in d.logs select k.time).Count() > 0 == true)//Means there exist logs
                    {
                        var log = (from k in d.logs
                                   select k).First();//a pointer to the log in the database
                        if (log == null)//for the first run of the application
                        {
                            dobackup();
                            log.time = lastModTime;//set the time to the last one
                            log.filesCount = filesCount;//after backup the filesCount should be set as well
                            d.SubmitChanges();
                            //doBackupThread.Start();
                        }
                        if (log.filesCount > filesCount || log.filesCount < filesCount)//Indicates a change in files count, Third condition
                        {
                            dobackup();
                            //doBackupThread.Start();
                            log.filesCount = filesCount;
                            log.time = lastModTime;
                            d.SubmitChanges();
                        }
                        else if ((lastModTime > log.time) && (log != null))//Second condition, a modification happened
                        {
                            dobackup();
                            log.time = lastModTime;
                            log.filesCount = filesCount;
                            d.SubmitChanges();
                            //doBackupThread.Start();

                        }

                    }
                    else //If no logs exist yet
                    {
                        log l = new log();
                        l.time = lastModTime;
                        l.filesCount = filesCount;
                        d.logs.InsertOnSubmit(l);
                        d.SubmitChanges();
                        if (File.Exists(path + "\\Backup\\eBackup.zip") == true)//no log and file exist means a problem exist and the file must be deleted
                        {
                            File.Delete(path + "\\Backup\\eBackup.zip");
                        }
                        dobackup();
                        //doBackupThread.Start();
                    }
                }
                #endregion
            }
            catch (Exception k)
            {
                System.Windows.Forms.MessageBox.Show(k.Message);
            }
        }

        /*
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            #region Do work

            //Get all files in the folder
            List<string> files = Directory.GetFiles(path + "\\Backup").ToList<string>();
            //Remove the backup files path from the list that it will not be consodered in the dates comparison
            if (files.FindIndex(a => a.Contains("Backup.zip")) >= 0)
                files.RemoveAt(files.FindIndex(a => a.Contains("Backup.zip")));


            //Get the last modification date
            long last = File.GetLastWriteTime(files[0]).Ticks;
            //Get total numebr (this will indicate if a file deleted)
            int total = files.Count();

            for (int i = 1; i < files.Count(); i++)
            {
                if (last < File.GetLastWriteTime(files[i]).Ticks)
                    last = File.GetLastWriteTime(files[i]).Ticks;
            }

            //Create backup.zip in case of non of the conditions met, to be uploaded to sever
            if (File.Exists(path + "\\Backup\\Backup.zip") == false)
            {
                dobackup();
            }

            //Check if this modification is new
            if ((from k in d.logs select k.time).Count() > 0 == true)
            {
                var dbtime = (from k in d.logs
                              select k).First();
                if (dbtime == null)//for the first run of the application
                {
                    dbtime.time = last;
                    d.SubmitChanges();
                    dobackup();
                }
                if (dbtime.total > total)
                {
                    dobackup();
                    dbtime.total = total;
                    d.SubmitChanges();
                }
                else if ((last > dbtime.time) && (dbtime != null))
                {
                    dbtime.time = last;
                    d.SubmitChanges();
                    dobackup();

                }

            }
            else
            {
                log l = new log();
                l.time = last;
                l.total = total;
                d.logs.InsertOnSubmit(l);
                d.SubmitChanges();
                if (File.Exists(path + "\\Backup\\Backup.zip") == true)
                {
                    File.Delete(path + "\\Backup\\Backup.zip");
                }
                dobackup();

            }
            #endregion
        }
        */

        
        private void dobackup()
        {
            try
            {
               
                #region Do Backup
                //Thread th = new Thread(new ThreadStart(
                    //delegate
                    //{
                        
                        try
                        {
                            
                            if (File.Exists(path + "\\Backup\\eBackup.zip") == true)//Delete Old
                            {
                                File.Delete(path + "\\Backup\\eBackup.zip");
                            }
                            if (File.Exists(path + "\\TechnoFriend000111_tmp_eBackup_tmp.zip") == true)
                            {
                                File.Delete(path + "\\TechnoFriend000111_tmp_eBackup_tmp.zip");
                                
                            }
                            ZipFile.CreateFromDirectory(path + "\\Backup", path + "\\TechnoFriend000111_tmp_eBackup_tmp.zip", CompressionLevel.Optimal, true);//Compress
                            
                            File.Move(path + "\\TechnoFriend000111_tmp_eBackup_tmp.zip", path + "\\Backup\\eBackup.zip");//Move the new one to the Backup folder
                            

                            //Encryption
                            byte[] key = Encoding.ASCII.GetBytes(@"2Techno0Friend20");
                            byte[] IV = Encoding.ASCII.GetBytes("@3l2-dld56/==-2k");
                            DoEncrypt(path + "\\Backup\\eBackup.zip", path + "\\Backup\\Backup.zip", key, IV);
                            File.Delete(path + "\\Backup\\eBackup.zip");
                            File.Move(path + "\\Backup\\Backup.zip", path + "\\Backup\\eBackup.zip");
                            ni.BalloonTipText = "Done";
                            ni.ShowBalloonTip(1000);



                        }
                        catch (Exception FF)
                        {
                            System.Windows.Forms.MessageBox.Show(FF.Message);
                            if (FF.Message.Contains("already exists"))
                                if (File.Exists(path + "\\eBackup.zip") == true)
                                {
                                    File.Delete(path + "\\eBackup.zip");
                                }
                        }


                        
                        //Add information to the listbox
                        Dispatcher.Invoke(new Action(() =>
                            {
                                logger.Items.Add(DateTime.Now.ToString());
                            }
                                    ));

                        //Add to historylist
                        history h = new history();
                        h.log = DateTime.Now;
                        d2.histories.InsertOnSubmit(h);
                        d2.SubmitChanges();

                    //}
                    //));
                
                //th.Start();th.Join();
                #endregion



            }
            catch (Exception k) { System.Windows.Forms.MessageBox.Show(k.Message); }
        }

        private void DoEncrypt(string inputFile, string outputFile, byte[] Key, byte[] IV)
        {
            try
            {
                string cryptFile = outputFile;
                FileStream fsCrypt = new FileStream(cryptFile, FileMode.Create);

                RijndaelManaged RMCrypto = new RijndaelManaged();

                RMCrypto.Padding = PaddingMode.PKCS7;

                CryptoStream cs = new CryptoStream(fsCrypt,
                        RMCrypto.CreateEncryptor(Key, IV),
                        CryptoStreamMode.Write);

                FileStream fsIn = new FileStream(inputFile, FileMode.Open);

                int data;
                while ((data = fsIn.ReadByte()) != -1)
                    cs.WriteByte((byte)data);


                fsIn.Close();
                cs.Close();
                fsCrypt.Close();
            }
            catch
            {
                logger.Items.Add("Encryption failed! Error");
            }
        }



        private void Window_StateChanged(object sender, System.EventArgs e)
        {
            try
            {
                if (this.WindowState == WindowState.Minimized)
                {
                    //ni.Icon = new Icon("../Resources//logo.ico");
                    ni.Icon = new Icon("Resources//logo.ico");
                    ni.Visible = true;
                    ni.BalloonTipText = "TechnoFriend in Tray..";
                    ni.ShowBalloonTip(1000);
                    ni.DoubleClick += Ni_DoubleClick;

                    System.Windows.Forms.ContextMenu conmen = new System.Windows.Forms.ContextMenu();
                    conmen.MenuItems.Add("Open", new System.EventHandler(open));
                    conmen.MenuItems.Add("Exit", new System.EventHandler(exit));
                    ni.ContextMenu = conmen;

                    this.Hide();
                }
            }
            catch (Exception k)
            {
                System.Windows.Forms.MessageBox.Show(k.Message);
            }
        }

        public void open(object sender, EventArgs e)
        {
            this.Show();
            ni.Visible = false;
            this.WindowState = WindowState.Normal;
        }

        public void exit(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        private void Ni_DoubleClick(object sender, System.EventArgs e)
        {
            this.Show();
            ni.Visible = false;
            this.WindowState = WindowState.Normal;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Environment.Exit(0);
        }

        private void Ellipse_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                LogWindow l = new LogWindow();
                l.Show();
            }
            catch (Exception k)
            {
                System.Windows.Forms.MessageBox.Show(k.Message);
            }
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            try
            {

                registryKey.SetValue("TechnoFriend", System.Windows.Forms.Application.ExecutablePath);

            }
            catch (Exception k)
            {
                System.Windows.Forms.MessageBox.Show(k.Message);
            }
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                registryKey.DeleteValue("TechnoFriend");
            }
            catch (Exception k)
            {
                System.Windows.Forms.MessageBox.Show(k.Message);
            }
        }


        /*
        private bool flag;
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Thread jj = new Thread(new ThreadStart(
                delegate
                {

                    try
                    {
                        if (flag)
                        {
                            d.logs.DeleteAllOnSubmit(d.logs.Select(a => a));
                            flag = false;
                        }
                        log l = new log();
                        l.time = DateTime.Now.Ticks;
                        d.logs.InsertOnSubmit(l);
                        d.SubmitChanges();

                    }
                    catch (SqlException ex)
                    {
                        d.ExecuteCommand("DBCC CHECKIDENT('log', RESEED, 0);");
                        d.logs.DeleteAllOnSubmit(d.logs.Select(a => a));
                        d.SubmitChanges();
                        flag = true;
                    }

                }
                ));
            jj.Start();

        }

        */
    }
}
