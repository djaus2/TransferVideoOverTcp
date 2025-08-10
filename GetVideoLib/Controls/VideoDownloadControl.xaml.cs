using GetVideoLib.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;

namespace GetVideoLib.Controls
{
    public partial class VideoDownloadControl : System.Windows.Controls.UserControl
    {
        public VideoDownloadViewModel ViewModel { get; private set; }

        public VideoDownloadControl()
        {
            InitializeComponent();
        }

        public void Initialize(VideoDownloadViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = ViewModel;

            // Wire up the Browse button click event
            BrowseButton.Click += (s, e) => BrowseFolder();
        }

        private void BrowseFolder()
        {
            var dialog = new FolderBrowserDialog
            {
                Description = "Select Download Folder",
                UseDescriptionForTitle = true,
                SelectedPath = ViewModel.DownloadFolder
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                ViewModel.DownloadFolder = dialog.SelectedPath;
                ViewModel.RefreshDownloadedFilesCommand.Execute(null);
            }
        }
    }
}
