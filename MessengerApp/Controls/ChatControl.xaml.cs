using MessengerApp.Connection.Schemas;
using MessengerApp.Controls.MessageBubbles;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace MessengerApp.Controls
{
    /// <summary>
    /// Логика взаимодействия для ChatControl.xaml
    /// </summary>
#nullable enable
    public partial class ChatControl : UserControl
    {
        private const int MessagesAtATime = 5;

        private ObservableCollection<MessageBubble> messagesCollection = new();
        private Dictionary<string, MessageBubble> messagesDictionary = new();
        private MessageBuilder messageBuilder = new();
        private Chat Data;

        private string? lastID;
        private MessageBubble? seletedBubble;
        private Action messageAction;

        private bool editing;
        private bool firstMessagesLoaded;
        private bool gettingMessages;

        private bool toolPanelShown = false;
        private bool editPanelShown = false;

        public ChatControl(Chat data)
        {
            InitializeComponent();

            Data = data;
            messageAction = SendTextMessage;

            ChatNameBlock.Text = Data.Name;
            MessageList.ItemsSource = messagesCollection;

            var token = Connections.Notifications.HoldNotifications();
            GetFirstMessages();
            Connections.Notifications.MessageRecieved += MessageRecievedHandler;
            Connections.Notifications.MessageDeleted += MessageDeletedHandler;
            token.Release();
        }

        private void MessageDeletedHandler(object obj)
        {
            var message = obj as Message;
            if (message is null) return;

            if (message.ChatID != Data.ID) return;

            var bubble = messagesDictionary[message.ID];
            messageBuilder.Delete(bubble);
            messagesCollection.Remove(bubble);
            bubble.MouseRightButtonDown -= MessageBubble_MouseRightButtonDown;
        }

        private void MessageRecievedHandler(object obj)
        {
            var message = obj as Message;
            if (message is null) return;

            if (message.ChatID != Data.ID) return;

            AddMessageToTheBottom(message);
        }

        private async Task<IEnumerable<Message>> GetMessages(int count = MessagesAtATime)
        {
            var schema = new GetMessagesSchema(Data.ID, count, lastID);
            var response = await Connections.Http.GetMessagesDescendingAsync(schema);
            if (response.Success)
            {
                if (response.Data!.Length > 0)
                    lastID = response.Data![^1].ID;
                return response.Data!.OrderByDescending(x => x.SendTime);
            }
            return new Message[0];
        }

        private async void GetFirstMessages()
        {
            IEnumerable<Message> messages;
            do
            {
                messages = await GetMessages();
                foreach (var message in messages) AddMessageToTheTop(message);
                MessageScroll.UpdateLayout();
                MessageScroll.ScrollToBottom();
            }
            while (MessageScroll.VerticalOffset <= 25 && messages.Any());
            firstMessagesLoaded = true;
        }

        private async void GetMoreMessages()
        {
            if (!firstMessagesLoaded) return;
            if (gettingMessages) return;

            gettingMessages = true;
            var messages = await GetMessages();
            if (messages.Count() is 0) return;

            var offset = MessageScroll.VerticalOffset;
            var oldHeight = MessageScroll.ExtentHeight;

            foreach (var message in messages) AddMessageToTheTop(message);

            MessageScroll.ScrollToVerticalOffset(MessageScroll.ExtentHeight - oldHeight);
            gettingMessages = false;
        }

        private void AddMessageToTheBottom(Message schema)
        {
            var scroller = MessageScroll;
            bool autoScroll = scroller.VerticalOffset + scroller.ViewportHeight == scroller.ExtentHeight;

            messagesCollection.Add(CreateMessage(schema));
            if (autoScroll) scroller.ScrollToBottom();
        }

        private void AddMessageToTheTop(Message schema)
        {
            messagesCollection.Insert(0, CreateMessage(schema));
        }

        private MessageBubble CreateMessage(Message schema)
        {
            var bubble = messageBuilder.CreateBubble(schema);
            messagesDictionary.Add(schema.ID, bubble);
            if (bubble.SendByUser)
                bubble.MouseRightButtonDown += MessageBubble_MouseRightButtonDown;

            return bubble;
        }

        private void CancelEditing()
        {
            if (!editing) return;
            editing = false;

            UnselectMessage();
            HideEditPanel();
            messageAction = SendTextMessage;
            MessageTextBox.Text = string.Empty;
        }

        private void UnselectMessage()
        {
            if (seletedBubble is null) return;
            seletedBubble.TurnOffHighlight();
            seletedBubble = null;
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            messageAction.Invoke();
        }

        private async void DeleteMessageButton_Click(object sender, RoutedEventArgs e)
        {
            if (seletedBubble is null) return;

            var response = await Connections.Http.DeleteMessage(new(seletedBubble.Data.ID));
            if (!response.Success) MessageBox.Show(response.ErrorMessage);

            HideToolPanel();
            UnselectMessage();
        }

        private void EditMessageButton_Click(object sender, RoutedEventArgs e)
        {
            switch (seletedBubble?.Data.Type)
            {
                case MessageType.Text:
                    editing = true;
                    HideToolPanel();
                    ShowEditPanel();

                    if (seletedBubble.MessageContent.Content is not TextContent content)
                        throw new Exception("Content wasn't text");

                    MessageTextBox.Text = content.TextMessageBlock.Text;
                    messageAction = EditMessage;
                    break;
            }
        }

        private string? GetMessageBoxText()
        {
            var text = MessageTextBox.Text;
            text = text?.TrimEnd(new char[] { ' ', '\r', '\n' });
            return text;
        }

        private async void SendTextMessage()
        {
            var text = GetMessageBoxText();
            if (string.IsNullOrEmpty(text)) return;

            var schema = new AddTextMessageSchema(Data.ID, text, DateTime.UtcNow);
            var response = await Connections.Http.AddTextMessageAsync(schema);

            if (response.Success) MessageTextBox.Text = string.Empty;
            else { } //AddErrorParse
        }

        private async void EditMessage()
        {
            if (seletedBubble is null)
            {
                CancelEditing();
                return;
            }

            var text = GetMessageBoxText();
            if (string.IsNullOrEmpty(text)) return;

            var schema = new EditTextMessageSchema(seletedBubble.Data.ID, text);
            var response = await Connections.Http.EditTextMessage(schema);

            if (!response.Success) MessageBox.Show(response.ErrorMessage);

            CancelEditing();
        }

        private void MessageBubble_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var bubble = sender as MessageBubble;
            if (bubble is null) return;

            CancelEditing();
            UnselectMessage();

            ShowToolPanel();
            seletedBubble = bubble;
            seletedBubble.TurnOnHighlight();
        }

        private void MessageList_MouseLeftButtonClick(object sender, MouseButtonEventArgs e)
        {
            CancelEditing();
            UnselectMessage();
            HideToolPanel();
        }

        private void MessageBox_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    if (!Keyboard.IsKeyDown(Key.LeftShift)) messageAction.Invoke();
                    else
                    {
                        MessageTextBox.Text += Environment.NewLine;
                        MessageTextBox.CaretIndex = MessageTextBox.Text.Length - 1;
                    }
                    break;
            }
        }

        private void MessageScroll_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (MessageScroll.VerticalOffset <= 0) return;

            if (MessageScroll.ExtentHeight > 50 && MessageScroll.VerticalOffset < 50) GetMoreMessages();
            if (MessageScroll.ExtentHeight < 50)
            {
                var gap = MessageScroll.ExtentHeight * 0.9;
                if (MessageScroll.VerticalOffset < gap) GetMoreMessages();
            }
        }

        public void ShowToolPanel()
        {
            if (toolPanelShown) return;

            toolPanelShown = true;
            var storyboard = (Storyboard)Resources["ShowToolPanel"];
            storyboard.Begin();
        }

        public void HideToolPanel()
        {
            if (!toolPanelShown) return;

            toolPanelShown = false;
            var storyboard = (Storyboard)Resources["HideToolPanel"];
            storyboard.Begin();
        }

        public void ShowEditPanel()
        {
            if (editPanelShown) return;

            editPanelShown = true;
            var storyboard = (Storyboard)Resources["ShowEditPanel"];
            storyboard.Begin();
        }

        public void HideEditPanel()
        {
            if (!editPanelShown) return;

            editPanelShown = false;
            var storyboard = (Storyboard)Resources["HideEditPanel"];
            storyboard.Begin();
        }
    }
}
