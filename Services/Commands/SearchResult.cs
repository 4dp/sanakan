using Discord.Commands;

namespace Sanakan.Services.Commands
{
    public class SearchResult
    {
        public SearchResult(IResult result = null, Command command = null)
        {
            Result = result;
            Command = command;
        }

        public IResult Result { get; private set; }
        public Command Command { get; private set; }

        public bool IsSuccess()
        {
            if (Command == null)
            {
                if (Result == null)
                    return false;

                return Result.IsSuccess;
            }

            return true;
        }
    }
}
