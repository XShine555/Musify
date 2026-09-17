namespace Musify.Domain.ValueObjects
{
    /// <summary>
    /// Encodes which genre pairs are mutually exclusive on a single track (e.g. a track cannot
    /// be tagged as both Classical and Metal at the same time).
    /// </summary>
    public static class GenreCompatibility
    {
        private static readonly HashSet<(Genre, Genre)> IncompatiblePairs = BuildIncompatiblePairs();

        private static HashSet<(Genre, Genre)> BuildIncompatiblePairs()
        {
            var pairs = new HashSet<(Genre, Genre)>();

            void Add(Genre a, Genre b)
            {
                pairs.Add((a, b));
                pairs.Add((b, a));
            }

            var intense = new[] { Genre.Metal, Genre.Punk, Genre.Dubstep, Genre.DrumAndBass };
            var mellow = new[] { Genre.Classical, Genre.Ambient, Genre.Lofi };

            foreach (var intenseGenre in intense)
                foreach (var mellowGenre in mellow)
                    Add(intenseGenre, mellowGenre);

            return pairs;
        }

        public static bool AreCompatible(Genre a, Genre b) =>
            a == b || !IncompatiblePairs.Contains((a, b));

        /// <summary>
        /// Returns every mutually exclusive genre pair found within the given set of tags.
        /// </summary>
        public static IReadOnlyCollection<(Genre First, Genre Second)> FindConflicts(IEnumerable<Genre> tags)
        {
            var distinctTags = tags.Distinct().ToList();
            var conflicts = new List<(Genre, Genre)>();

            for (var i = 0; i < distinctTags.Count; i++)
            {
                for (var j = i + 1; j < distinctTags.Count; j++)
                {
                    if (!AreCompatible(distinctTags[i], distinctTags[j]))
                        conflicts.Add((distinctTags[i], distinctTags[j]));
                }
            }

            return conflicts;
        }
    }
}
