import matplotlib.pyplot as plt
import pandas as pd
from modules.factory.Operation import Operation
import math

class GanttSchedule:

    def create(schedule: list[Operation]) -> plt:
        # Daten in ein DataFrame konvertieren
        df = pd.DataFrame([op.to_dict_sim() for op in schedule])

        size_height = math.ceil(df["machine"].count().itemsize / 3)
        # Gantt-Diagramm erstellen
        fig, ax = plt.subplots(figsize=(24, size_height))

        # Maschinen als y-Werte für die Balken
        machines = df['machine'].unique()
        machine_to_y = {machine: i for i, machine in enumerate(machines)}

        # Erstelle eine Colormap für die job_ids
        unique_jobs = df['job_id'].unique()
        job_colors = {job_id: plt.cm.get_cmap('tab20')(i / len(unique_jobs)) for i, job_id in enumerate(unique_jobs)}

        # Iteriere über jede Zeile des DataFrames
        for i, row in df.iterrows():
            start = row['start_time']
            duration = row['duration']
            job_id = row['job_id']
            task_id = row['operation_id']
            machine = row['machine']
            label = f'{job_id} {task_id}'

            # Verwende die Farbe für den entsprechenden job_id
            color = job_colors[job_id]

            # Stelle die Aufgabe als Balken im Diagramm dar
            ax.barh(y=machine_to_y[machine], left=start, width=duration, height=0.8, align='center', color=color, edgecolor='black')
            
            # Text in den Balken einfügen (optional, falls du das möchtest)
            # ax.text(x=start + duration / 2, y=machine_to_y[machine], s=label, va='center', ha='center', color='black')

        # Diagramm formatieren
        ax.set_xlabel('Time')
        ax.set_yticks(list(machine_to_y.values()))
        ax.set_yticklabels(list(machine_to_y.keys()))
        ax.set_title('Gantt Chart')
        max_time = df['start_time'].max() + df['duration'].max()  # Determine max time to cover full range
        ax.set_xticks(range(0, math.ceil(max_time) + 1, 5))

        # Grid anzeigen
        ax.grid(True)

        # Diagramm anzeigen
        plt.show()

        return plt