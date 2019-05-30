#pragma warning disable 1591

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Sanakan.Extensions;
using Microsoft.EntityFrameworkCore;
using Z.EntityFramework.Plus;
using Sanakan.Config;

namespace Sanakan.Api.Controllers
{
    [ApiController, Authorize]
    [Route("api/[controller]")]
    public class QuizController : ControllerBase
    {
        private IConfig _config;

        public QuizController(IConfig config)
        {
            _config = config;
        }

        /// <summary>
        /// Pobiera liste pytań
        /// </summary>
        [HttpGet("questions")]
        public async Task<List<Database.Models.Question>> GetQuestionsAsync()
        {
            using (var db = new Database.UserContext(_config))
            {
                return await db.GetCachedAllQuestionsAsync();
            }
        }

        /// <summary>
        /// Pobiera pytanie po id
        /// </summary>
        /// <param name="id">id pytania</param>
        /// <response code="500">Internal Server Error</response>
        [HttpGet("question/{id}")]
        public async Task<Database.Models.Question> GetQuestionAsync(ulong id)
        {
            using (var db = new Database.UserContext(_config))
            {
                return await db.GetCachedQuestionAsync(id);
            }
        }

        /// <summary>
        /// Dodaje nowe pytanie
        /// </summary>
        /// <param name="question">pytanie</param>
        /// <response code="500">Internal Server Error</response>
        [HttpPost("question")]
        public async Task AddQuestionAsync([FromBody]Database.Models.Question question)
        {
            using (var db = new Database.UserContext(_config))
            {
                db.Questions.Add(question);
                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"quiz" });
            }
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
            using (var db = new Database.UserContext(_config))
            {
                var question = await db.Questions.Include(x => x.Answer).FirstOrDefaultAsync(x => x.Id == id);
                if (question != null)
                {
                    db.Questions.Remove(question);
                    await db.SaveChangesAsync();

                    QueryCacheManager.ExpireTag(new string[] { $"quiz" });

                    await "Question removed!".ToResponse(200).ExecuteResultAsync(ControllerContext);
                    return;
                }
            }
            await "Question not found!".ToResponse(404).ExecuteResultAsync(ControllerContext);
        }
    }
}