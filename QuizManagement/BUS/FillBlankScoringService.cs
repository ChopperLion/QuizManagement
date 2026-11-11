using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace QuizManagement.BUS
{
    internal static class FillBlankScoringService
    {
        private static readonly HashSet<string> StopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "la","lam","va","voi","cua","cho","mot","nhung","cac","khi","neu","thi","tren","duoi","trong","ngoai",
            "tai","do","day","kia","nay","ay","hon","it","rat","co","khong","duoc","bi","tu","den","vua","se",
            "user","mode","type","driver","kieu","loai","phan","cau","dap","an","hien","dai","cu","moi","tot",
            "better","more","less","with","from","into","than","the","a","an","and","or","to","of","for","in","on"
        };

        internal sealed class FillBlankEvaluation
        {
            public string NormalizedAnswer { get; set; }
            public string NormalizedCorrect { get; set; }
            public decimal KeywordRatio { get; set; }
            public decimal SimilarityRatio { get; set; }
            public decimal ContainmentRatio { get; set; }
            public decimal ScoreRatio { get; set; }
            public bool IsCorrect { get; set; }
            public string MatchedKeywords { get; set; }
        }

        public static FillBlankEvaluation Evaluate(string answer, string correctAnswer)
        {
            string normalizedAnswer = Normalize(answer);
            string normalizedCorrect = Normalize(correctAnswer);
            var evaluation = new FillBlankEvaluation
            {
                NormalizedAnswer = normalizedAnswer,
                NormalizedCorrect = normalizedCorrect,
                KeywordRatio = 0M,
                SimilarityRatio = 0M,
                ContainmentRatio = 0M,
                ScoreRatio = 0M,
                IsCorrect = false,
                MatchedKeywords = ""
            };

            if (string.IsNullOrWhiteSpace(normalizedAnswer) || string.IsNullOrWhiteSpace(normalizedCorrect))
                return evaluation;

            if (string.Equals(normalizedAnswer, normalizedCorrect, StringComparison.OrdinalIgnoreCase))
            {
                evaluation.KeywordRatio = 1M;
                evaluation.SimilarityRatio = 1M;
                evaluation.ContainmentRatio = 1M;
                evaluation.ScoreRatio = 1M;
                evaluation.IsCorrect = true;
                evaluation.MatchedKeywords = normalizedCorrect;
                return evaluation;
            }

            List<string> correctTokens = Tokenize(normalizedCorrect);
            List<string> answerTokens = Tokenize(normalizedAnswer);
            List<string> matched = MatchKeywords(correctTokens, answerTokens);
            if (correctTokens.Count > 0)
                evaluation.KeywordRatio = Math.Round((decimal)matched.Count / correctTokens.Count, 2, MidpointRounding.AwayFromZero);
            evaluation.MatchedKeywords = string.Join(", ", matched);

            evaluation.SimilarityRatio = Math.Round(ComputeSimilarity(normalizedAnswer, normalizedCorrect), 2, MidpointRounding.AwayFromZero);
            evaluation.ContainmentRatio = Math.Round(ComputeContainmentRatio(answerTokens, correctTokens), 2, MidpointRounding.AwayFromZero);

            decimal strongest = Math.Max(evaluation.KeywordRatio, Math.Max(evaluation.SimilarityRatio, evaluation.ContainmentRatio));
            bool strongKeywordMatch = evaluation.KeywordRatio >= 0.85M && evaluation.ContainmentRatio >= 0.65M;
            bool nearExactMeaning = evaluation.SimilarityRatio >= 0.9M || evaluation.ContainmentRatio >= 0.92M;
            if (evaluation.KeywordRatio >= 1M || nearExactMeaning || strongKeywordMatch)
            {
                evaluation.ScoreRatio = 1M;
                evaluation.IsCorrect = true;
            }
            else if (evaluation.KeywordRatio >= 0.7M || evaluation.ContainmentRatio >= 0.75M)
            {
                evaluation.ScoreRatio = Math.Max(0.8M, strongest);
            }
            else if (strongest >= 0.55M)
            {
                evaluation.ScoreRatio = strongest;
            }

            return evaluation;
        }

        public static string Normalize(string value)
        {
            string text = (value ?? "").Trim().ToLowerInvariant();
            if (text.Length == 0) return "";

            string formD = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(formD.Length);
            bool lastSpace = false;
            foreach (char c in formD)
            {
                UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(c);
                if (category == UnicodeCategory.NonSpacingMark)
                    continue;

                char mapped = c == 'đ' ? 'd' : c;
                if (char.IsLetterOrDigit(mapped))
                {
                    sb.Append(mapped);
                    lastSpace = false;
                }
                else if (!lastSpace)
                {
                    sb.Append(' ');
                    lastSpace = true;
                }
            }
            return sb.ToString().Trim();
        }

        private static List<string> Tokenize(string normalized)
        {
            return normalized.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(token => token.Length >= 2 && !StopWords.Contains(token))
                .Distinct()
                .ToList();
        }

        private static decimal ComputeContainmentRatio(List<string> answerTokens, List<string> correctTokens)
        {
            if (answerTokens == null || correctTokens == null || answerTokens.Count == 0 || correctTokens.Count == 0)
                return 0M;

            int longestRun = 0;
            int currentRun = 0;
            foreach (string token in correctTokens)
            {
                if (answerTokens.Any(candidate => candidate == token || ComputeSimilarity(candidate, token) >= 0.86M))
                {
                    currentRun++;
                    if (currentRun > longestRun) longestRun = currentRun;
                }
                else
                {
                    currentRun = 0;
                }
            }

            return (decimal)longestRun / correctTokens.Count;
        }

        private static List<string> MatchKeywords(List<string> correctTokens, List<string> answerTokens)
        {
            var matched = new List<string>();
            foreach (string token in correctTokens)
            {
                if (answerTokens.Any(candidate => candidate == token || ComputeSimilarity(candidate, token) >= 0.86M))
                    matched.Add(token);
            }
            return matched;
        }

        private static decimal ComputeSimilarity(string left, string right)
        {
            if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
                return 0M;
            if (string.Equals(left, right, StringComparison.OrdinalIgnoreCase))
                return 1M;

            int distance = ComputeLevenshtein(left, right);
            int maxLength = Math.Max(left.Length, right.Length);
            if (maxLength <= 0) return 1M;
            decimal similarity = 1M - ((decimal)distance / maxLength);
            if (similarity < 0M) similarity = 0M;
            return similarity;
        }

        private static int ComputeLevenshtein(string left, string right)
        {
            int[,] dp = new int[left.Length + 1, right.Length + 1];
            for (int i = 0; i <= left.Length; i++) dp[i, 0] = i;
            for (int j = 0; j <= right.Length; j++) dp[0, j] = j;

            for (int i = 1; i <= left.Length; i++)
            {
                for (int j = 1; j <= right.Length; j++)
                {
                    int cost = left[i - 1] == right[j - 1] ? 0 : 1;
                    dp[i, j] = Math.Min(
                        Math.Min(dp[i - 1, j] + 1, dp[i, j - 1] + 1),
                        dp[i - 1, j - 1] + cost);
                }
            }

            return dp[left.Length, right.Length];
        }
    }
}
