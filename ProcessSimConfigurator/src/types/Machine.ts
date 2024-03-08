import * as z from "zod";

export const machineSchema = z.object({
	typeId: z.number(),
	count: z.number(),
	name: z.string(),
	allowedToolIds: z.array(z.number()),
	changeoverTimes: z.array(z.array(z.number())),
});

export type Machine = z.infer<typeof machineSchema>;
