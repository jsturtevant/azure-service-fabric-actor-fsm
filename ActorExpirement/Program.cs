using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Actors.Query;
using FSM.Interfaces;

namespace ActorExpirement
{
    class Program
    {
        static async Task Main(string[] args)
        {
            ActorId actorId = new ActorId("test");

            // This only creates a proxy object, it does not activate an actor or invoke any methods yet.
            IFSM actor = ActorProxy.Create<IFSM>(actorId, new Uri("fabric:/ActorFSM/FSMActorService"));

            Console.WriteLine("running actor 1");
            var token = new CancellationToken();
            await actor.Assign("Joe", token);
            await actor.Defer(token);
            await actor.Assign("Harry", token);
            await actor.Assign("Fred", token);
            await actor.Close(token);

            Console.WriteLine("running actor 2");
            ActorId actorId2 = new ActorId("test2");
            IFSM actor2 = ActorProxy.Create<IFSM>(actorId2, new Uri("fabric:/ActorFSM/FSMActorService"));
            await actor2.Assign("Joe", token);
            await actor2.Defer(token);
            await actor2.Assign("Harry", token);

            Console.WriteLine("waiting for actors to deactivate");
            await Task.Delay(TimeSpan.FromSeconds(20));

            var actorService = ActorServiceProxy.Create(new Uri("fabric:/ActorFSM/FSMActorService"), actorId);

            ContinuationToken continuationToken = null;
            IEnumerable<ActorInformation> inactiveActors;
            do
            {
                var queryResult = await actorService.GetActorsAsync(continuationToken, token);
                inactiveActors = queryResult.Items.Where(x => !x.IsActive);
                continuationToken = queryResult.ContinuationToken;
            } while (continuationToken != null);

            Console.WriteLine("inactive actors are ones we just created");
            foreach (var actorInformation in inactiveActors)
            {
                Console.WriteLine(actorInformation.ActorId);
            }

            try
            {
                //should blow up because joe is in closed state 
                await actor.Assign("Joe", token);
            }
            catch (Exception e)
            {
                Console.WriteLine("should blow up becuase it is in closed state");
            }

            Console.WriteLine("actor2 should be able to close");
            await actor2.Close(token);

            Console.ReadKey();
        }
    }
}   
