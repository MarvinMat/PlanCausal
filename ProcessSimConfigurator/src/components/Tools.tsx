import { Button, Table } from "react-bootstrap";
import { addTool, deleteTool, editTool } from "../redux/dataSlice";
import { useAppDispatch, useAppSelector } from "../redux/hooks";
import { useState } from "react";
import { IoAdd } from "react-icons/io5";
import { FiEdit } from "react-icons/fi";
import { BsTrashFill } from "react-icons/bs";
import { IoIosSave } from "react-icons/io";
import { AiOutlineClose } from "react-icons/ai";
import { DeleteModal } from "./DeleteModal";

export const Tools = () => {
	const tools = useAppSelector((state) => state.data.tools);
	const dispatch = useAppDispatch();

	const [name, setName] = useState("");
	const [description, setDescription] = useState("");

	const [editingId, setEditingId] = useState<number | false>(false);
	const [deletingId, setDeletingId] = useState<number | false>(false);

	return (
		<div className="content">
			<DeleteModal
				show={!!deletingId}
				text="Wollen Sie dieses Werkzeug wirklich löschen? Dies kann nicht rückgängig gemacht werden. Alle Maschinen und Arbeitsgänge, die dieses Werkzeug verwenden, werden ebenfalls gelöscht."
				onClose={() => setDeletingId(false)}
				onOk={() => {
					dispatch(deleteTool(deletingId as number));
				}}
			/>
			<div className="heading">Werkzeuge</div>
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
							dispatch(addTool({ name, description }));
							setName("");
							setDescription("");
						}}
						disabled={!name || !description || tools.some((tool) => tool.name === name)}
					>
						<IoAdd size={"1.4em"} /> Hinzufügen
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
							<AiOutlineClose size={"1.35em"} /> Abbrechen
						</Button>
						<Button
							type="button"
							variant="success"
							onClick={() => {
								dispatch(editTool({ name, description, typeId: editingId }));
								setName("");
								setDescription("");
								setEditingId(false);
							}}
							disabled={
								!name ||
								!description ||
								(tools.find((tool) => tool.name === name)?.typeId ?? editingId) !== editingId
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
						<th>Beschreibung</th>
						<th colSpan={2}></th>
					</tr>
				</thead>
				<tbody>
					{tools.map((tool) => (
						<tr key={tool.typeId}>
							<td>
								<b>{tool.name}</b>
							</td>
							<td>{tool.description}</td>
							<td>
								<Button
									type="button"
									variant="secondary"
									onClick={() => {
										setEditingId(tool.typeId);
										setName(tool.name);
										setDescription(tool.description);
									}}
								>
									<FiEdit size={"1.2em"} /> Bearbeiten
								</Button>
							</td>
							<td>
								<Button
									type="button"
									variant="danger"
									onClick={() => setDeletingId(tool.typeId)}
								>
									<BsTrashFill size={"1.2em"} /> Löschen
								</Button>
							</td>
							{/* {!!deletingId && deletingId === tool.typeId && (
								<>
									<button
										type="button"
										onClick={() => {
											dispatch(deleteTool(tool.typeId));
											setDeletingId(false);
										}}
									>
										Bestätigen
									</button>
									<button
										type="button"
										onClick={() => {
											setDeletingId(false);
										}}
									>
										Abbrechen
									</button>
								</>
							)} */}
						</tr>
					))}
				</tbody>
			</Table>
		</div>
	);
};
