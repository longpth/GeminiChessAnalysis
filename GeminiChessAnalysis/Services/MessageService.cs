using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GeminiChessAnalysis.Services
{
    public interface IMessageService
    {
        void Subscribe(Action<string> onMessageReceived);
        void Unsubscribe(Action<string> onMessageReceived);
        void NotifySubscribers(string msgText);
    }

    public static class MessageKeys
    {
        public const string ScrollToFirst = "ScrollToFirst";
        public const string PGNFromChessCom = "Chess.com";
    }

    public class MessageService : IMessageService
    {
        private readonly List<Action<string>> _subscribers = new List<Action<string>>();
        private string _pendingmsgText;

        // Singleton instance
        private static readonly Lazy<MessageService> _instance = new Lazy<MessageService>(() => new MessageService());
        public static MessageService Instance => _instance.Value;

        private MessageService() { }

        public void Subscribe(Action<string> onMessageReceived)
        {
            if (!_subscribers.Contains(onMessageReceived))
            {
                _subscribers.Add(onMessageReceived);
                // Immediately notify this subscriber if there's pending data
                if (_pendingmsgText != null)
                {
                    onMessageReceived(_pendingmsgText);
                    _pendingmsgText = null; // Clear after notifying
                }
            }
        }

        public void Unsubscribe(Action<string> onMessageReceived)
        {
            if (_subscribers.Contains(onMessageReceived))
            {
                _subscribers.Remove(onMessageReceived);
            }
        }

        public void NotifySubscribers(string msgText)
        {
            if (_subscribers.Any())
            {
                foreach (var subscriber in _subscribers)
                {
                    subscriber(msgText);
                }
                _pendingmsgText = null; // Clear after notifying
            }
            else
            {
                // Hold onto the data if no subscribers are present
                _pendingmsgText = msgText;
            }
        }
    }
}
