using System;

namespace MessengerApp.Controls.UserProfileControls
{
    /// <summary>
    /// Логика взаимодействия для UsernameChangePanel.xaml
    /// </summary>
#nullable enable
    public partial class ProfileInfoChangePanel : UserControl
    {
        public event Action? ChangeComplete;

        public ProfileInfoChangePanel(ProfileInfo type)
        {
            InitializeComponent();

            switch (type)
            {
                case ProfileInfo.Username: SetForUsername(); break;
                case ProfileInfo.Password: SetForPassword(); break;
                case ProfileInfo.Mail: SetForMail(); break;
                default: throw new ArgumentException(nameof(type));
            }
        }

        public enum ProfileInfo
        {
            Username,
            Mail,
            Password
        }

        private void SetForUsername()
        {
            SecondFieldGrid.Visibility = Visibility.Collapsed;
            FirstFieldBlock.Text = Application.Current.Resources["lcUsername"] as string;
            TakenBlock.Text = Application.Current.Resources["lcUsernameTaken"] as string;
            ChangeButton.Click += ChangeUsername;
        }

        private void SetForMail()
        {
            SecondFieldGrid.Visibility = Visibility.Collapsed;
            FirstFieldBlock.Text = Application.Current.Resources["lcMail"] as string;
            TakenBlock.Text = Application.Current.Resources["lcMailTaken"] as string;
            ChangeButton.Click += ChangeMail;
        }

        private void SetForPassword()
        {
            SecondFieldGrid.Visibility = Visibility.Visible;
            FirstFieldBlock.Text = Application.Current.Resources["lcOldPassword"] as string;
            SecondFieldBlock.Text = Application.Current.Resources["lcNewPassword"] as string;
            TakenBlock.Text = Application.Current.Resources["lcWrongPassword"] as string;
            ChangeButton.Click += ChangePassword;
        }

        private async void ChangeUsername(object sender, RoutedEventArgs args)
        {
            TakenBlock.Visibility = Visibility.Hidden;

            var response = await Connections.Http.ChangeUsernameAsync(new(FirstFieldBox.Text));
            if (response.Success) ChangeComplete?.Invoke();
            else if (response.ErrorMessage == "UsernameTaken") TakenBlock.Visibility = Visibility.Visible;
            else MessageBox.Show(response.ErrorMessage);
        }

        private async void ChangeMail(object sender, RoutedEventArgs args)
        {
            TakenBlock.Visibility = Visibility.Hidden;

            var response = await Connections.Http.ChangeMailAsync(new(FirstFieldBox.Text));
            if (response.Success) ChangeComplete?.Invoke();
            else if (response.ErrorMessage == "MailTaken") TakenBlock.Visibility = Visibility.Visible;
            else MessageBox.Show(response.ErrorMessage);
        }

        private async void ChangePassword(object sender, RoutedEventArgs args)
        {
            TakenBlock.Visibility = Visibility.Hidden;

            var response = await Connections.Http.ChangePasswordAsync(
                new(FirstFieldBox.Text, FirstFieldBox.Text));

            if (response.Success) ChangeComplete?.Invoke();
            else if (response.ErrorMessage == "WrongPassword") TakenBlock.Visibility = Visibility.Visible;
            else MessageBox.Show(response.ErrorMessage);
        }
    }
}
