using SAFE.EventSourcing.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SAFE.CQRS
{
    public interface IContext
    {
        Task<bool> ExecuteAsync();
        Task<bool> CommitAsync();
    }

    public class Context<TCmd, TAggregate> : IContext where TCmd : Cmd where TAggregate : Aggregate
    {
        readonly TCmd _cmd;
        readonly Repository _repo;
        TAggregate _instance;
        Func<TCmd, TAggregate, Task<bool>> _action;

        public Context(TCmd cmd, Repository repo)
        {
            //if (!HandlesCmd())
            //    throw new ArgumentException($"{nameof(TAggregate)} does not handle ...");
            _cmd = cmd;
            _repo = repo;
        }

        public void SetAction(Func<TCmd, TAggregate, Task<bool>> action)
        {
            if (_action != null)
                throw new InvalidOperationException("An action is already added!");
            _action = action;
        }

        public async Task<bool> ExecuteAsync()
        {
            if (_action == null)
                throw new InvalidOperationException("Cannot execute before an action is added!");
            if (_instance != null)
                throw new InvalidOperationException("Can only execute once!");
            _instance = await Locate(_cmd);
            return await _action(_cmd, _instance);
        }

        public async Task<bool> CommitAsync()
        {
            if (_instance == null)
                throw new InvalidOperationException("Cannot commit before executing!");

            var events = _instance.GetUncommittedEvents();
            if (events.Count == 0)
                throw new InvalidOperationException("Already committed.");

            var data = events.Select(e => new EventData(
                e.Payload,
                _cmd.CorrelationId,
                _cmd.Id,
                e.EventClrType,
                e.Id,
                e.Name,
                e.SequenceNumber,
                e.TimeStamp))
            .ToList();

            var batch = new EventBatch(_instance.StreamKey, _cmd.Id, data);

            if (!await _repo.Save(batch))
                return false;

            _instance.ClearUncommittedEvents();
            return true;
        }

        async Task<TAggregate> Locate(TCmd cmd)
        {
            var streamKey = StreamKey(cmd);
            return await _repo.GetAR<TAggregate>(streamKey, cmd.ExpectedVersion);
        }

        // The cmd will hold information
        // on which stream key it is intended for.
        // since every cmd map to exactly one aggregate 
        // type and the aggregate type (stream name) together 
        // with cmd property TargetId (stream id) will 
        // form the StreamKey. (StreamKey = [StreamName]@[StreamId])
        string StreamKey(TCmd cmd)
        {
            return $"{typeof(TAggregate).Name}@{cmd.TargetId}";
        }
    }
}