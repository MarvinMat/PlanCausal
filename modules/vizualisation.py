import matplotlib.pyplot as plt
import pandas as pd
from modules.factory.Operation import Operation
import math
import os

class GanttSchedule:

    @staticmethod
    def create(schedule: pd.DataFrame, output_dir: str, model_name: str) -> str:
        """
        Create a Gantt chart and save it to a configurable path with the model name included.

        Args:
            schedule (list[Operation]): List of operations to plot.
            output_dir (str): Base directory for saving the output.
            model_name (str): Name of the model, used in the output filename.

        Returns:
            str: Full path to the saved Gantt chart.
        """
        size_height = math.ceil(schedule["machine"].count().itemsize / 3)
        # Gantt-Diagramm erstellen
        fig, ax = plt.subplots(figsize=(24, size_height))

        # Maschinen als y-Werte für die Balken
        machines = schedule['machine'].unique()
        machine_to_y = {machine: i for i, machine in enumerate(machines)}

        # Erstelle eine Colormap für die job_ids
        unique_jobs = schedule['job_id'].unique()
        job_colors = {job_id: plt.cm.get_cmap('tab20')(i / len(unique_jobs)) for i, job_id in enumerate(unique_jobs)}

        # Iteriere über jede Zeile des DataFrames
        for i, row in schedule.iterrows():
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
        max_time = schedule['start_time'].max() + schedule['duration'].max()  # Determine max time to cover full range
        ax.set_xticks(range(0, math.ceil(max_time) + 1, 5))

        # Grid anzeigen
        ax.grid(True)

        # Ensure the output directory exists
        os.makedirs(output_dir, exist_ok=True)

        # Construct the output filename and path
        output_file = os.path.join(output_dir, f'gantt_chart_{model_name}.png')

        # Save the figure
        fig.savefig(output_file, dpi=300, bbox_inches='tight')

        # Return the full path to the saved chart
        return output_file