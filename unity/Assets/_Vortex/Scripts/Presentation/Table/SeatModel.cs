using System;
using System.Collections.Generic;
using System.Linq;
using Vortex.Core.Content;
using Vortex.Core.Projection;

namespace Vortex.Client.Presentation
{
    /// <summary>What the table shows of one seat. Built from the public view, then patched event by event.</summary>
    public sealed class SeatModel
    {
        private readonly List<TechColor> _technologies;

        private SeatModel(PlayerView player)
        {
            Seat = player.Seat;
            Name = player.Name;
            Hp = player.Hp;
            Shield = player.Shield;
            Overcharge = player.Overcharge;
            Eliminated = player.Eliminated;
            AttackCard = player.AttackSlot;
            DefenseCard = player.DefenseSlot;
            Statuses = player.Statuses;
            _technologies = player.Technologies.Distinct().ToList();
        }

        /// <summary>Seat number (0-based, table order).</summary>
        public int Seat { get; }

        /// <summary>Player name, as typed: display it as plain text, never as rich text.</summary>
        public string Name { get; }

        /// <summary>Hit points.</summary>
        public int Hp { get; internal set; }

        /// <summary>Shield value.</summary>
        public int Shield { get; internal set; }

        /// <summary>Overcharge tokens.</summary>
        public int Overcharge { get; internal set; }

        /// <summary>True once eliminated.</summary>
        public bool Eliminated { get; internal set; }

        /// <summary>Equipped attack modifier, or null.</summary>
        public CardView? AttackCard { get; }

        /// <summary>Equipped defense modifier, or null.</summary>
        public CardView? DefenseCard { get; }

        /// <summary>Temporary effects on the seat.</summary>
        public IReadOnlyList<StatusView> Statuses { get; }

        /// <summary>Technologies obtained, each once, in the order obtained.</summary>
        public IReadOnlyList<TechColor> Technologies => _technologies;

        /// <summary>Builds the model of a seat from the public view.</summary>
        public static SeatModel From(PlayerView player) => new SeatModel(player ?? throw new ArgumentNullException(nameof(player)));

        internal void AddTechnology(TechColor color)
        {
            if (!_technologies.Contains(color))
            {
                _technologies.Add(color);
            }
        }
    }
}
