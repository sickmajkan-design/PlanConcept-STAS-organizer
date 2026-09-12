import { z } from 'zod';

import { toolOwnershipTypes, toolStatuses } from '../../api/types';

/** Mirrors the API's ToolCommandBaseValidator so the form catches errors early. */
export const toolFormSchema = z.object({
  name: z.string().trim().min(1, 'Tool name is required.').max(256),
  category: z.string().trim().max(128).optional().or(z.literal('')),
  serialNumber: z.string().trim().max(128).optional().or(z.literal('')),
  qrCode: z.string().trim().max(256).optional().or(z.literal('')),
  status: z.enum(toolStatuses),
  ownershipType: z.enum(toolOwnershipTypes),
});

export type ToolFormValues = z.infer<typeof toolFormSchema>;
