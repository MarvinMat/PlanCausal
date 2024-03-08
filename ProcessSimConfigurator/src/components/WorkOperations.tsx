import {
	addOperationToWorkPlan,
	deleteOperationFromWorkPlan,
	editOperationInWorkPlan,
	swapOperationDownInWorkPlan,
	swapOperationUpInWorkPlan,
} from "../redux/dataSlice";
import { useAppDispatch, useAppSelector } from "../redux/hooks";
import { useState } from "react";
import { Button, ButtonGroup, Table } from "react-bootstrap";
import { IoAdd } from "react-icons/io5";
import { FiEdit } from "react-icons/fi";
import { BsTrashFill } from "react-icons/bs";
import { IoIosSave } from "react-icons/io";
import { AiOutlineClose, AiFillCaretUp, AiFillCaretDown } from "react-icons/ai";
import { DeleteModal } from "./DeleteModal";

type WorkOperationsProps = {
	workPlanId: number | null;
};

export const WorkOperations: React.FC<WorkOperationsProps> = ({ workPlanId }) => {
	const { tools, machines, workPlans } = useAppSelector((state) => state.data);
	const dispatch = useAppDispatch();

	const selectedWorkPlan = workPlans.find((workPlan) => workPlan.workPlanId === workPlanId);

	const [name, setName] = useState("");
	const [duration, setDuration] = useState(0);
	const [machineId, setMachineId] = useState<number | undefined>(undefined);
	const [toolId, setToolId] = useState<number | undefined>(undefined);

	const selectedMachine = machines.find((machine) => machine.typeId === machineId);
	const allowedTools = tools.filter((tool) => selectedMachine?.allowedToolIds.includes(tool.typeId));

	const [editingId, setEditingId] = useState<number | false>(false);
	const operationToEditIdx = editingId ? editingId - 1 : -1;
	const [deletingId, setDeletingId] = useState<number | false>(false);
	const operationToDeleteIdx = deletingId ? deletingId - 1 : -1;

	return (
		<>
			<div className="heading">Arbeitsgänge{!!selectedWorkPlan && ` für ${selectedWorkPlan.name}`}</div>
			{!selectedWorkPlan && (
				<div>
					Bearbeiten Sie einen Arbeitsplan, um seine Arbeitsgänge sehen und bearbeiten zu können.
				</div>
			)}
			{!!selectedWorkPlan && (
				<>
					<DeleteModal
						show={!!deletingId}
						text="Wollen Sie diesen Arbeitsgang wirklich löschen? Diese Aktion kann nicht rückgängig gemacht werden."
						onClose={() => setDeletingId(false)}
						onOk={() => {
							dispatch(
								deleteOperationFromWorkPlan({
									workPlanId: selectedWorkPlan.workPlanId,
									operationIdx: operationToDeleteIdx,
								})
							);
						}}
					/>
					<div className="input">
						<input
							type="text"
							placeholder="Name"
							value={name}
							onChange={(e) => setName(e.target.value)}
							autoComplete="off"
						/>
						<br />
						<input
							type="number"
							step={0.1}
							min={0}
							placeholder="Dauer (in Minuten)"
							value={duration}
							onChange={(e) => setDuration(parseFloat(e.target.value))}
							autoComplete="off"
						/>
						<br />
						<select
							key={machineId}
							value={machineId}
							onChange={(e) => {
								setMachineId(parseInt(e.target.value));
								setToolId(undefined);
							}}
						>
							<option value={undefined} hidden>
								Wählen Sie eine Maschine
							</option>
							{machines.map((machine) => (
								<option key={machine.typeId} value={machine.typeId}>
									{machine.name}
								</option>
							))}
						</select>
						{!!selectedMachine && (
							<>
								<br />
								<select value={toolId} onChange={(e) => setToolId(parseInt(e.target.value))}>
									<option value={undefined} hidden>
										Wählen Sie ein Werkzeug
									</option>
									{allowedTools.map((tool) => (
										<option key={tool.typeId} value={tool.typeId}>
											{tool.name}
										</option>
									))}
								</select>
							</>
						)}
						<br />
						{!editingId && (
							<Button
								type="button"
								variant="success"
								onClick={() => {
									dispatch(
										addOperationToWorkPlan({
											workPlanId: selectedWorkPlan.workPlanId,
											operation: {
												name,
												duration,
												machineId: machineId!,
												toolId: toolId!,
											},
										})
									);
									setName("");
									setDuration(0);
									setMachineId(undefined);
									setToolId(undefined);
								}}
								disabled={!name || duration === 0 || !machineId || !toolId}
							>
								<IoAdd size="1.35em" /> Hinzufügen
							</Button>
						)}
						{!!editingId && (
							<>
								<Button
									type="button"
									variant="success"
									onClick={() => {
										dispatch(
											editOperationInWorkPlan({
												workPlanId: selectedWorkPlan.workPlanId,
												operationIdx: operationToEditIdx,
												operation: {
													name,
													duration,
													machineId: machineId!,
													toolId: toolId!,
												},
											})
										);
										setName("");
										setDuration(0);
										setMachineId(undefined);
										setToolId(undefined);
										setEditingId(false);
									}}
									disabled={!name || duration === 0 || !machineId || !toolId}
								>
									<IoIosSave size="1.35em" /> Speichern
								</Button>
								<Button
									type="button"
									variant="secondary"
									onClick={() => {
										setName("");
										setDuration(0);
										setMachineId(undefined);
										setToolId(undefined);
										setEditingId(false);
									}}
								>
									<AiOutlineClose size="1.35em" /> Abbrechen
								</Button>
							</>
						)}
					</div>
					{selectedWorkPlan.operations.length === 0 && (
						<div>Noch keine Arbeitsgänge vorhanden.</div>
					)}
					{selectedWorkPlan.operations.length > 0 && (
						<Table striped bordered hover className="operation-table">
							<thead>
								<tr>
									<th>#</th>
									<th>Name</th>
									<th>Dauer</th>
									<th>Maschine</th>
									<th>Werkzeug</th>
									<th colSpan={3}></th>
								</tr>
							</thead>
							<tbody>
								{selectedWorkPlan.operations.map((operation, index, operations) => {
									const machine = machines.find(
										(machine) => machine.typeId === operation.machineId
									);
									const tool = tools.find((tool) => tool.typeId === operation.toolId);
									const id = index + 1;

									return (
										<tr key={operation.name}>
											<td>{id}</td>
											<td>{operation.name}</td>
											<td className="nowrap">{operation.duration} Minuten</td>
											<td>{machine?.name}</td>
											<td>{tool?.name}</td>
											<td>
												<ButtonGroup>
													{index > 0 && (
														<Button
															type="button"
															variant="secondary"
															style={{
																width: "2em",
																height: "2em",
																padding: "0.35em",
																paddingTop: "0.1em",
															}}
															onClick={() => {
																dispatch(
																	swapOperationUpInWorkPlan({
																		workPlanId:
																			selectedWorkPlan.workPlanId,
																		operationIdx: index,
																	})
																);
															}}
														>
															<AiFillCaretUp size="1.3em" />
														</Button>
													)}
													{index < operations.length - 1 && (
														<Button
															type="button"
															variant="secondary"
															style={{
																width: "2em",
																height: "2em",
																padding: "0.35em",
																paddingTop: "0.1em",
															}}
															onClick={() => {
																dispatch(
																	swapOperationDownInWorkPlan({
																		workPlanId:
																			selectedWorkPlan.workPlanId,
																		operationIdx: index,
																	})
																);
															}}
														>
															<AiFillCaretDown size="1.3em" />
														</Button>
													)}
												</ButtonGroup>
											</td>
											<td>
												<Button
													type="button"
													variant="secondary"
													onClick={() => {
														setName(operation.name);
														setDuration(operation.duration);
														setMachineId(operation.machineId);
														setToolId(operation.toolId);
														setEditingId(id);
													}}
												>
													<FiEdit size="1.2em" /> Bearbeiten
												</Button>
											</td>
											<td>
												<Button
													type="button"
													variant="danger"
													onClick={() => {
														setDeletingId(id);
													}}
												>
													<BsTrashFill size="1.2em" /> Löschen
												</Button>
											</td>
										</tr>
									);
								})}
							</tbody>
						</Table>
					)}
				</>
			)}
		</>
	);
};
