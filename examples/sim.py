import simpy


# mini example of ressource group 

# A resource has a limited and fixed number of slots that can be requested by a process. 
# If all slots are taken, requesters are put into a queue. If a process releases a slot,
# the next process is popped from the queue and gets one slot

def do(name, env, machine_store):
    machine = yield machine_store.get()
    with machine.request() as req:
        yield req
        print(name, 'got machine', machine, 'at', env.now)
        yield env.timeout(1)
    yield machine_store.put(machine)
    print(name, 'done at', env.now)


env = simpy.Environment()
machines = [simpy.Resource(env, 1) for i in range(2)]
machine_store = simpy.Store(env, len(machines))
machine_store.items = machines
for i in range(3):
    env.process(do(i, env, machine_store))
env.run()