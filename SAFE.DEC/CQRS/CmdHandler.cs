using SAFE.SystemUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace SAFE.CQRS
{
    public abstract class CmdHandler
    {
        Dictionary<Type, Func<Cmd, IContext>> _handlers = new Dictionary<Type, Func<Cmd, IContext>>();
        protected readonly Repository _repo;


        public CmdHandler(Repository repo)
        {
            _repo = repo;
            var methods = this.GetType()
                .GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(m => m.Name == "Handle");

            foreach (var m in methods)
            {
                _handlers[m.GetParameters().First().ParameterType] = new Func<Cmd, IContext>((e) => (IContext)m.Invoke(this, new object[] { e }));
            }
        }

        public async Task<Result<bool>> Handle(Cmd cmd)
        {
            try
            {
                var ctx = _handlers[cmd.GetType()](cmd);

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