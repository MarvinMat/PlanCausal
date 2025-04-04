import logging
import os

from casymda.visualization.web_server.sim_controller import (
    RunnableSimulation,
    SimController,
)
from flask import Flask, request, send_file, send_from_directory, Response
from flask_cors import CORS

logging.getLogger("werkzeug").setLevel(logging.ERROR)


class FlaskSimServer:
    """provides control endpoints and serves visual state of a simulation model"""

    def __init__(
        self, sim_controller: SimController, port=5000, host="0.0.0.0"
    ) -> None:
        self.sim_controller: SimController = sim_controller
        self.root_file = sim_controller.simulation.root_file
        self.port = port
        self.host = host

    def run_sim_server(self):
        app = Flask(__name__)
        CORS(app)
        app_dir = os.path.dirname(os.path.abspath(self.root_file))
        flask_dir = os.path.dirname(os.path.abspath(__file__))
        print(
            "starting flask server, port: %s, app_dir: %s, flask_dir: %s"
            % (self.port, app_dir, flask_dir)
        )

        @app.route("/")
        def root():
            # assumed to be next to this file
            HTML_FILE = "canvas_animation.html"
            return send_from_directory(flask_dir, HTML_FILE)

        @app.route("/lib-files/<filename>")
        def lib_files(filename):
            # assumed to be next to this file
            return send_from_directory(flask_dir, filename)

        @app.route("/files")
        def provide_file():
            filepath = request.args.get("filepath")
            if filepath.startswith("/") or filepath.startswith("C:"):
                # absolute path
                return send_file(filepath)
            else:
                # relative path
                return send_from_directory(app_dir, filepath)

        @app.route("/width")
        def get_width():
            return str(self.sim_controller.get_sim_width())

        @app.route("/height")
        def get_height():
            return str(self.sim_controller.get_sim_height())

        @app.route("/state")
        def get_state():
            return self.sim_controller.get_state_dumps()

        @app.route("/partial_state")
        def get_partial_state():
            return self.sim_controller.get_partial_state_dumps()

        @app.route("/state_fb")  # flatbuffers binary
        def get_state_fb():
            binary = self.sim_controller.get_state_dumps_fb()
            #print(f"Type of data returned: {type(binary)}")  # Debug print to confirm the data type
            # Ensure the data is in bytes if it is not already
            if isinstance(binary, bytearray):
                binary = bytes(binary)
            return Response(binary, mimetype="binary/octet-stream")

        @app.route("/partial_state_fb")  # flatbuffers binary
        def get_partial_state_fb():
            binary = self.sim_controller.get_partial_state_dumps_fb()
            #print(f"Type of data returned: {type(binary)}")  # Debug print to confirm the data type
            # Ensure the data is in bytes if it is not already
            if isinstance(binary, bytearray):
                binary = bytes(binary)
            return Response(binary, mimetype="binary/octet-stream")

        @app.route("/start", methods=["POST"])
        def start():
            return self.sim_controller.start_simulation_process()

        @app.route("/stop", methods=["POST"])
        def stop():
            return self.sim_controller.reset_sim()

        @app.route("/pause", methods=["POST"])
        def pause():
            return self.sim_controller.pause_sim()

        @app.route("/resume", methods=["POST"])
        def resume():
            return self.sim_controller.resume_sim()

        @app.route("/rt_factor", methods=["POST"])
        def post_rt_factor():
            value = float(request.args.get("value"))
            return self.sim_controller.set_rt_factor(value)

        # set debug=False for IDE debugging
        app.run(debug=True, threaded=False, port=self.port, host=self.host)


def run_server(runnable_sim: RunnableSimulation, port=5000, host="0.0.0.0"):
    sim_controller = SimController(runnable_sim)
    flask_sim_server = FlaskSimServer(sim_controller, port=port, host=host)
    flask_sim_server.run_sim_server()
