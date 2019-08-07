using System;
using System.Collections.Generic;
using System.Configuration;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;

using DatabaseVersioningTool.Application;
using DatabaseVersioningTool.DataAccess.Sql;

namespace DatabaseVersioningTool.WPF
{
    public partial class MainWindow : Window
    {
        private static bool isSetup = false;
        
        string SelectedDatabaseName
        {
            get
            {
                return cboDatabase.Text;
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            App.DatabaseManager.OnLogUpdated += DatabaseManager_OnLogUpdated;   
        }

        private void DatabaseManager_OnLogUpdated(object sender, LogUpdatedEventArgs e)
        {
            txtLog.Text = txtLog.Text += e.Message + "\n";
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);

            if (isSetup == false)
            {
                var folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
                folderBrowserDialog.RootFolder = Environment.SpecialFolder.MyComputer;
                folderBrowserDialog.SelectedPath = ConfigurationManager.AppSettings["DefaultScriptPath"];

                System.Windows.Forms.DialogResult result = folderBrowserDialog.ShowDialog(this.GetIWin32Window());

                if (result == System.Windows.Forms.DialogResult.OK || result == System.Windows.Forms.DialogResult.Yes)
                {
                    Setup(folderBrowserDialog.SelectedPath);
                }

                isSetup = true;

                this.BringIntoView();
                this.Focus();
            }
        }

        private void Setup(string scriptPath)
        {
            EnsurePrerequisites(scriptPath);

            PopulateDatabaseComboSelector();
        }

        private void EnsurePrerequisites(string scriptPath)
        {
            FileManager.Manager.Initialise(scriptPath);
            App.DatabaseManager.VersionTracker.Load();
        }

        private void PopulateDatabaseComboSelector()
        {
            IEnumerable<string> databaseNames = App.DatabaseManager.GetDatabaseNames(App.DatabaseConnection);
            if (databaseNames != null)
            {
                foreach (string name in databaseNames)
                {
                    cboDatabase.Items.Add(new ComboBoxItem() { Content = name });
                }
            }
        }

        private void PopulateVersionSelector()
        {
            cboVersion.Items.Clear();
            var availableVersions = App.DatabaseManager.VersionTracker.GetAvailableVersions(App.DatabaseConnection.DatabaseName, App.DatabaseManager.GetDatabaseVersion(App.DatabaseConnection));
            if (availableVersions != null)
            {
                foreach (var version in availableVersions)
                {
                    cboVersion.Items.Add(new ComboBoxItem() { Content = version.To });
                }
            }
        }


        private void btnRestore_Click(object sender, RoutedEventArgs e)
        {
            SetStateAsBusy();

            var fileDialog = new System.Windows.Forms.OpenFileDialog();
            System.Windows.Forms.DialogResult result = fileDialog.ShowDialog(this.GetIWin32Window());

            if (result == System.Windows.Forms.DialogResult.OK || result == System.Windows.Forms.DialogResult.Yes)
            {
                try
                {
                    App.DatabaseManager.RestoreDatabase(App.DatabaseConnection, SelectedDatabaseName, fileDialog.FileName);
                    System.Windows.Forms.MessageBox.Show($"{SelectedDatabaseName} successfully restored");
                }
                catch
                {
                    System.Windows.Forms.MessageBox.Show($"Problem with restoring database {SelectedDatabaseName} to directory, check you have write permissions to this directory", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            SetStateAsFree();
        }

        private void btnBackup_Click(object sender, RoutedEventArgs e)
        {
            bool success = false;
            SetStateAsBusy();

            var dlg = new FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = dlg.ShowDialog(this.GetIWin32Window());

            if (result == System.Windows.Forms.DialogResult.OK || result == System.Windows.Forms.DialogResult.Yes)
            {
                if (!string.IsNullOrEmpty(dlg.SelectedPath))
                {
                    try
                    {
                        App.DatabaseManager.BackUpDatabase(App.DatabaseConnection, dlg.SelectedPath);
                    }
                    catch
                    {
                        System.Windows.Forms.MessageBox.Show($"Problem with writing to directory { dlg.SelectedPath }, check you have write permissions to this directory", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }

            if (success)
            {
                System.Windows.Forms.MessageBox.Show($"{SelectedDatabaseName} successfully backed up");
            }
            else
            {
                System.Windows.Forms.MessageBox.Show($"backup failed for {SelectedDatabaseName}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            SetStateAsFree();
        }

        private void btnCreateUpdate_Click(object sender, RoutedEventArgs e)
        {
            SetStateAsBusy();

            string sql = txtScript.Text;

            try
            {
                App.DatabaseManager.ValidateUpdate(App.DatabaseConnection, new SqlDatabaseUpdate() { Sql = sql });

                App.DatabaseManager.CreateDatabaseUpdate(App.DatabaseConnection, new SqlDatabaseUpdate() { Sql = sql });
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message, "Error creating update", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            PopulateVersionSelector();

            SetSelectedVersion();

            SetStateAsFree();
        }

        private void btnUpgrade_Click(object sender, RoutedEventArgs e)
        {
            SetStateAsBusy();
            if(cboVersion.SelectedValue == null)
            {
                System.Windows.MessageBox.Show("No version selected");
                return;
            }

            string version = (string)((ContentControl)(cboVersion).SelectedValue).Content;

            try
            {
                App.DatabaseManager.UpgradeDatabaseToVersion(App.DatabaseConnection, version);
                string currentVersion = App.DatabaseManager.GetDatabaseVersion(App.DatabaseConnection);
                lblDbVersion.Content = currentVersion;
                System.Windows.MessageBox.Show($"Upgrade to {currentVersion} successful");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            SetStateAsFree();
        }

        private void cboDatabase_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SetStateAsBusy();

            string selectedDatabase = (string)((ContentControl)((System.Windows.Controls.ComboBox)(sender)).SelectedValue).Content;
            App.DatabaseName = selectedDatabase;

            PopulateVersionSelector();

            SetSelectedVersion();

            SetStateAsFree();
        }

        private void cboVersion_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                bool enable = ((ComboBoxItem)e.AddedItems[0]).IsEnabled;

                btnUpgrade.IsEnabled = enable;
            }
        }

        private void btnCreateScripts_Click(object sender, RoutedEventArgs e)
        {
            SetStateAsBusy();
            App.DatabaseManager.GenerateCreateScripts(App.DatabaseConnection);
            SetStateAsFree();
        }

        #region Shared

        private void SetSelectedVersion()
        {
            string version = App.DatabaseManager.GetDatabaseVersion(App.DatabaseConnection);

            lblDbVersion.Content = version;

            foreach (ComboBoxItem item in cboVersion.Items)
            {
                if (item.Content.Equals(version))
                {
                    item.IsSelected = true;
                    item.IsEnabled = false;
                }
            }
        }

        private void SetStateAsFree()
        {
            this.Opacity = 1;
            this.Cursor = System.Windows.Input.Cursors.Arrow;
            this.IsEnabled = true;

            SetControlState(this.IsEnabled);
        }

        private void SetStateAsBusy()
        {
            this.IsEnabled = false;
            this.Cursor = System.Windows.Input.Cursors.Wait;
            this.Opacity = .5;

            SetControlState(this.IsEnabled);
        }

        private void SetControlState(bool enable)
        {
            btnBackup.IsEnabled = enable;
            btnRestore.IsEnabled = enable;
            btnCreateScripts.IsEnabled = enable;
            btnCompare.IsEnabled = enable;

            cboVersion.IsEnabled = enable;

            txtScript.IsEnabled = enable;
            btnCreateUpdate.IsEnabled = enable;
        }

        #endregion

    }
}
