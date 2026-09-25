using System;

namespace Ninefold.Core.Combat
{
    public sealed class UnitHealthDefinition
    {
        public string TeamId { get; }
        public int MaximumHealth { get; }
        public int Armor { get; }

        public UnitHealthDefinition(string teamId, int maximumHealth, int armor)
        {
            if (string.IsNullOrWhiteSpace(teamId)) throw new ArgumentException("Team ID required.", nameof(teamId));
            if (maximumHealth <= 0) throw new ArgumentOutOfRangeException(nameof(maximumHealth));
            if (armor < 0) throw new ArgumentOutOfRangeException(nameof(armor));
            TeamId = teamId;
            MaximumHealth = maximumHealth;
            Armor = armor;
        }
    }

    public sealed class HealthStateView
    {
        public string UnitId { get; }
        public string TeamId { get; }
        public int MaximumHealth { get; }
        public int CurrentHealth { get; }
        public int Armor { get; }
        public bool IsDefeated => CurrentHealth == 0;

        internal HealthStateView(string unitId, UnitHealthDefinition definition, int currentHealth)
        {
            UnitId = unitId;
            TeamId = definition.TeamId;
            MaximumHealth = definition.MaximumHealth;
            CurrentHealth = currentHealth;
            Armor = definition.Armor;
        }
    }
}
