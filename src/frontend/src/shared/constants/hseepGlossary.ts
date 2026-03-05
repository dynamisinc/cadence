/**
 * HSEEP Glossary and Contextual Help Content
 *
 * Centralized definitions for HSEEP terminology and feature-level
 * help text. Used by HelpTooltip and the HomePage role orientation.
 */

export interface GlossaryEntry {
  term: string
  definition: string
  details?: string
}

export interface ContextualHelp {
  /** Short one-liner shown in tooltip */
  summary: string
  /** Longer explanation shown in popover */
  details?: string
  /** HSEEP glossary term keys relevant to this context */
  relatedTerms?: string[]
  /** Role-specific tips keyed by exercise role name */
  roleTips?: Record<string, string>
}

export const HSEEP_GLOSSARY: Record<string, GlossaryEntry> = {
  msel: {
    term: 'MSEL',
    definition:
      'Master Scenario Events List \u2014 the ordered sequence of injects that drive exercise play.',
    details:
      'The MSEL defines what happens during an exercise and when. Controllers use it to deliver injects in the planned sequence, though the Exercise Director may adjust timing based on exercise flow.',
  },
  inject: {
    term: 'Inject',
    definition:
      'A single scenario event delivered to players during exercise conduct.',
    details:
      'Injects simulate real-world events that players must respond to. Each inject has a planned scenario time and is fired by a Controller when appropriate.',
  },
  fire: {
    term: 'Fire',
    definition:
      'To deliver an inject to players. Controllers fire injects at the appropriate time during exercise conduct.',
  },
  skip: {
    term: 'Skip',
    definition:
      'To intentionally pass over an inject without delivering it to players.',
  },
  defer: {
    term: 'Defer',
    definition: 'To postpone an inject for delivery later in the exercise.',
  },
  scenarioTime: {
    term: 'Scenario Time',
    definition:
      'The fictional time within the exercise story. May differ from real-world time.',
    details:
      'Scenario time allows exercises to simulate events across hours or days within a shorter real-world window. The exercise clock tracks scenario time independently from wall clock time.',
  },
  wallClock: {
    term: 'Wall Clock',
    definition:
      'The actual real-world time when an action occurs during exercise conduct.',
  },
  controller: {
    term: 'Controller',
    definition:
      'Manages exercise flow by firing injects and guiding the scenario narrative.',
  },
  evaluator: {
    term: 'Evaluator',
    definition:
      'Records observations and documents player performance against exercise objectives.',
  },
  exerciseDirector: {
    term: 'Exercise Director',
    definition:
      'Has overall authority over the exercise. Makes Go/No-Go decisions and manages all aspects of conduct.',
  },
  observer: {
    term: 'Observer',
    definition:
      'Watches exercise conduct without interfering. Has read-only access.',
  },
  observation: {
    term: 'Observation',
    definition:
      'A documented note about player performance recorded by an Evaluator during exercise conduct.',
  },
  eeg: {
    term: 'Exercise Evaluation Guide (EEG)',
    definition:
      'A structured evaluation tool that links observations to specific capabilities and critical tasks.',
  },
}

export const CONTEXTUAL_HELP: Record<string, ContextualHelp> = {
  'conduct.fire': {
    summary: 'Fire, skip, or defer injects to control exercise flow.',
    details:
      'Injects are delivered to players in sequence. Fire delivers the inject, Skip passes over it, and Defer postpones it for later.',
    relatedTerms: ['inject', 'fire', 'skip', 'defer'],
    roleTips: {
      Controller:
        'You fire injects when the time is right. Use Skip or Defer if the exercise flow requires adjustment.',
      ExerciseDirector:
        'You oversee inject delivery and can direct Controllers on timing adjustments.',
    },
  },
  'conduct.clock': {
    summary:
      'Dual time tracking: Scenario Time is the story time, Wall Clock is real time.',
    details:
      'The exercise clock tracks scenario time independently from real-world time. This allows exercises to simulate longer timeframes within a shorter conduct window.',
    relatedTerms: ['scenarioTime', 'wallClock'],
    roleTips: {
      ExerciseDirector:
        'You can start, pause, and reset the exercise clock to control scenario pacing.',
    },
  },
  'msel.overview': {
    summary:
      'The MSEL is your exercise script \u2014 all injects listed in sequence.',
    details:
      'View, add, edit, and reorder injects that make up the exercise scenario. Each inject has a planned scenario time and assigned Controller.',
    relatedTerms: ['msel', 'inject'],
    roleTips: {
      Controller: 'You can add, edit, and reorder injects in the MSEL.',
      ExerciseDirector:
        'You have full control over the MSEL structure and can approve changes.',
      Evaluator: 'You can view the MSEL to understand the planned scenario flow.',
      Observer: 'You can view the MSEL to follow along with the exercise.',
    },
  },
  'observations.overview': {
    summary: 'Document what you observe during exercise play.',
    details:
      'Observations capture how players respond to injects. They are linked to exercise objectives and help build the after-action report.',
    relatedTerms: ['observation', 'evaluator'],
    roleTips: {
      Evaluator:
        'Record observations as you watch players respond. Note strengths and areas for improvement.',
      ExerciseDirector:
        'Review observations in real-time to gauge exercise effectiveness.',
    },
  },
  'participants.roles': {
    summary: 'Assign HSEEP roles to team members for this exercise.',
    details:
      'Each participant is assigned a role that determines what they can do during exercise conduct. Roles follow the HSEEP standard.',
    relatedTerms: ['controller', 'evaluator', 'exerciseDirector', 'observer'],
    roleTips: {
      ExerciseDirector:
        'Add participants and assign their exercise roles here.',
    },
  },
  'assignments.overview': {
    summary: 'Your exercise role assignments across all exercises.',
    details:
      'View exercises you are assigned to, grouped by status. Click an exercise to enter its conduct view.',
  },
}
