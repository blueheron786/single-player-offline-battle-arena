using System.Collections.Generic;
using System.Linq;

namespace SobaRL.Game
{
    public class MessageLog
    {
        private readonly Queue<string> _messages = new();
        private readonly int _maxMessages;
        public MessageLog(int maxMessages = 5)
        {
            _maxMessages = maxMessages;
        }
        public void Add(string message)
        {
            _messages.Enqueue(message);
            while (_messages.Count > _maxMessages)
                _messages.Dequeue();
        }
        public IEnumerable<string> GetRecentMessages(int count)
        {
            return _messages.Reverse().Take(count);
        }
        public int Count => _messages.Count;
    }
}
