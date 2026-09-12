import type { MessageKey } from './en';
import type { TranslateValues } from './context';
import { liveT } from './liveT';

/**
 * A Zod `error` callback that resolves the given key at validation time,
 * not at schema-definition time — schemas in this app are built once as
 * module constants, so a plain string message would freeze in whatever
 * language was active when the module first loaded. Zod calls `error`
 * lazily on each failed `parse()`, which is what makes this pick up a
 * language switch made after the schema was built.
 */
export function zodMsg(key: MessageKey, values?: TranslateValues): () => string {
  return () => liveT(key, values);
}
