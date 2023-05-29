using MessengerApp.Connection.Schemas;
using MessengerApp.Controls.MessageBubbles;
using System.Windows.Media;

namespace MessengerApp.Controls
{
    /// <summary>
    /// Логика взаимодействия для MessageBubble.xaml
    /// </summary>
    public partial class MessageBubble : UserControl
    {
        public Message Data { get; private set; }
        public readonly bool SendByUser;

        public MessageBubble(Message message)
        {
            InitializeComponent();
            Data = message;
            SendByUser = Data.SendBy == Connections.UserInfo.ID;

            Connections.Notifications.MessageUpdated += MessageUpdatedHandler;
        }

        private void MessageUpdatedHandler(object obj)
        {
            var message = obj as Message;
            if (message is null) return;

            if (message.ID != Data.ID) return;
            Data = message;

            switch (message.Type)
            {
                case MessageType.Text:
                    MessageBuilder.SetTextContent(this, message.Data);
                    break;
            }
        }

        public void TurnOnHighlight()
        {
            BubbleBorder.BorderBrush = Application.Current.Resources["TertiaryBitDarkerBrush"] as SolidColorBrush;
        }

        public void TurnOffHighlight()
        {
            BubbleBorder.BorderBrush = null;
        }
    }
}
