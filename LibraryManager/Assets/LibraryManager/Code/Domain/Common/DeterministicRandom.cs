using System;

namespace LibraryManager.Domain.Common
{
    /// <summary>
    /// Generador pseudoaleatorio determinista (SplitMix64). Con la misma semilla
    /// produce siempre la misma secuencia y su estado se puede guardar y restaurar.
    /// </summary>
    public sealed class DeterministicRandom
    {
        private ulong _state;

        public DeterministicRandom(ulong seed)
        {
            _state = seed;
        }

        /// <summary>Estado interno, para guardarlo en la partida.</summary>
        public ulong State => _state;

        public static DeterministicRandom FromState(ulong state)
        {
            return new DeterministicRandom(state);
        }

        public ulong NextULong()
        {
            unchecked
            {
                _state += 0x9E3779B97F4A7C15UL;
                ulong z = _state;
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return z ^ (z >> 31);
            }
        }

        /// <summary>Número real en [0, 1).</summary>
        public double NextDouble()
        {
            return (NextULong() >> 11) * (1.0 / (1UL << 53));
        }

        /// <summary>Entero en [minInclusive, maxExclusive).</summary>
        public int NextInt(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
            {
                throw new ArgumentOutOfRangeException(nameof(maxExclusive), "El máximo debe ser mayor que el mínimo.");
            }

            ulong range = (ulong)((long)maxExclusive - minInclusive);
            return (int)(minInclusive + (long)(NextULong() % range));
        }

        public bool Chance(double probability)
        {
            return NextDouble() < probability;
        }
    }
}
