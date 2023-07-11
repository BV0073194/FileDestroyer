using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;

namespace FileDeleter
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void FindFilesButton_Click(object sender, RoutedEventArgs e)
        {
            string folderPath = folderPathTextBox.Text;
            string fileExtension = fileExtensionTextBox.Text;

            if (string.IsNullOrWhiteSpace(folderPath))
            {
                MessageBox.Show("Please enter a valid folder path.");
                return;
            }

            if (!Directory.Exists(folderPath))
            {
                MessageBox.Show("The specified folder does not exist.");
                return;
            }

            string[] files = string.IsNullOrWhiteSpace(fileExtension)
                ? Directory.GetFiles(folderPath)
                : Directory.GetFiles(folderPath, $"*.{fileExtension}");

            fileListBox.ItemsSource = files.Select(Path.GetFileName);
        }

        private void RemoveSelectedButton_Click(object sender, RoutedEventArgs e)
        {
            if (fileListBox.SelectedItems.Count == 0)
            {
                MessageBox.Show("No files selected.");
                return;
            }

            List<string> selectedItems = fileListBox.SelectedItems.Cast<string>().ToList();
            List<string> fileItems = fileListBox.ItemsSource.Cast<string>().ToList();

            foreach (string selectedItem in selectedItems)
            {
                fileItems.Remove(selectedItem);
            }

            fileListBox.ItemsSource = null; // Remove the binding temporarily
            fileListBox.ItemsSource = fileItems; // Set the modified list as the new ItemsSource

            // Refresh the selection
            fileListBox.SelectedItems.Clear();
            foreach (string selectedItem in selectedItems)
            {
                fileListBox.SelectedItems.Add(selectedItem);
            }
        }

        private void DeleteSelectedButton_Click(object sender, RoutedEventArgs e)
        {
            if (fileListBox.SelectedItems.Count == 0)
            {
                MessageBox.Show("No files selected.");
                return;
            }

            bool permanentDelete = permanentDeleteCheckBox.IsChecked == true;

            if (permanentDelete)
            {
                // Prompt for confirmation once
                MessageBoxResult result = MessageBox.Show(
                    "Are you sure you want to permanently delete the selected files?",
                    "Confirm Deletion",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    PerformFileDeletion(permanentDelete);
                }
            }
            else
            {
                // Check if admin privileges are required
                bool isAdminRequired = RequiresAdminPrivileges();

                if (isAdminRequired)
                {
                    // Prompt for admin privileges confirmation
                    MessageBoxResult adminResult = MessageBox.Show(
                        "This operation requires administrator privileges. Do you want to continue?",
                        "Confirm Action",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (adminResult == MessageBoxResult.Yes)
                    {
                        PerformFileDeletion(permanentDelete);
                    }
                }
                else
                {
                    PerformFileDeletion(permanentDelete);
                }
            }
        }

        private void PerformFileDeletion(bool permanentDelete)
        {
            string folderPath = folderPathTextBox.Text;

            foreach (string fileName in fileListBox.SelectedItems)
            {
                string filePath = Path.Combine(folderPath, fileName);

                try
                {
                    if (permanentDelete)
                    {
                        File.Delete(filePath);
                    }
                    else
                    {
                        // Move the file to the recycle bin
                        FileSystem.DeleteFile(filePath, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to delete the file: {ex.Message}");
                }
            }

            MessageBox.Show(permanentDelete ? "Selected files permanently deleted." : "Selected files moved to the recycle bin.");
            FindFilesButton_Click(null, null);
        }

        private bool RequiresAdminPrivileges()
        {
            if (!permanentDeleteCheckBox.IsChecked == true)
                return false;

            if (!Environment.OSVersion.Platform.Equals(PlatformID.Win32NT))
                return false;

            try
            {
                using (Process process = new Process())
                {
                    process.StartInfo.FileName = "cmd.exe";
                    process.StartInfo.Arguments = "/k echo Elevated privileges acquired.";
                    process.StartInfo.Verb = "runas";
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    process.Start();

                    process.WaitForExit();
                }

                return false; // Admin privileges are not required
            }
            catch (System.ComponentModel.Win32Exception)
            {
                return true; // Admin privileges are required
            }
        }


        private void ToggleAllButton_Click(object sender, RoutedEventArgs e)
        {
            bool allSelected = fileListBox.SelectedItems.Count == fileListBox.Items.Count;

            if (allSelected)
            {
                fileListBox.SelectedItems.Clear();
            }
            else
            {
                fileListBox.SelectAll();
            }
        }
    }

    // Recycle bin helper class
    public static class RecycleBinHelper
    {
        // Native methods for interacting with the recycle bin
        private static class NativeMethods
        {
            [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
            public static extern int SHEmptyRecycleBin(IntPtr hwnd, string pszRootPath, RecycleFlags dwFlags);
        }

        // Flags for the SHEmptyRecycleBin function
        [Flags]
        enum RecycleFlags : int
        {
            SHERB_NOCONFIRMATION = 0x00000001, // No confirmation dialog
            SHERB_NOPROGRESSUI = 0x00000002, // No progress dialog
            SHERB_NOSOUND = 0x00000004 // No sound on deletion
        }

        // Empty the recycle bin
        public static void EmptyRecycleBin()
        {
            NativeMethods.SHEmptyRecycleBin(IntPtr.Zero, null, RecycleFlags.SHERB_NOCONFIRMATION |
                RecycleFlags.SHERB_NOPROGRESSUI | RecycleFlags.SHERB_NOSOUND);
        }
    }
}
