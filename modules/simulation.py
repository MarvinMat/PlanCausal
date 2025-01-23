from modules.simulator.Simulator import Simulator
from modules.simulator.Monitoring.BasicMonitor import monitorResource
from functools import partial

def run_simulation(machines, operations, inference_func, plan_name="plan"):
    """
    Execute a simulation using a given plan and operations.
    """
    # array to store monitored data
    data = []

    # resource monitor [pre , post] execution
    monitor = [None, partial(monitorResource, data)]

    sim = Simulator(machines
                    , operations
                    , monitor
                    , inference_func)

    sim.env.run(12000)
    
    return data 