#pragma warning disable 1591

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Sanakan.Extensions;
using Microsoft.EntityFrameworkCore;
using Z.EntityFramework.Plus;

namespace Sanakan.Api.Controllers
{
    [ApiController, Authorize]
    [Route("api/[controller]")]
    public class QuizController : ControllerBase
    {
        private readonly Database.UserContext _database;

        public QuizController(Database.UserContext db)
        {
            _database = db;
        }

        /// <summary>
        /// Pobiera liste pytań
        /// </summary>
        [HttpGet("questions")]
        public async Task<List<Database.Models.Question>> GetQuestionsAsync()
        {
            return await _database.GetCachedAllQuestionsAsync();
        }

        /// <summary>
        /// Pobiera pytanie po id
        /// </summary>
        /// <param name="id">id pytania</param>
        /// <response code="500">Internal Server Error</response>
        [HttpGet("question/{id}")]
        public async Task<Database.Models.Question> GetQuestionAsync(ulong id)
        {
            return await _database.GetCachedQuestionAsync(id);
        }

        /// <summary>
        /// Dodaje nowe pytanie
        /// </summary>
        /// <param name="question">pytanie</param>
        /// <response code="500">Internal Server Error</response>
        [HttpPost("question")]
        public async Task AddQuestionAsync([FromBody]Database.Models.Question question)
        {
            _database.Questions.Add(question);
            await _database.SaveChangesAsync();

            QueryCacheManager.ExpireTag(new string[] { $"quiz" });

            await "Question added!".ToResponse(200).ExecuteResultAsync(ControllerContext);
        }

        /// <summary>
        /// Usuwa pytanie
        /// </summary>
        /// <param name="id">id pytania</param>
        /// <response code="404">Question not found</response>
        /// <response code="500">Internal Server Error</response>
        [HttpDelete("question/{id}")]
        public async Task RemoveQuestionAsync(ulong id)
        {
            var question = await _database.Questions.Include(x => x.Answer).FirstOrDefaultAsync(x => x.Id == id);
            if (question != null)
            {
                _database.Questions.Remove(question);
                await _database.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"quiz" });

                await "Question removed!".ToResponse(200).ExecuteResultAsync(ControllerContext);
                return;
            }
            await "Question not found!".ToResponse(404).ExecuteResultAsync(ControllerContext);
        }
    }
}