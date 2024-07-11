"""
Simpy demo showing how to dynamicaly chain
together tasks to make a process.

a task can have more then one pressedent
and the task will wait for all pressedents to
finish before starting

programmer: Michael R. Gibbs
"""

import simpy

# each machine type has it own resource pool
# This allows testing the addition of 
# machines to relieve a bottle necks
machine_pools_data = [
    # machine name, qty
    ['a1', 1],
    ['a2', 1],
    ['a3', 1],
    ['a4', 1],
    ['a5', 1],
    ['a6', 1],    
]

# defines a job made up of tasks
# each task uses a machine for x amount of time
# and it output goes to a next task.
jobs_data = [
    # job id, task id, machine, time, next task
    ['p1', 1, 'a1', 17, 2],
    ['p1', 2, 'a2', 30, 4],
    ['p1', 3, 'a3', 14, 4],
    ['p1', 4, 'a4', 15, 5],
    ['p1', 5, 'a5', 25, -1],

    ['p2', 1, 'a1', 13, 3],
    ['p2', 2, 'a3', 15, 3],
    ['p2', 3, 'a2', 10, 4],
    ['p2', 4, 'a6', 20, -1],
]


def task(env, job_id, task_id, machine_pool, time, precedent_tasks):
    """
    hart of the processing

    waits for the completions of pressidenct tasks (list can be empty)
    grabs a resouce
    spend some time doing the task
    """
        
    print(f'{env.now}, job: {job_id}, task_id: {task_id}, waiting for presedents')
    
    yield env.all_of(precedent_tasks)

    print(f'{env.now}, job: {job_id}, task_id: {task_id}, getting resource')
    with machine_pool.request() as req:
        
        yield req

        print(f'{env.now}, job: {job_id}, task_id: {task_id}, starting task')

        yield env.timeout(time)

    print(f'{env.now}, job: {job_id}, task_id: {task_id}, finished task')


def build_pools(env, pool_data):
    """
    builds a dict of resouces pools from data

    index 0: name of machine type
    index 1: number of machines in the pool
    """

    pools = {}

    for pool in pool_data:
            pools[pool[0]] = simpy.Resource(env, capacity=pool[1])

    return pools

def build_jobs(env, pools, job_data):
    """
    builds a tree of tasks where the root node
    is the exit of the job, and leaf nodes
    start the job.  leaf nodes have no pressidents
    there can be more then one leaf node.
    there can only be one root node
    """
      
    jobs = {}


    # prime the node tree with default empty nodes
    for job in job_data:
        tasks = jobs.setdefault(job[0],{})
        tasks[job[1]] = []

        if job[4] < 0:
             # add exit node
             tasks[-1] = []

    # fill in pressedents for each node
    # leaf nodes end with empty pressident lists
    for job in job_data:
         
        tasks = jobs[job[0]] # tasks for job
        press = tasks[job[4]] # get pressident list for task
        press.append(job) # add pressedent node data

    # start a recursive process that
    # walks the node tree, creating the tasks
    for job in jobs.keys():
         tasks = jobs[job]

         exit_node = tasks[-1][0]

         build_tasks(env, tasks, exit_node, pools)

def build_tasks(env, tasks, node, pools):
    """
    recurse down the pressidents and work from the
    leafs back creating tasks, which are used as
    pressident events for the parent node.
    """

    press_tasks = []

    press_nodes = tasks[node[1]] # get list of pressident nodes

    # recurse the pressidents to get task processes that
    # this node can use to wait on.
    for press_node in press_nodes:
        press_tasks.append(build_tasks(env, tasks, press_node, pools))

    # create the task process
    t = task(env, node[0], node[1], pools[node[2]], node[3], press_tasks)

    # retrun the process to the parent, which the parent
    # will wait on as a pressident
    t = env.process(t)

    return t


# boot up
env = simpy.Environment()

pools = build_pools(env, machine_pools_data)

build_jobs(env, pools, jobs_data)

env.run(100)

print('done')