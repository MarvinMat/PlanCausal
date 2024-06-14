from flask import Flask, render_template, request, redirect, url_for, send_file, make_response
import pandas as pd
import numpy as np
import simpy
from io import StringIO
from production_simulation import Machine, ProductionProcess, run_simulation

app = Flask(__name__)

@app.route('/')
def index():
    return render_template('index.html')

@app.route('/upload', methods=['POST'])
def upload_file():
    if 'file' not in request.files:
        return redirect(request.url)
    
    file = request.files['file']
    if file.filename == '':
        return redirect(request.url)

    if file:
        df = pd.read_csv(file)
        orders = df.to_dict('records')
        
        env = simpy.Environment()
        machines = [
            Machine(env, machine_id=1, process_time_mean=5.0, process_time_std=1.0),
            Machine(env, machine_id=2, process_time_mean=6.0, process_time_std=1.5)
        ]
        
        interarrival_time_mean = 7.0
        interarrival_time_std = 2.0
        
        # Simulate and capture output
        output = StringIO()
        run_simulation(machines, interarrival_time_mean, interarrival_time_std, orders, output)
        
        # Prepare the response
        response = make_response(output.getvalue())
        response.headers["Content-Disposition"] = "attachment; filename=simulation_output.txt"
        response.mimetype = "text/plain"
        return response

    return redirect(url_for('index'))

if __name__ == "__main__":
    app.run(debug=True)
