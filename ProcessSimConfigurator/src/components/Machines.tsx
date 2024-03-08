import { useState } from "react";
import { useAppDispatch, useAppSelector } from "../redux/hooks";
import { addMachine, deleteMachine, editMachine } from "../redux/dataSlice";
import { Button, Table } from "react-bootstrap";
import { IoAdd } from "react-icons/io5";
import { FiEdit } from "react-icons/fi";
import { BsTrashFill } from "react-icons/bs";
import { IoIosSave } from "react-icons/io";
import { AiOutlineClose } from "react-icons/ai";
import { DeleteModal } from "./DeleteModal";

export const Machines = () => {
	const { machines, tools } = useAppSelector((state) => state.data);
	const dispatch = useAppDispatch();

	const [name, setName] = useState("");
	const [count, setCount] = useState(0);
	const [allowedToolIds, setAllowedToolIds] = useState<number[]>([]);
	const [changeoverTimes, setChangeoverTimes] = useState<number[][]>([]);

	const [editingId, setEditingId] = useState<number | false>(false);
	const [deletingId, setDeletingId] = useState<number | false>(false);

	return (
		<div className="content">
			<DeleteModal
				show={!!deletingId}
				text="Möchten Sie diese Maschine wirklich löschen? Dies kann nicht rückgängig gemacht werden. Alle Arbeitsgänge, die diese Maschine verwenden, werden ebenfalls gelöscht."
				onClose={() => setDeletingId(false)}
				onOk={() => {
					dispatch(deleteMachine(deletingId as number));
				}}
			/>

			<div className="heading">Maschinen</div>
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
					placeholder="Anzahl"
					value={count}
					min={0}
					onChange={(e) => setCount(parseInt(e.target.value))}
					autoComplete="off"
				/>
				<br />
				<br />
				Erlaubte Werkzeuge:
				<br />
				{tools.map((tool) => (
					<div key={tool.typeId}>
						<input
							type="checkbox"
							checked={allowedToolIds.includes(tool.typeId)}
							onChange={(e) => {
								if (e.target.checked) {
									setAllowedToolIds([...allowedToolIds, tool.typeId].sort());

									const newChangeoverTimes = [...changeoverTimes.map((row) => [...row])];
									newChangeoverTimes.push([]);
									newChangeoverTimes.forEach((row) => {
										while (row.length < newChangeoverTimes.length) {
											row.push(0);
										}
									});
									setChangeoverTimes(newChangeoverTimes);
								} else {
									const toolIdx = allowedToolIds.findIndex(
										(toolId) => toolId === tool.typeId
									);
									if (toolIdx !== -1) {
										const newAllowedToolIds = [...allowedToolIds];
										newAllowedToolIds.splice(toolIdx, 1);
										setAllowedToolIds(newAllowedToolIds);

										const newChangeoverTimes = [
											...changeoverTimes.map((row) => [...row]),
										];
										newChangeoverTimes.splice(toolIdx, 1);
										newChangeoverTimes.forEach((row) => row.splice(toolIdx, 1));
										setChangeoverTimes(newChangeoverTimes);
									}
								}
							}}
						/>
						<label>{tool.name}</label>
					</div>
				))}
				<br />
				{allowedToolIds.length > 0 && (
					<>
						Rüstzeiten (in Minuten):
						<br />
						<table>
							<thead>
								<tr>
									<th></th>
									{allowedToolIds.map((a) => (
										<th key={a}>{tools.find((t) => t.typeId === a)?.name}</th>
									))}
								</tr>
							</thead>
							<tbody>
								{allowedToolIds.map((a, i) => (
									<tr key={a}>
										<th>{tools.find((t) => t.typeId === a)?.name}</th>
										{allowedToolIds.map((b, j) => (
											<td key={b}>
												<input
													type="number"
													step={0.1}
													value={changeoverTimes[i]?.[j] ?? 0}
													autoComplete="off"
													onChange={(e) => {
														const newChangeoverTimes = [...changeoverTimes];
														newChangeoverTimes[i] = [
															...(newChangeoverTimes[i] ?? []),
														];
														newChangeoverTimes[i][j] = parseFloat(e.target.value);
														setChangeoverTimes(newChangeoverTimes);
													}}
												/>
											</td>
										))}
									</tr>
								))}
							</tbody>
						</table>
						<br />
					</>
				)}
				{!editingId && (
					<Button
						type="button"
						variant="success"
						onClick={() => {
							dispatch(addMachine({ name, allowedToolIds, changeoverTimes, count }));
							setName("");
							setCount(0);
							setAllowedToolIds([]);
							setChangeoverTimes([]);
						}}
						disabled={
							!name ||
							machines.some((m) => m.name === name) ||
							count === 0 ||
							allowedToolIds.length === 0 ||
							changeoverTimes.length !== allowedToolIds.length ||
							changeoverTimes.some((a) => a.length !== allowedToolIds.length)
						}
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
								setCount(0);
								setAllowedToolIds([]);
								setChangeoverTimes([]);
								setEditingId(false);
							}}
						>
							<AiOutlineClose size={"1.35em"} /> Abbrechen
						</Button>
						<Button
							type="button"
							variant="success"
							onClick={() => {
								dispatch(
									editMachine({
										name,
										typeId: editingId,
										allowedToolIds,
										changeoverTimes,
										count,
									})
								);
								setName("");
								setCount(0);
								setAllowedToolIds([]);
								setChangeoverTimes([]);
								setEditingId(false);
							}}
							disabled={
								!name ||
								(machines.find((machine) => machine.name === name)?.typeId ?? editingId) !==
									editingId ||
								count === 0 ||
								allowedToolIds.length === 0 ||
								changeoverTimes.length !== allowedToolIds.length ||
								changeoverTimes.some((a) => a.length !== allowedToolIds.length)
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
						<th>Anzahl</th>
						<th>Erlaubte Werkzeuge</th>
						<th colSpan={2}></th>
					</tr>
				</thead>
				<tbody>
					{machines.map((machine) => (
						<tr key={machine.typeId}>
							<td>
								<b>{machine.name}</b>
							</td>
							<td>{machine.count}</td>
							<td>
								{machine.allowedToolIds.map((tId) => {
									const tool = tools.find((tool) => tool.typeId === tId);
									return (
										<>
											<span>{tool?.name}</span>
											<br />
										</>
									);
								})}
							</td>
							<td>
								<Button
									type="button"
									variant="secondary"
									onClick={() => {
										setName(machine.name);
										setCount(machine.count);
										setAllowedToolIds(machine.allowedToolIds);
										setChangeoverTimes(machine.changeoverTimes);
										setEditingId(machine.typeId);
									}}
								>
									<FiEdit size={"1.2em"} /> Bearbeiten
								</Button>
							</td>
							<td>
								<Button
									type="button"
									variant="danger"
									onClick={() => setDeletingId(machine.typeId)}
								>
									<BsTrashFill size={"1.2em"} /> Löschen
								</Button>
							</td>
						</tr>
					))}
				</tbody>
			</Table>
		</div>
	);
};
