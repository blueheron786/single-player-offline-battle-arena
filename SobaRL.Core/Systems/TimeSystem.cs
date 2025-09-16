using SobaRL.Core.Models;

namespace SobaRL.Core.Systems
{
    public class TimeSystem
    {
        private int _currentTime = 0;
        private readonly Dictionary<Unit, int> _unitActionTimes = new Dictionary<Unit, int>();
        
        public int CurrentTime => _currentTime;
        
        public void RegisterUnit(Unit unit)
        {
            if (!_unitActionTimes.ContainsKey(unit))
            {
                _unitActionTimes[unit] = _currentTime;
            }
        }
        
        public void UnregisterUnit(Unit unit)
        {
            _unitActionTimes.Remove(unit);
        }
        
        public bool CanUnitAct(Unit unit)
        {
            if (!_unitActionTimes.ContainsKey(unit))
                return false;
                
            return _unitActionTimes[unit] <= _currentTime;
        }
        
        public void ConsumeUnitAction(Unit unit)
        {
            if (!_unitActionTimes.ContainsKey(unit))
                return;
                
            // Calculate when this unit can act next based on speed
            // Higher speed = more frequent actions
            // Ensure speed is at least 1 to prevent divide by zero
            int effectiveSpeed = Math.Max(1, unit.Speed);
            int actionDelay = Math.Max(1, 100 / effectiveSpeed);
            _unitActionTimes[unit] = _currentTime + actionDelay;
        }
        
        public void AdvanceTime()
        {
            _currentTime++;
        }
        
        public List<Unit> GetUnitsReadyToAct()
        {
            return _unitActionTimes
                .Where(kvp => kvp.Value <= _currentTime)
                .Select(kvp => kvp.Key)
                .Where(unit => unit.IsAlive)
                .OrderByDescending(unit => unit.Speed) // Faster units act first
                .ToList();
        }
        
        public int GetTimeUntilUnitCanAct(Unit unit)
        {
            if (!_unitActionTimes.ContainsKey(unit))
                return 0;
                
            return Math.Max(0, _unitActionTimes[unit] - _currentTime);
        }
        
        public void Reset()
        {
            _currentTime = 0;
            _unitActionTimes.Clear();
        }
    }
}