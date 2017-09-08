using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;

namespace FSM.Interfaces
{
    /// <summary>
    /// This interface defines the methods exposed by an actor.
    /// Clients use this interface to interact with the actor that implements it.
    /// </summary>
    public interface IFSM : IActor
    {
        Task Close(CancellationToken cancellationToken);
        Task Assign(string assignee, CancellationToken cancellationToken);
        Task<bool> CanAssign(CancellationToken cancellationToken);
        Task Defer(CancellationToken cancellationToken);
    }
}
