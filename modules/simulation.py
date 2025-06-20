from modules.simulator.Simulator import Simulator
from modules.simulator.Monitoring.BasicMonitor import monitorResource
from functools import partial
from modules.factory.Operation import Operation

def run_simulation(machines, operations, model, planned_mode, oberserved_data_path = None) -> list[Operation]:
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
                    , model
                    , oberserved_data_path
                    , planned_mode=planned_mode)

    sim.env.run(100000000)
    
    sim.write_data() if oberserved_data_path is not None else None
    
    return sim.schedule 