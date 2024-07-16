from factory.MachineState import State
import simpy

class Machine(simpy.Resource):
    """Operation with job id, operation id, machine, duration, next operation"""
    def __init__(self, id, group, tools, env = None):
        self.id = id
        self.group = group
        self.tools = tools
        self.current_operation = None
        self.current_tool = None
        self.state = State.FREE
        self.history = []
        
        super().__init__(env, 1)

    def __repr__(self):
        return f"Machine(id='{self.id}', name={self.group}, tool={self.current_tool}, " \
               f"operation={self.current_operation}, state={self.state})"
