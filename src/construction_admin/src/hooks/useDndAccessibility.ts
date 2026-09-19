import { useT } from '../i18n/useI18n';

/**
 * The spoken and written hints drag-and-drop gives, in the panel's language.
 * The library ships English only, which showed up as untranslated text on the
 * dashboard and assignment board.
 */
export function useDndAccessibility() {
  const t = useT();

  return {
    screenReaderInstructions: { draggable: t('dnd.instructions') },
    announcements: {
      onDragStart: () => t('dnd.pickedUp'),
      onDragOver: () => t('dnd.movedOver'),
      onDragEnd: () => t('dnd.dropped'),
      onDragCancel: () => t('dnd.cancelled'),
    },
  };
}
