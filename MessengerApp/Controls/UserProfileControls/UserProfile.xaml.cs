using MessengerApp.Connection.Schemas;
using Microsoft.Win32;
using System.IO;
using System.Windows.Media.Animation;
using static MessengerApp.Controls.UserProfileControls.ProfileInfoChangePanel;

namespace MessengerApp.Controls.UserProfileControls
{
    /// <summary>
    /// Логика взаимодействия для UserProfile.xaml
    /// </summary>
    public partial class UserProfile : UserControl
    {
        private bool overlayVisible = false;

        public UserProfile()
        {
            InitializeComponent();

            UsernameBlock.Text = Connections.UserInfo.Username;
            UsernameButtonBlock.Text = Connections.UserInfo.Username;
            MailButtonBlock.Text = Connections.UserInfo.Mail;

            Connections.Notifications.ProfileUpdated += InfoChangedHandler;
            Connections.Notifications.UserImageUpdated += ImageUpdateHandler;

            LoadImage();
        }

        private void OverlayGrid_Click(object sender, RoutedEventArgs e)
        {
            HideOverlay();
        }

        public void ShowOverlay()
        {
            if (!overlayVisible)
            {
                overlayVisible = true;
                var storyboard = (Storyboard)Resources["ShowOverlay"];
                storyboard.Begin();
            }
        }

        public void HideOverlay()
        {
            if (overlayVisible)
            {
                overlayVisible = false;
                var storyboard = (Storyboard)Resources["HideOverlay"];
                storyboard.Begin();
                OverlayPlaceholder.Content = null;
            }
        }

        private void ImageUpdateHandler(object schema)
        {
            var info = schema as ImageUpdateSchema;
            if (info is null) return;

            if (Connections.UserInfo.ID == info.ID) LoadImage();
        }

        private void InfoChangedHandler(object schema)
        {
            var info = schema as UserInfo;
            if (info is null) return;

            UsernameBlock.Text = info.Username;
            UsernameButtonBlock.Text = info.Username;
            MailButtonBlock.Text = info.Mail;
        }

        private void UsernameButton_Click(object sender, RoutedEventArgs e)
        {
            var panel = new ProfileInfoChangePanel(ProfileInfo.Username);
            panel.ChangeComplete += () => HideOverlay();
            OverlayPlaceholder.Content = panel;
            ShowOverlay();
        }

        private void MailButton_Click(object sender, RoutedEventArgs e)
        {
            var panel = new ProfileInfoChangePanel(ProfileInfo.Mail);
            panel.ChangeComplete += () => HideOverlay();
            OverlayPlaceholder.Content = panel;
            ShowOverlay();
        }

        private void PasswordButton_Click(object sender, RoutedEventArgs e)
        {
            var panel = new ProfileInfoChangePanel(ProfileInfo.Password);
            panel.ChangeComplete += () => HideOverlay();
            OverlayPlaceholder.Content = panel;
            ShowOverlay();
        }

        private async void Image_Click(object sender, RoutedEventArgs e)
        {
            var fileDialog = new OpenFileDialog();
            fileDialog.Filter = "Image| *.BMP;*.JPG; *.JPEG; *.PNG";
            if (fileDialog.ShowDialog() == true)
            {
                using var stream = File.Open(fileDialog.FileName, FileMode.Open);
                var resp = await Connections.Http.UpdateProfileImage(stream);
                if (!resp.Success) MessageBox.Show(resp.ErrorMessage);
            }
        }

        private async void LoadImage()
        {
            var imageResponse = await Connections.Http.GetUserProfileImageAsync(new(Connections.UserInfo.ID));
            if (imageResponse.Success)
            {
                var image = imageResponse.Data!;
                Image.Source = image;
            }
        }

        private async void Image_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var fileDialog = new OpenFileDialog();
            fileDialog.Filter = "Image| *.BMP;*.JPG; *.JPEG; *.PNG";
            if (fileDialog.ShowDialog() == true)
            {
                using var stream = File.Open(fileDialog.FileName, FileMode.Open);
                var resp = await Connections.Http.UpdateProfileImage(stream);
                if (!resp.Success) MessageBox.Show(resp.ErrorMessage);
            }
        }
    }
}
