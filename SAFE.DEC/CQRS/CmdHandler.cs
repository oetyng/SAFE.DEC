using SAFE.SystemUtils;
using System;
using System.Threading.Tasks;

namespace SAFE.CQRS
{
    public abstract class CmdHandler
    {
       protected readonly Repository _repo;

        public CmdHandler(Repository repo)
        {
            _repo = repo;
        }

        public async Task<Result<bool>> Handle(Cmd cmd)
        {
            try
            {
                var ctx = (IContext)Handle((dynamic)cmd);

                var changed = await ctx.ExecuteAsync();

                if (!changed)
                    return Result.OK(false);

                var savedChanges = await ctx.CommitAsync();
                if (!savedChanges)
                    throw new InvalidOperationException("Could not save changes!");

                return Result.OK(true);
            }
            catch (InvalidOperationException ex)
            {
                // logging
                return Result.Fail<bool>(ex.Message);
            }
            catch (Exception ex)
            {
                // logging
                return Result.Fail<bool>(ex.Message);
            }
        }
    }
}