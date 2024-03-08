import { useAppDispatch, useAppSelector } from "../redux/hooks";
import { toolSchema } from "../types/Tool";
import { machineSchema } from "../types/Machine";
import { workPlanSchema } from "../types/WorkPlan";
import { setMachines, setTools, setWorkPlans } from "../redux/dataSlice";
import { Button, Form } from "react-bootstrap";
import {PiExport} from "react-icons/pi";

export const ImportExport = () => {
	const { machines, tools, workPlans } = useAppSelector((state) => state.data);
	const dispatch = useAppDispatch();

	const handleFileChange = (event: React.ChangeEvent<HTMLInputElement>) => {
		const selectedFiles = event.target.files;
		if (!selectedFiles) return;
        
		for (let i = 0; i < selectedFiles.length; i++) {
            const reader = new FileReader();
			const file = selectedFiles[i];
			if (file.type === "application/json") {
				reader.onload = () => {
					try {
						const parsedData = toolSchema.array().parse(JSON.parse(reader.result as string));
						dispatch(setTools(parsedData));
					} catch (error) {
						try {
							const parsedData = machineSchema
								.array()
								.parse(JSON.parse(reader.result as string));
							dispatch(setMachines(parsedData));
						} catch (error) {
							try {
								const parsedData = workPlanSchema
									.array()
									.parse(JSON.parse(reader.result as string));
								dispatch(setWorkPlans(parsedData));
							} catch (error) {
								alert(`Error parsing file ${file.name}.`);
							}
						}
					}
				};
				reader.readAsText(file);
			} else {
				alert(`File ${file.name} is not a JSON file.`);
			}
		}
	};

	const preview = {
		machines: JSON.stringify(machines, null, 2),
		tools: JSON.stringify(tools, null, 2),
		workPlans: JSON.stringify(workPlans, null, 2),
	};

	const downloadFiles = () => {
		const machineData = JSON.stringify(machines, null, 2);
		const toolData = JSON.stringify(tools, null, 2);
		const workplanData = JSON.stringify(workPlans, null, 2);

		const machineBlob = new Blob([machineData], { type: "application/json" });
		const toolBlob = new Blob([toolData], { type: "application/json" });
		const workplanBlob = new Blob([workplanData], { type: "application/json" });

		const machineUrl = URL.createObjectURL(machineBlob);
		const toolUrl = URL.createObjectURL(toolBlob);
		const workplanUrl = URL.createObjectURL(workplanBlob);

		const machineLink = document.createElement("a");
		machineLink.href = machineUrl;
		machineLink.download = "Machines.json";

		const toolLink = document.createElement("a");
		toolLink.href = toolUrl;
		toolLink.download = "Tools.json";

		const workplanLink = document.createElement("a");
		workplanLink.href = workplanUrl;
		workplanLink.download = "Workplans.json";

		document.body.appendChild(machineLink);
		document.body.appendChild(toolLink);
		document.body.appendChild(workplanLink);

		machineLink.click();
		toolLink.click();
		workplanLink.click();

		document.body.removeChild(machineLink);
		document.body.removeChild(toolLink);
		document.body.removeChild(workplanLink);

		URL.revokeObjectURL(machineUrl);
		URL.revokeObjectURL(toolUrl);
		URL.revokeObjectURL(workplanUrl);
	};

	return (
		<div className="content">
			<div className="heading">Import</div>
			<div className="import">
				Wählen Sie bestehende JSON-Dateien aus, um sie zu importieren. Die Namen dieser Dateien sind
				nicht relevant, es wird aus dem Inhalt erkannt, ob es sich um Werkzeuge, Maschinen oder
				Arbeitspläne handelt.
				<br />
				<br />
				<Form.Control type="file" multiple onChange={handleFileChange} accept="application/json" />
			</div>
			<div className="heading">Export</div>
			<div className="preview">
				<div>
					<h3>Werkzeuge</h3>
					<pre>{preview.tools}</pre>
				</div>
				<div>
					<h3>Maschinen</h3>
					<pre>{preview.machines}</pre>
				</div>
				<div>
					<h3>Arbeitspläne</h3>
					<pre>{preview.workPlans}</pre>
				</div>
			</div>
			<Button onClick={downloadFiles} className="export-button">
				<PiExport size={"1.25em"} /> Exportieren
			</Button>
		</div>
	);
};
