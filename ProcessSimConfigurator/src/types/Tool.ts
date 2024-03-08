import * as z from "zod";

export const toolSchema = z.object({
	typeId: z.number(),
	name: z.string(),
	description: z.string(),
});

export type Tool = z.infer<typeof toolSchema>;
