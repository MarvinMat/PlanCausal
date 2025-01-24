from modules.simulator.Simulator import Simulator
from modules.simulator.Monitoring.BasicMonitor import monitorResource
from functools import partial
from modules.factory.Operation import Operation

def run_simulation(machines, operations, model, oberserved_data_path) -> list[Operation]:
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
                    , model,
                    oberserved_data_path)

    sim.env.run(12000)
    
    return sim.schedule 