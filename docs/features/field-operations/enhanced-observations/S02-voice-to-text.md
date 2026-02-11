# Story: Voice-to-Text Observation Input

## S02-voice-to-text

**As an** Evaluator in the field,
**I want** to dictate my observation using voice input,
**So that** I can capture detailed notes hands-free while keeping my eyes on the exercise activity.

### Context

An Evaluator watching a search-and-rescue team clear a collapsed structure needs both hands free (holding a clipboard, wearing gloves, or positioned where typing is impractical) and their eyes on the action. Typing a detailed observation on a tablet takes 30-60 seconds and requires looking at the screen. Voice dictation lets them speak naturally — "Rescue team entered structure at 10:42, primary search of first floor completed in 8 minutes, secondary search not initiated before moving to second floor" — while continuing to observe. The browser's built-in SpeechRecognition API works offline on most modern devices (Chrome, Edge, Safari) because the speech model runs on-device, making this viable even in low-connectivity field environments.

### Acceptance Criteria

- [ ] **Given** I am creating or editing an observation, **when** I tap the microphone icon in the text field, **then** voice recognition activates and a visual indicator shows that the system is listening
- [ ] **Given** voice recognition is active, **when** I speak, **then** my words appear in the observation text field in real-time (interim results visible, replaced by final transcription)
- [ ] **Given** voice recognition is active, **when** I pause speaking for 3+ seconds, **then** recognition stops automatically and the transcribed text remains in the field for review
- [ ] **Given** voice recognition is active, **when** I tap the microphone icon again, **then** recognition stops and the transcribed text remains in the field
- [ ] **Given** voice recognition has produced text, **when** I review the transcription, **then** I can edit it manually using the keyboard before saving
- [ ] **Given** voice recognition has produced text and I want to add more, **when** I tap the microphone icon again, **then** new speech is appended to the existing text (not replacing it)
- [ ] **Given** my device or browser does not support the SpeechRecognition API, **when** I view the observation form, **then** the microphone icon is not displayed (graceful degradation, no error)
- [ ] **Given** I have not granted microphone permissions, **when** I tap the microphone icon, **then** I see a clear prompt explaining why microphone access is needed
- [ ] **Given** voice recognition is active, **when** I am in a noisy environment and recognition quality is poor, **then** the interim text is still visible so I can decide whether to keep or discard it
- [ ] **Given** voice recognition is active, **when** I receive a poor transcription, **then** I can tap a "Clear" button to remove the transcribed text and try again

### Out of Scope

- Cloud-based speech recognition (e.g., Azure Speech Services) — use browser-native API only for offline capability and zero cost
- Voice commands (e.g., "save observation", "rate as performed") — voice is for text input only
- Language selection or multilingual support (use device default language)
- Audio recording storage (only the transcribed text is saved, not the audio)
- Speaker identification or voice profiles
- Punctuation commands ("period", "comma") — users edit text manually if needed

### Dependencies

- S01-quick-add-observation (text field where voice input is entered)
- Browser SpeechRecognition API (or webkitSpeechRecognition)

### Open Questions

- [ ] Should voice-to-text be available in the full observation form as well as Quick-Add? (Recommendation: yes — same microphone icon pattern in any text field)
- [ ] Should the system attempt automatic punctuation? (Recommendation: rely on browser API defaults — some browsers add punctuation automatically, some don't. Don't build custom punctuation logic.)
- [ ] Is there value in a "voice memo" mode that records audio as a fallback when transcription quality is poor? (Recommendation: defer — adds complexity and storage requirements. Text-only for now.)

### Domain Terms

| Term | Definition |
|------|------------|
| Voice-to-Text | Browser-native speech recognition that converts spoken words into text in the observation field, enabling hands-free capture |
| Interim Results | Partially-recognized text displayed in real-time as the user speaks, which may change as the recognition engine refines its interpretation |
| Final Transcription | The completed, stable text output from speech recognition after the user stops speaking |

### UI/UX Notes

```
Voice input states:

Idle (microphone available):
┌─────────────────────────────────┐
│ What did you observe?       🎤  │
│                                 │
│                                 │
└─────────────────────────────────┘

Listening (voice active):
┌─────────────────────────────────┐
│ What did you observe?       🔴  │
│                                 │
│ Rescue team entered structure   │
│ at ten forty two_               │ ← interim text, cursor blinking
└─────────────────────────────────┘
│ 🔴 Listening... [Stop]          │

Transcription complete (editable):
┌─────────────────────────────────┐
│ What did you observe?       🎤  │
│                                 │
│ Rescue team entered structure   │
│ at 10:42, primary search of     │
│ first floor completed in 8      │
│ minutes                         │
└─────────────────────────────────┘
│ [🎤 Add More] [✕ Clear]         │
```

- Microphone icon changes to red/pulsing when actively listening
- "Listening..." indicator with an audio waveform animation gives confidence that the system is working
- Interim text displayed in a lighter color or italic to indicate it may change
- Final text displayed in normal style, fully editable
- "Add More" button to append additional voice input to existing text
- "Clear" button to remove all transcribed text and start fresh
- Keep the listening indicator visible even if the user scrolls — sticky at bottom of text area

### Technical Notes

- Use the Web Speech API: `new (window.SpeechRecognition || window.webkitSpeechRecognition)()`
- Configuration: `recognition.continuous = true; recognition.interimResults = true; recognition.lang = navigator.language`
- Feature detection: `if ('SpeechRecognition' in window || 'webkitSpeechRecognition' in window)` — hide mic icon if unsupported
- Handle `onresult` event: interim results (`event.results[i].isFinal === false`) shown in lighter style, final results committed to text field
- Handle `onerror` events gracefully: `not-allowed` (permissions), `no-speech` (silence timeout), `network` (some browsers need connectivity)
- Auto-stop after configurable silence timeout (3 seconds default)
- Note: Chrome on Android typically does on-device recognition. Chrome on desktop may use cloud — still works but requires connectivity. Safari uses on-device Siri recognition.
- Test across target browsers: Chrome (Android tablet), Safari (iPad), Edge
