namespace Musify.Domain.ValueObjects
{

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
