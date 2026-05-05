using System;
using System.Windows;
using EZPos.Models.Domain;

namespace EZPos.UI.Dialogs
{
    public partial class UpdateAvailableDialog : Window
    {
        public UpdateManifest? Manifest { get; private set; }
        public bool UserClickedUpdate { get; private set; }

        public UpdateAvailableDialog(UpdateManifest manifest, string currentVersion)
        {
            InitializeComponent();
            
            Manifest = manifest;
            UserClickedUpdate = false;

            // Populate version info
            CurrentVersionLabel.Text = currentVersion;
            AvailableVersionLabel.Text = manifest.Version ?? "Unknown";

            // Populate release notes
            if (!string.IsNullOrEmpty(manifest.ReleaseNotes))
            {
                ReleaseNotesTextBlock.Text = manifest.ReleaseNotes;
            }
            else
            {
                ReleaseNotesTextBlock.Text = "No release notes available.";
            }

            // Show mandatory warning if needed
            if (manifest.Mandatory)
            {
                MandatoryWarning.Visibility = Visibility.Visible;
                SkipButton.IsEnabled = false;
                SkipButton.Opacity = 0.5;
            }
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            UserClickedUpdate = true;
            DialogResult = true;
            Close();
        }

        private void SkipButton_Click(object sender, RoutedEventArgs e)
        {
            UserClickedUpdate = false;
            DialogResult = false;
            Close();
        }
    }
}
