using System.Collections.Generic;
using System.Linq;

namespace SpeechToEmotion
{
    public static class MessageBuilder
    {
        public static string GetInitialEmotionScoreString(int latestCount) => $"Total: ?, Last {latestCount}: ?";

        public static string BuildEmotionScoreString(double totalScore, double latestScore, int latestCount) =>
            $"Total: {BuildScoreGauge(totalScore)}" + $", Last {latestCount}: {BuildScoreGauge(latestScore)}";

        public static string BuildRecognizedText(IEnumerable<Sentence> contents, string lastContent) =>
            string.Join("\r\n", contents.Select(s => s.Text).ToList().Append(lastContent));

        private static string BuildScoreGauge(double? score) =>
            (score >= 0.5 ? "Positive" : "Negative") + $" ({score:0.000})";
    }
}
