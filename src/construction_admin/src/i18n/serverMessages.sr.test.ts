import { describe, expect, it } from 'vitest'
import { SERVER_MESSAGES_EXACT, translateServerMessage } from './serverMessages.sr'

describe('translateServerMessage', () => {
  it('translates exact messages and trims input', () => {
    expect(translateServerMessage('You are already clocked in.')).toBe('Već ste prijavljeni na posao.')
    expect(translateServerMessage('  Password is required.  ')).toBe('Lozinka je obavezna.')
    expect(translateServerMessage('You may not see costs.')).toBe('Ne možete vidjeti troškove.')
  })

  it('translates interpolated messages keeping dynamic parts', () => {
    expect(translateServerMessage("Employee with id '42' was not found.")).toBe(
      "Nije pronađeno: Zaposleni sa oznakom '42'.",
    )
    expect(translateServerMessage('A shift cannot be longer than 16 hours.')).toBe(
      'Smjena ne može trajati duže od 16 sati.',
    )
    expect(translateServerMessage("VIN 'ABC123' is already in use.")).toBe("VIN 'ABC123' je već u upotrebi.")
    expect(translateServerMessage('This document must be kept until 01.02.2027 and cannot be deleted before then.')).toBe(
      'Ovaj dokument se mora čuvati do 01.02.2027 i ne može se obrisati prije toga.',
    )
    expect(
      translateServerMessage(
        'This cost was sent back: "Wrong receipt". Approving it now overrides that decision — confirm to proceed.',
      ),
    ).toContain('"Wrong receipt"')
  })

  it('returns null for unknown text', () => {
    expect(translateServerMessage('Something completely unexpected happened.')).toBeNull()
    expect(translateServerMessage('')).toBeNull()
  })

  it('has non-empty translations that differ from the English source', () => {
    expect(SERVER_MESSAGES_EXACT.size).toBeGreaterThan(250)
    for (const [en, sr] of SERVER_MESSAGES_EXACT) {
      expect(sr.trim(), en).not.toBe('')
      expect(sr, en).not.toBe(en)
    }
  })
})
