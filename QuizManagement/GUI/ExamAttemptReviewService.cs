using System;
using System.Collections.Generic;
using System.Data;
using QuizManagement.BUS;
using QuizManagement.DAL;

namespace QuizManagement.GUI
{
    internal static class ExamAttemptReviewService
    {
        public sealed class AttemptReviewData
        {
            public int CorrectCount { get; set; }
            public int TotalQuestions { get; set; }
            public double Score { get; set; }
            public decimal EarnedPoints { get; set; }
            public decimal TotalPoints { get; set; }
            public bool UsesPointSnapshot { get; set; }
            public bool IsPracticeMode { get; set; }
            public IReadOnlyList<ResultReviewRow> ReviewRows { get; set; }
        }

        public static AttemptReviewData LoadByResultId(int resultId)
        {
            var db = new Database();
            DataTable dtResult = db.ExecuteQuery(
                "SELECT TOP 1 r.Score, ISNULL(e.ScoreScale, 10) AS ScoreScale, ISNULL(e.ExamMode, N'Exam') AS ExamMode " +
                "FROM Results r LEFT JOIN Exams e ON r.ExamID = e.ExamID WHERE r.ResultID=" + resultId);
            DataTable dt = db.ExecuteQuery(
                "SELECT a.AttemptAnswerID, a.QuestionID, a.QuestionOrder, a.QuestionContent, a.OptionA, a.OptionB, a.OptionC, a.OptionD, a.CorrectAnswer, a.UserAnswer, a.IsCorrect, " +
                "a.QuestionPoints AS AttemptQuestionPoints, a.AwardedPoints AS AttemptAwardedPoints, " +
                "ISNULL(eq.Points, ISNULL(q.Points, 1)) AS CurrentQuestionPoints " +
                "FROM ExamAttemptAnswers a " +
                "LEFT JOIN ExamQuestions eq ON eq.ExamID = a.ExamID AND eq.QuestionID = a.QuestionID " +
                "LEFT JOIN Questions q ON q.QuestionID = a.QuestionID " +
                "WHERE a.ResultID=" + resultId + " ORDER BY a.QuestionOrder, a.AttemptAnswerID");

            var rows = new List<ResultReviewRow>();
            int correct = 0;
            int total = dt.Rows.Count;
            decimal earnedPoints = 0M;
            decimal totalPoints = 0M;
            bool usesPointSnapshot = false;
            bool isPracticeMode = dtResult.Rows.Count > 0 &&
                string.Equals((dtResult.Rows[0]["ExamMode"] ?? "").ToString(), "Practice", StringComparison.OrdinalIgnoreCase);

            foreach (DataRow r in dt.Rows)
            {
                bool isCorrect = ToBool(r["IsCorrect"]);
                if (isCorrect) correct++;
                decimal questionPoints = ToDecimal(r["AttemptQuestionPoints"], -1M);
                decimal awardedPoints = ToDecimal(r["AttemptAwardedPoints"], -1M);
                if (questionPoints >= 0 || awardedPoints >= 0)
                    usesPointSnapshot = true;
                if (questionPoints < 0)
                    questionPoints = ToDecimal(r["CurrentQuestionPoints"], 1M);
                if (awardedPoints < 0)
                    awardedPoints = isCorrect ? questionPoints : 0M;
                totalPoints += questionPoints;
                earnedPoints += awardedPoints;

                string correctAnswer = r["CorrectAnswer"].ToString();
                string userAnswer = r["UserAnswer"] == DBNull.Value ? "" : r["UserAnswer"].ToString();
                bool fillBlank = IsFillBlankAttempt(r);
                var fillEvaluation = fillBlank ? FillBlankScoringService.Evaluate(userAnswer, correctAnswer) : null;

                rows.Add(new ResultReviewRow
                {
                    AttemptAnswerId = ToInt(r["AttemptAnswerID"]),
                    QuestionId = ToInt(r["QuestionID"]),
                    Number = ToInt(r["QuestionOrder"]),
                    QuestionContent = r["QuestionContent"].ToString(),
                    UserAnswer = BuildDisplayAnswer(userAnswer, correctAnswer, r, false, fillBlank),
                    CorrectAnswer = BuildDisplayAnswer(correctAnswer, correctAnswer, r, true, fillBlank),
                    IsCorrect = isCorrect,
                    IsFillBlank = fillBlank,
                    MaxPoints = questionPoints,
                    AwardedPoints = awardedPoints,
                    SuggestedScoreRatio = fillEvaluation == null ? (isCorrect ? 1M : 0M) : fillEvaluation.ScoreRatio,
                    MatchedKeywords = fillEvaluation == null ? "" : fillEvaluation.MatchedKeywords
                });
            }

            double savedScore = dtResult.Rows.Count > 0 && dtResult.Rows[0]["Score"] != DBNull.Value
                ? Math.Round(Convert.ToDouble(dtResult.Rows[0]["Score"]), 2)
                : 0;
            decimal scoreScale = dtResult.Rows.Count > 0 ? ToDecimal(dtResult.Rows[0]["ScoreScale"], 10M) : 10M;
            if (!usesPointSnapshot && !isPracticeMode && savedScore > 0 && scoreScale > 0)
            {
                earnedPoints = Math.Round((decimal)savedScore, 2);
                totalPoints = scoreScale;
            }

            double score = savedScore > 0
                ? savedScore
                : (totalPoints <= 0 ? 0 : Math.Round((double)(earnedPoints / totalPoints * scoreScale), 2));
            if (isPracticeMode && totalPoints > 0)
                score = Math.Round((double)(earnedPoints / totalPoints * scoreScale), 2);
            return new AttemptReviewData
            {
                CorrectCount = correct,
                TotalQuestions = total,
                Score = score,
                EarnedPoints = earnedPoints,
                TotalPoints = totalPoints,
                UsesPointSnapshot = usesPointSnapshot,
                IsPracticeMode = isPracticeMode,
                ReviewRows = rows
            };
        }

        public static void ApplyTeacherFillBlankReview(int resultId, IReadOnlyList<ResultReviewRow> reviewedRows)
        {
            if (reviewedRows == null || reviewedRows.Count == 0) return;

            var db = new Database();
            DataTable dtResult = db.ExecuteQuery(
                "SELECT TOP 1 r.ResultID, ISNULL(e.ScoreScale, 10) AS ScoreScale " +
                "FROM Results r LEFT JOIN Exams e ON r.ExamID = e.ExamID WHERE r.ResultID=" + resultId);
            if (dtResult.Rows.Count == 0) return;

            foreach (ResultReviewRow row in reviewedRows)
            {
                if (!row.IsFillBlank || row.AttemptAnswerId <= 0) continue;
                bool isCorrect = row.MaxPoints > 0 && row.AwardedPoints >= row.MaxPoints;
                db.ExecuteNonQuery(
                    "UPDATE ExamAttemptAnswers SET AwardedPoints=" + row.AwardedPoints.ToString().Replace(",", ".") +
                    ", IsCorrect=" + (isCorrect ? "1" : "0") +
                    " WHERE AttemptAnswerID=" + row.AttemptAnswerId);
            }

            DataTable totals = db.ExecuteQuery(
                "SELECT ISNULL(SUM(ISNULL(QuestionPoints, 0)), 0) AS TotalPoints, ISNULL(SUM(ISNULL(AwardedPoints, 0)), 0) AS EarnedPoints " +
                "FROM ExamAttemptAnswers WHERE ResultID=" + resultId);
            decimal totalPoints = totals.Rows.Count > 0 ? ToDecimal(totals.Rows[0]["TotalPoints"], 0M) : 0M;
            decimal earnedPoints = totals.Rows.Count > 0 ? ToDecimal(totals.Rows[0]["EarnedPoints"], 0M) : 0M;
            decimal scoreScale = ToDecimal(dtResult.Rows[0]["ScoreScale"], 10M);
            decimal score = totalPoints <= 0 ? 0M : Math.Round((earnedPoints / totalPoints) * scoreScale, 2, MidpointRounding.AwayFromZero);
            db.ExecuteNonQuery("UPDATE Results SET Score=" + score.ToString().Replace(",", ".") + " WHERE ResultID=" + resultId);
        }

        private static string BuildDisplayAnswer(string answer, string correctAnswer, DataRow row, bool displayCorrectAnswer, bool fillBlank)
        {
            string value = (answer ?? "").Trim();
            if (string.IsNullOrWhiteSpace(value))
                return "(ỏ trống)";

            if (fillBlank)
                return value;

            string optionValue = null;
            switch (value.ToUpperInvariant())
            {
                case "A": optionValue = row["OptionA"].ToString(); break;
                case "B": optionValue = row["OptionB"].ToString(); break;
                case "C": optionValue = row["OptionC"].ToString(); break;
                case "D": optionValue = row["OptionD"].ToString(); break;
            }

            if (string.IsNullOrWhiteSpace(optionValue))
                return value;

            return value.ToUpperInvariant() + ". " + optionValue;
        }

        private static bool IsFillBlankAttempt(DataRow row)
        {
            bool emptyChoices =
                string.IsNullOrWhiteSpace(row["OptionA"].ToString()) &&
                string.IsNullOrWhiteSpace(row["OptionB"].ToString()) &&
                string.IsNullOrWhiteSpace(row["OptionC"].ToString()) &&
                string.IsNullOrWhiteSpace(row["OptionD"].ToString());

            string correct = (row["CorrectAnswer"] ?? "").ToString().Trim().ToUpperInvariant();
            bool isChoiceKey = correct == "A" || correct == "B" || correct == "C" || correct == "D";
            return emptyChoices || !isChoiceKey;
        }

        private static bool ToBool(object value)
        {
            if (value == null || value == DBNull.Value) return false;
            bool parsed;
            if (bool.TryParse(value.ToString(), out parsed)) return parsed;
            return value.ToString() == "1";
        }

        private static int ToInt(object value)
        {
            if (value == null || value == DBNull.Value) return 0;
            int result;
            return int.TryParse(value.ToString(), out result) ? result : 0;
        }

        private static decimal ToDecimal(object value, decimal fallback)
        {
            if (value == null || value == DBNull.Value) return fallback;
            decimal result;
            return decimal.TryParse(value.ToString(), out result) ? result : fallback;
        }
    }
}
