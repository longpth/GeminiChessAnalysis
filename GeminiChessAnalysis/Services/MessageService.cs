using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GeminiChessAnalysis.Services
{
    public interface IMessageService
    {
        void Subscribe(Action<string> onPgnReceived);
        void Unsubscribe(Action<string> onPgnReceived);
        void NotifySubscribers(string pgnText);
    }
    public class MessageService : IMessageService
    {
        private readonly List<Action<string>> _subscribers = new List<Action<string>>();
        private string _pendingPgnText;

        // Singleton instance
        private static readonly Lazy<MessageService> _instance = new Lazy<MessageService>(() => new MessageService());
        public static MessageService Instance => _instance.Value;

        private MessageService() { }

        public void Subscribe(Action<string> onPgnReceived)
        {
            if (!_subscribers.Contains(onPgnReceived))
            {
                _subscribers.Add(onPgnReceived);
                // Immediately notify this subscriber if there's pending data
                if (_pendingPgnText != null)
                {
                    onPgnReceived(_pendingPgnText);
                    _pendingPgnText = null; // Clear after notifying
                }
            }
        }

        public void Unsubscribe(Action<string> onPgnReceived)
        {
            if (_subscribers.Contains(onPgnReceived))
            {
                _subscribers.Remove(onPgnReceived);
            }
        }

        public void NotifySubscribers(string pgnText)
        {
            if (_subscribers.Any())
            {
                foreach (var subscriber in _subscribers)
                {
                    subscriber(pgnText);
                }
                _pendingPgnText = null; // Clear after notifying
            }
            else
            {
                // Hold onto the data if no subscribers are present
                _pendingPgnText = pgnText;
            }
        }
    }
}
