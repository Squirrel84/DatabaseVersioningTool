using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;

using DatabaseVersioningTool.DataAccess;

namespace DatabaseVersioningTool.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);

            this.Setup();
        }

        private void Setup()
        {
            EnsurePrerequisites();

            PopulateDatabaseComboSelector();
        }

        private static readonly DatabaseManager DatabaseManager = new DatabaseManager();

        string SelectedDatabaseName
        {
            get
            {
                return cboDatabase.Text;
            }
        }



        private void EnsurePrerequisites()
        {
            FileManager.Manager.Initialise();
            DatabaseManager.VersionTracker.Load();
        }


        private void PopulateDatabaseComboSelector()
        {
            string errorMessage = string.Empty;
            IEnumerable<string> databaseNames = DatabaseManager.GetDatabaseNames(App.GetDatabaseConnection(), out errorMessage);
            if (databaseNames != null)
            {
                foreach (string name in databaseNames)
                {
                    cboDatabase.Items.Add(new ComboBoxItem() { Content = name });
                }
            }
            else
            {
                System.Windows.Forms.MessageBox.Show(errorMessage);
            }
        }

        private void PopulateVersionSelector()
        {
            cboVersion.Items.Clear();
            DatabaseVersionCollection database = DatabaseManager.VersionTracker.Versions.SingleOrDefault(x => x.Name == App.GetDatabaseConnection().DatabaseName);
            if (database != null)
            {
                foreach (DatabaseVersion version in database.Versions)
                {
                    cboVersion.Items.Add(new ComboBoxItem() { Content = version.To });
                }
            }
        }


        private void btnRestore_Click(object sender, RoutedEventArgs e)
        {
            string errorMessage;
            SetStateAsBusy();

            var dlg = new System.Windows.Forms.OpenFileDialog();
            System.Windows.Forms.DialogResult result = dlg.ShowDialog(this.GetIWin32Window());

            if (result == System.Windows.Forms.DialogResult.OK || result == System.Windows.Forms.DialogResult.Yes)
            {
                if (DatabaseManager.RestoreDatabase(App.GetDatabaseConnection(), SelectedDatabaseName, dlg.FileName, out errorMessage))
                {
                    System.Windows.Forms.MessageBox.Show(string.Format("{0} successfully restored", SelectedDatabaseName));
                }
                else
                {
                    System.Windows.Forms.MessageBox.Show(errorMessage);
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
                        success = DatabaseManager.BackUpDatabase(App.GetDatabaseConnection(), dlg.SelectedPath);
                    }
                    catch
                    {
                        System.Windows.Forms.MessageBox.Show("Problem with writing to directory, check you have write permissions to this directory", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }

            if (success)
            {
                System.Windows.Forms.MessageBox.Show(String.Format("{0} successfully backed up", SelectedDatabaseName));
            }
            else
            {
                System.Windows.Forms.MessageBox.Show(String.Format("backup failed for {0}", SelectedDatabaseName), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            SetStateAsFree();
        }

        private void btnCreateUpdate_Click(object sender, RoutedEventArgs e)
        {
            SetStateAsBusy();

            string sql = txtScript.Text;
            string errorMessage = string.Empty;

            try
            {
                DatabaseManager.RunUpdate(App.GetDatabaseConnection(), sql);

                DatabaseManager.CreateDatabaseUpdate(App.GetDatabaseConnection(), sql);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message, "Error Creating Update", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            PopulateVersionSelector();

            SetSelectedVersion();

            SetStateAsFree();
        }

        private void btnUpgrade_Click(object sender, RoutedEventArgs e)
        {
            string version = (string)((ContentControl)(cboVersion).SelectedValue).Content;

            try
            {
                DatabaseManager.Upgrade(App.GetDatabaseConnection(), version);
                System.Windows.MessageBox.Show("Upgrade Successful");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void cboDatabase_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SetStateAsBusy();

            btnBackup.IsEnabled = true;
            btnRestore.IsEnabled = true;

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
                btnUpgrade.IsEnabled = ((ComboBoxItem)e.AddedItems[0]).IsEnabled;
            }
        }


        #region Shared

        private void SetSelectedVersion()
        {
            string version = DatabaseManager.GetDatabaseVersion(App.GetDatabaseConnection());

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
        }

        private void SetStateAsBusy()
        {
            this.IsEnabled = false;
            this.Cursor = System.Windows.Input.Cursors.Wait;
            this.Opacity = .5;
        }

        #endregion
    }
}
