import heapq

class Task:
    """Task with job id, task id, machine, time, next task"""
    def __init__(self, job_id, task_id, machine, duration, succ, plan_start):
        self.job_id = job_id
        self.task_id = task_id
        self.machine = machine
        self.duration = duration
        self.successor = succ
        self.plan_start = plan_start
        self.plan_end = None
        self.successor_task = None
        self.predecessor_tasks = []

    def __repr__(self):
        return f"Task(job_id='{self.job_id}', task_id={self.task_id}, machine='{self.machine}', " \
               f"duration={self.duration}, successor={self.successor}, plan_start={self.plan_start})"

def calculate_dynamic_priority(task):
    # Priorität basierend auf der geplanten Startzeit der Vorgängeraufgaben
    if not task.predecessor_tasks:
        return task.plan_start if task.plan_start is not None else 0
    else:
        return max(pred.plan_start for pred in task.predecessor_tasks) + task.duration

def update_priorities(ready_operations):
    temp_heap = []
    while ready_operations:
        _,_, task = heapq.heappop(ready_operations)
        new_priority = calculate_dynamic_priority(task)
        heapq.heappush(temp_heap, (new_priority, (str(task.job_id) + str(task.task_id)), task))
    return temp_heap

def giffen_thompson(tasks, machine_pools):
    ready_operations = []
    inserted_tasks = set()
    machine_available_time = {machine: [0] * qty for machine, qty in machine_pools}

    # Dictionary zur Verwaltung der Task-Objekte über (job_id, task_id)
    task_dict = {(task.job_id, task.task_id): task for task in tasks}

    # Setze die Referenzen zu Successor und Predecessor Tasks
    for task in tasks:
        if task.successor != -1:
            next_task = task_dict[(task.job_id, task.successor)]
            task.successor_task = next_task
            next_task.predecessor_tasks.append(task)

    # Initialisiere die ersten Operationen jedes Jobs
    for task in tasks:
        if not task.predecessor_tasks:
            priority = calculate_dynamic_priority(task)
            heapq.heappush(ready_operations, (priority, (str(task.job_id) + str(task.task_id)), task))
            inserted_tasks.add(task)

    schedule = []

    while ready_operations:

        # Aktualisiere alle Prioritäten in der ready_operations Heap
        ready_operations = update_priorities(ready_operations)
        # !Potentielle optimierung: kann bei Statischen Prioritäten übersprungen werden, 
        # kann übersprungen werden wenn die Zeit nicht vorrangeschritten ist.
        _, _, current_task = heapq.heappop(ready_operations)

        # Überprüfe die Maschinenverfügbarkeit
        machine = current_task.machine
        available_times = machine_available_time[machine]
        earliest_start_time = max(available_times[0], current_task.plan_start if current_task.plan_start is not None else 0)
        selected_machine_idx = 0

        for i in range(1, len(available_times)):
            if available_times[i] < earliest_start_time:
                earliest_start_time = max(available_times[i], current_task.plan_start if current_task.plan_start is not None else 0)
                selected_machine_idx = i

        end_time = earliest_start_time + current_task.duration
        current_task.plan_end = end_time
        current_task.plan_start = earliest_start_time
        available_times[selected_machine_idx] = end_time

        # Aktualisiere die geplante Startzeit für die Nachfolgeaufgaben
        if current_task.successor_task:
            successor = current_task.successor_task
            # Überprüfe, ob alle Vorgängeraufgaben erledigt sind
            if all(pred.plan_end is not None for pred in successor.predecessor_tasks):
                earliest_start = max(pred.plan_end for pred in successor.predecessor_tasks)
                successor.plan_start = max(earliest_start, end_time)
                # Füge die Nachfolgeaufgabe zur Heap-Warteschlange hinzu
                priority = calculate_dynamic_priority(successor)
                heapq.heappush(ready_operations, (priority, (str(current_task.job_id) + str(current_task.task_id)), successor))
                inserted_tasks.add(successor)

        # Füge die Aufgabe zur Zeitplanung hinzu
        schedule.append([current_task.job_id, current_task.task_id, current_task.machine + '_' + str(selected_machine_idx), earliest_start_time, current_task.duration, end_time])

    return schedule

# Beispielhafte Datenstruktur
jobs_data = [
    ['p1', 1, 'a1', 17, 2, None],
    ['p1', 2, 'a2', 30, 4, None],
    ['p1', 3, 'a3', 14, 4, None],
    ['p1', 4, 'a4', 15, 5, None],
    ['p1', 5, 'a5', 25, -1, None],
    ['p2', 1, 'a1', 13, 3, None],
    ['p2', 2, 'a3', 15, 3, None],
    ['p2', 3, 'a2', 10, 4, None],
    ['p2', 4, 'a6', 20, -1, None],
]

# Maschinenpools definieren
machine_pools_data = [
    ['a1', 2],
    ['a2', 1],
    ['a3', 2],
    ['a4', 1],
    ['a5', 1],
    ['a6', 1],
]

# Konvertiere die jobs_data in Task-Objekte
tasks = [Task(*data) for data in jobs_data]

# Führe den Giffler Thompson Algorithmus aus
result_schedule = giffen_thompson(tasks, machine_pools_data)

# Ausgabe des Ergebnisses
for entry in result_schedule:
    print(entry)
