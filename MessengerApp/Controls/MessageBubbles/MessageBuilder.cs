using MessengerApp.Connection.Schemas;
using System.Collections.Generic;
using System.Windows.Media;

namespace MessengerApp.Controls.MessageBubbles
{
    public class MessageBuilder
    {
        private readonly List<MessageBubble> messageCollection = new();

        public MessageDisplayMode DisplayMode = MessageDisplayMode.TwoSide;

        public MessageBuilder()
        {

        }

        public void Delete(MessageBubble bubble) => messageCollection.Remove(bubble);

        public MessageBubble CreateBubble(Message messageSchema)
        {
            var bubble = new MessageBubble(messageSchema);
            messageCollection.Add(bubble);

            bubble.BubbleBorder.Background = bubble.SendByUser ?
                Application.Current.Resources["SecondaryBitDarkerBrush"] as SolidColorBrush :
                Application.Current.Resources["SecondaryBitLighterBrush"] as SolidColorBrush;

            bubble.TimeBlock.Text = messageSchema.SendTime.ToDisplayableString();

            switch (messageSchema.Type)
            {
                case MessageType.Text: SetTextContent(bubble, messageSchema.Data); break;
                default: break;
            }

            switch (DisplayMode)
            {
                case MessageDisplayMode.TwoSide:
                    if (bubble.SendByUser)
                    {
                        bubble.BubbleBorder.CornerRadius = new CornerRadius(15, 15, 0, 15);
                        bubble.HorizontalAlignment = HorizontalAlignment.Right;
                    }
                    else
                    {
                        bubble.BubbleBorder.CornerRadius = new CornerRadius(0, 15, 15, 15);
                        bubble.HorizontalAlignment = HorizontalAlignment.Left;
                    }
                    break;
            }

            return bubble;
        }

        public static void SetTextContent(MessageBubble bubble, string text)
        {
            var textContent = new TextContent();
            textContent.TextMessageBlock.Text = text;
            bubble.MessageContent.Content = textContent;
        }

        public enum MessageDisplayMode : byte
        {
            TwoSide,
        }
    }
}
