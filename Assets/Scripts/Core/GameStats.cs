namespace OneButtonSubmission.Core
{
    /// Session-wide tallies that outlive level rebuilds. Reset when a fresh
    /// run starts, never on retries — a banked kill stays banked.
    public static class GameStats
    {
        public static int Kills;

        public static void Reset() => Kills = 0;
    }
}
