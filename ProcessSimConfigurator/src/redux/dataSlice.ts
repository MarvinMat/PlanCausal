import { createSlice, PayloadAction } from "@reduxjs/toolkit";
import { Machine } from "../types/Machine";
import { Tool } from "../types/Tool";
import { WorkOperation, WorkPlan } from "../types/WorkPlan";

type DataState = {
	machines: Machine[];
	tools: Tool[];
	workPlans: WorkPlan[];
};

const initialState: DataState = {
	machines: [],
	tools: [],
	workPlans: [],
};

const dataSlice = createSlice({
	name: "data",
	initialState,
	reducers: {
		addMachine: (state, action: PayloadAction<Omit<Machine, "typeId">>) => {
			const maxTypeId = state.machines.reduce(
				(max, machine) => (machine.typeId > max ? machine.typeId : max),
				0
			);
			const typeId = maxTypeId + 1;
			state.machines.push({ ...action.payload, typeId });
		},
		editMachine: (state, action: PayloadAction<Machine>) => {
			const machine = state.machines.find((machine) => machine.typeId === action.payload.typeId);
			if (machine) {
				machine.name = action.payload.name;
				machine.count = action.payload.count;
				machine.allowedToolIds = action.payload.allowedToolIds;
				machine.changeoverTimes = action.payload.changeoverTimes;
			}
		},
		deleteMachine: (state, action: PayloadAction<number>) => {
			state.machines = state.machines.filter((machine) => machine.typeId !== action.payload);

			state.workPlans.forEach((workPlan) => {
				workPlan.operations = workPlan.operations.filter(
					(operation) => operation.machineId !== action.payload
				);
			});
		},
		addTool: (state, action: PayloadAction<Omit<Tool, "typeId">>) => {
			const maxTypeId = state.tools.reduce((max, tool) => (tool.typeId > max ? tool.typeId : max), 0);
			const typeId = maxTypeId + 1;

			state.tools.push({ ...action.payload, typeId });
		},
		editTool: (state, action: PayloadAction<Tool>) => {
			const tool = state.tools.find((tool) => tool.typeId === action.payload.typeId);
			if (tool) {
				tool.name = action.payload.name;
				tool.description = action.payload.description;
			}
		},
		deleteTool: (state, action: PayloadAction<number>) => {
			state.tools = state.tools.filter((tool) => tool.typeId !== action.payload);

			// remove this tool from all machines: remove it from allowedToolIds and remove the corresponding changeoverTimes (column and row of that tool in the matrix)
			state.machines.forEach((machine) => {
				const toolIdx = machine.allowedToolIds.findIndex((toolId) => toolId === action.payload);
				if (toolIdx !== -1) {
					machine.allowedToolIds.splice(toolIdx, 1);
					machine.changeoverTimes.splice(toolIdx, 1);
					machine.changeoverTimes.forEach((row) => row.splice(toolIdx, 1));
				}
			});

			// delete work operations that use this tool
			state.workPlans.forEach((workPlan) => {
				workPlan.operations = workPlan.operations.filter(
					(operation) => operation.toolId !== action.payload
				);
			});

			// if a machine has no allowedToolIds left, delete it
			const machinesWithoutTools = state.machines.filter((machine) => machine.allowedToolIds.length === 0);
			state.machines = state.machines.filter((machine) => machine.allowedToolIds.length > 0);

			// delete work operations that use any of these machines
			state.workPlans.forEach((workPlan) => {
				workPlan.operations = workPlan.operations.filter(
					(operation) => !machinesWithoutTools.some((machine) => machine.typeId === operation.machineId)
				);
			});

		},
		addWorkPlan: (state, action: PayloadAction<Omit<WorkPlan, "workPlanId">>) => {
			const maxWorkPlanId = state.workPlans.reduce(
				(max, workPlan) => (workPlan.workPlanId > max ? workPlan.workPlanId : max),
				0
			);
			const workPlanId = maxWorkPlanId + 1;
			state.workPlans.push({ ...action.payload, workPlanId });
		},
		editWorkPlan: (state, action: PayloadAction<Omit<WorkPlan, "operations">>) => {
			const workPlan = state.workPlans.find(
				(workPlan) => workPlan.workPlanId === action.payload.workPlanId
			);
			if (workPlan) {
				workPlan.name = action.payload.name;
				workPlan.description = action.payload.description;
			}
		},
		deleteWorkPlan: (state, action: PayloadAction<number>) => {
			state.workPlans = state.workPlans.filter((workPlan) => workPlan.workPlanId !== action.payload);
		},
		addOperationToWorkPlan: (
			state,
			action: PayloadAction<{ workPlanId: number; operation: WorkOperation }>
		) => {
			const workPlan = state.workPlans.find(
				(workPlan) => workPlan.workPlanId === action.payload.workPlanId
			);
			if (workPlan) {
				workPlan.operations.push(action.payload.operation);
			}
		},
		editOperationInWorkPlan: (
			state,
			action: PayloadAction<{ workPlanId: number; operationIdx: number; operation: WorkOperation }>
		) => {
			const workPlan = state.workPlans.find(
				(workPlan) => workPlan.workPlanId === action.payload.workPlanId
			);
			if (workPlan) {
				workPlan.operations[action.payload.operationIdx] = action.payload.operation;
			}
		},
		deleteOperationFromWorkPlan: (
			state,
			action: PayloadAction<{ workPlanId: number; operationIdx: number }>
		) => {
			const workPlan = state.workPlans.find(
				(workPlan) => workPlan.workPlanId === action.payload.workPlanId
			);
			if (workPlan) {
				workPlan.operations.splice(action.payload.operationIdx, 1);
			}
		},
		swapOperationUpInWorkPlan: (
			state,
			action: PayloadAction<{ workPlanId: number; operationIdx: number }>
		) => {
			const workPlan = state.workPlans.find(
				(workPlan) => workPlan.workPlanId === action.payload.workPlanId
			);
			if (
				workPlan &&
				action.payload.operationIdx > 0 &&
				action.payload.operationIdx < workPlan.operations.length
			) {
				const operation = workPlan.operations[action.payload.operationIdx];
				workPlan.operations[action.payload.operationIdx] =
					workPlan.operations[action.payload.operationIdx - 1];
				workPlan.operations[action.payload.operationIdx - 1] = operation;
			}
		},
		swapOperationDownInWorkPlan: (
			state,
			action: PayloadAction<{ workPlanId: number; operationIdx: number }>
		) => {
			const workPlan = state.workPlans.find(
				(workPlan) => workPlan.workPlanId === action.payload.workPlanId
			);
			if (
				workPlan &&
				action.payload.operationIdx >= 0 &&
				action.payload.operationIdx < workPlan.operations.length - 1
			) {
				const operation = workPlan.operations[action.payload.operationIdx];
				workPlan.operations[action.payload.operationIdx] =
					workPlan.operations[action.payload.operationIdx + 1];
				workPlan.operations[action.payload.operationIdx + 1] = operation;
			}
		},
		setTools: (state, action: PayloadAction<Tool[]>) => {
			state.tools = action.payload;
		},
		setMachines: (state, action: PayloadAction<Machine[]>) => {
			state.machines = action.payload;
		},
		setWorkPlans: (state, action: PayloadAction<WorkPlan[]>) => {
			state.workPlans = action.payload;
		},
	},
});

export const {
	addMachine,
	addTool,
	addWorkPlan,
	deleteMachine,
	deleteTool,
	deleteWorkPlan,
	editTool,
	editMachine,
	editWorkPlan,
	addOperationToWorkPlan,
	editOperationInWorkPlan,
	deleteOperationFromWorkPlan,
	swapOperationUpInWorkPlan,
	swapOperationDownInWorkPlan,
	setTools,
	setMachines,
	setWorkPlans,
} = dataSlice.actions;

export default dataSlice.reducer;
