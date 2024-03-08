import React from "react";
import { Button, Modal } from "react-bootstrap";

type DeleteModalProps = {
	show: boolean;
	onOk: () => void;
	onClose: () => void;
	text: string;
};

export const DeleteModal: React.FC<DeleteModalProps> = ({ onClose, onOk, show, text }) => {
	return (
		<Modal show={show}>
			<Modal.Header>
				<Modal.Title>Sind Sie sicher?</Modal.Title>
			</Modal.Header>
			<Modal.Body>{text}</Modal.Body>
			<Modal.Footer>
				<Button
					variant="danger"
					onClick={() => {
						onOk();
						onClose();
					}}
				>
					Löschen
				</Button>
				<Button variant="secondary" onClick={onClose}>
					Abbrechen
				</Button>
			</Modal.Footer>
		</Modal>
	);
};
