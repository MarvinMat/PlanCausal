import { useState } from "react";
import { useAppDispatch, useAppSelector } from "../redux/hooks";
import { addWorkPlan, deleteWorkPlan, editWorkPlan } from "../redux/dataSlice";
import { WorkOperations } from "./WorkOperations";
import { Button, Table } from "react-bootstrap";
import { IoAdd } from "react-icons/io5";
import { FiEdit } from "react-icons/fi";
import { BsTrashFill } from "react-icons/bs";
import { IoIosSave } from "react-icons/io";
import { AiOutlineClose } from "react-icons/ai";
import { DeleteModal } from "./DeleteModal";

export const WorkPlans = () => {
	const workPlans = useAppSelector((state) => state.data.workPlans);
	const dispatch = useAppDispatch();

	const [name, setName] = useState("");
	const [description, setDescription] = useState("");

	const [editingId, setEditingId] = useState<number | false>(false);
	const [deletingId, setDeletingId] = useState<number | false>(false);

	return (
		<>
			<div className="content" style={{ width: editingId ? "30%" : "70%" }}>
				<DeleteModal
					show={!!deletingId}
					text="Sind Sie sicher, dass Sie diesen Arbeitsplan löschen wollen? Dies kann nicht rückgängig gemacht werden. Alle Operationen, die zu diesem Arbeitsplan gehören, werden ebenfalls gelöscht."
					onClose={() => setDeletingId(false)}
					onOk={() => {
						dispatch(deleteWorkPlan(deletingId as number));
					}}
				/>

				<div className="heading">Arbeitspläne</div>
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
						type="text"
						placeholder="Beschreibung"
						value={description}
						onChange={(e) => setDescription(e.target.value)}
						autoComplete="off"
					/>
					<br />
					{!editingId && (
						<Button
							type="button"
							variant="success"
							onClick={() => {
								dispatch(addWorkPlan({ name, description, operations: [] }));
								setName("");
								setDescription("");
							}}
							disabled={
								!name || !description || workPlans.some((workPlan) => workPlan.name === name)
							}
						>
							<IoAdd size={"1.35em"} /> Hinzufügen
						</Button>
					)}
					{!!editingId && (
						<>
							<Button
								type="button"
								variant="secondary"
								onClick={() => {
									setName("");
									setDescription("");
									setEditingId(false);
								}}
							>
								<AiOutlineClose size="1.35em" /> Abbrechen
							</Button>
							<Button
								type="button"
								variant="success"
								onClick={() => {
									dispatch(editWorkPlan({ name, description, workPlanId: editingId }));
									setName("");
									setDescription("");
									setEditingId(false);
								}}
								disabled={
									!name ||
									!description ||
									(workPlans.find((workPlan) => workPlan.name === name)?.workPlanId ??
										editingId) !== editingId
								}
							>
								<IoIosSave size={"1.35em"} /> Speichern
							</Button>
						</>
					)}
				</div>
				<Table striped bordered hover>
					<thead>
						<tr>
							<th>Name</th>
							{!editingId && <th>Beschreibung</th>}
							<th colSpan={2}></th>
						</tr>
					</thead>
					<tbody>
						{workPlans.map((workPlan) => (
							<tr key={workPlan.workPlanId}>
								<td>
									<b>{workPlan.name}</b>
								</td>
								{!editingId && <td>{workPlan.description}</td>}
								<td>
									<Button
										type="button"
										variant="secondary"
										className="nowrap"
										onClick={() => {
											setName(workPlan.name);
											setDescription(workPlan.description);
											setEditingId(workPlan.workPlanId);
										}}
									>
										<FiEdit size="1.2em" /> {!editingId && "Bearbeiten"}
									</Button>
								</td>
								<td>
									<Button
										type="button"
										variant="danger"
										className="nowrap"
										onClick={() => {
											setDeletingId(workPlan.workPlanId);
										}}
									>
										<BsTrashFill size="1.2em" /> {!editingId && "Löschen"}
									</Button>
								</td>
							</tr>
						))}
					</tbody>
				</Table>
			</div>
			<div className="content">
				<WorkOperations workPlanId={editingId ? editingId : null} />
			</div>
		</>
	);
};
