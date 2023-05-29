using MessengerApp.Connection.Schemas;

namespace MessengerApp.Controls
{
    /// <summary>
    /// Логика взаимодействия для ChatPanel.xaml
    /// </summary>
    public partial class ChatPanel : UserControl
    {
        public readonly Chat Schema;

        public ChatPanel(Chat schema)
        {
            InitializeComponent();
            Indicator.Visibility = Visibility.Collapsed;
            Schema = schema;

            ChatNameBlock.Text = Schema.Name;

            var token = Connections.Notifications.HoldNotifications();
            GetLastMessage();
            GetImage();
            Connections.Notifications.MessageRecieved += MessageRecievedHandler;
            Connections.Notifications.ChatImageUpdated += ChatImageUpdatedHandler;
            token.Release();
        }

        private void UserControl_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Overlay.Visibility = Visibility.Visible;
        }

        private void UserControl_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Overlay.Visibility = Visibility.Collapsed;
        }

        private async void GetImage()
        {
            var imageResponse = await Connections.Http.GetChatImageAsync(new(Schema.ID));
            if (imageResponse.Success)
            {
                var image = imageResponse.Data!;
                Image.Source = image;
            }
        }

        private void ChatImageUpdatedHandler(object obj)
        {
            var info = obj as ChatUpdateSchema;
            if (info is null) return;
            if (Schema.ID == info.ID) GetImage();
        }

        private void MessageRecievedHandler(object obj)
        {
            var message = obj as Message;
            if (message is null) return;
            if (Schema.ID == message.ChatID) SetMessage(message);
        }

        private async void GetLastMessage()
        {
            var schema = new GetMessagesSchema(Schema.ID, 1, null);
            var response = await Connections.Http.GetMessagesDescendingAsync(schema);
            if (response.Success && response.Data!.Length > 0)
                SetMessage(response.Data![0]);
        }

        public void SetMessage(Message message)
        {
            MessageBlock.Text = message.Type switch
            {
                MessageType.Text => message.Data,
                _ => string.Empty,
            };
            TimeBlock.Text = message.SendTime.ToDisplayableString();
        }
    }
}
