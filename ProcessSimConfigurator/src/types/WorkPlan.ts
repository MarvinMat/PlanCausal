import * as z from "zod";

export const workOperationSchema = z.object({
	machineId: z.number(),
	duration: z.number(),
	name: z.string(),
	toolId: z.number(),
});

export const workPlanSchema = z.object({
	workPlanId: z.number(),
	name: z.string(),
	description: z.string(),
	operations: z.array(workOperationSchema),
});

export type WorkPlan = z.infer<typeof workPlanSchema>;
export type WorkOperation = z.infer<typeof workOperationSchema>;
