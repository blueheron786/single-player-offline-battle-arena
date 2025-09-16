using SadRogue.Primitives;
using SobaRL.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SobaRL.Game
{
    public class AttackAnimationManager
    {
        private readonly Dictionary<Position, AttackAnimation> _animations = new();
        public IReadOnlyDictionary<Position, AttackAnimation> Animations => _animations;

        public void AddAnimation(Position pos, char character, Color color, int durationMs = 100)
        {
            _animations[pos] = new AttackAnimation(character, color, durationMs);
        }

        public void RemoveExpired()
        {
            var expired = _animations.Where(kvp => kvp.Value.IsExpired).Select(kvp => kvp.Key).ToList();
            foreach (var pos in expired)
                _animations.Remove(pos);
        }

        public bool HasExpiredAnimations => _animations.Any(kvp => kvp.Value.IsExpired);

        public bool Contains(Position pos) => _animations.ContainsKey(pos);
        public AttackAnimation? Get(Position pos) => _animations.TryGetValue(pos, out var anim) ? anim : null;
    }

    public class AttackAnimation
    {
        public char Character { get; }
        public Color Color { get; }
        public DateTime StartTime { get; }
        public int DurationMs { get; }
        public AttackAnimation(char character, Color color, int durationMs = 150)
        {
            Character = character;
            Color = color;
            StartTime = DateTime.Now;
            DurationMs = durationMs;
        }
        public bool IsExpired => DateTime.Now.Subtract(StartTime).TotalMilliseconds > DurationMs;
    }
}
