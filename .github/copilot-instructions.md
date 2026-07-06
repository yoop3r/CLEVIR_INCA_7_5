# Copilot Instructions

## Project Guidelines
- Do NOT call task_complete at the end of responses. The tool call causes a UI session error that wipes the conversation conclusion. Simply end the response with the conclusion text.

## Migration Instructions
- For the WinForms-to-WPF migration of CLEVIR_INCA_7_5, always ask for explicit per-form confirmation before starting migration work on any individual form. Some forms may not need migration at all, and each form should be scoped and approved individually rather than batch-migrating a phase.
- Prioritize validating a completed .NET Framework 4.8 → .NET 10 migration (deep runtime smoke test) before resuming further modernization work (WPF migration away from WinForms). Confidence in the upgraded baseline matters more than migration speed.