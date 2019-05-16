#pragma warning disable 1591

namespace Sanakan.Database.Models
{
    public class Answer
    {
        public ulong Id { get; set; }
        public int Number { get; set; }
        public string Content { get; set; }

        public ulong QuestionId { get; set; }
        public virtual Question Question { get; set; }
    }
}