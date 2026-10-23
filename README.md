# Prompt Studio

A standalone Windows desktop app for reusable prompt templates. No account, API key, or internet connection is required.

Run `PromptStudio.exe` (or `Launch.cmd`). To rebuild from source, run `powershell -ExecutionPolicy Bypass -File build.ps1` on Windows with .NET Framework 4.x.

## How it works

1. Write a template using placeholders such as `{{topic}}`, `{{audience}}`, and `{{source_text}}`.
2. Fill the automatically generated input fields. Each field supports pasted, multiline text.
3. Review the live final prompt and copy it, or export it to a text file.

Use **+ Add variable** to insert a placeholder at the template cursor. Variable names accept letters, numbers, underscores, spaces, and hyphens. Repeated placeholders share the same input. Empty inputs remain visible as placeholders in the output.

Use **Clean all** above the inputs to enable all six edge-cleanup options for every variable in the current template in one click. **Turn off all** disables them. For a custom combination, expand **Choose specific characters**, select your options, and click **Apply to all variables**. These actions replace existing per-variable choices and set the default for newly added variables. Other templates are unaffected.

Expand **Customize this variable** below an input for individual overrides. Cleanup trims periods, commas, semicolons, colons, double quotes (straight or curly), or whitespace from its beginning and end. Enable whitespace too when punctuation is surrounded by spaces. Internal punctuation and spacing, your original input, and punctuation written in the template are preserved. Choices save with the template and are included when duplicating it. If cleanup removes an entire nonempty value, it contributes empty text to the output.

Templates, inputs, theme, and the current selection save automatically to `%LOCALAPPDATA%\PromptStudio\workspace.json`. **New**, **Duplicate**, and **Delete** manage your library. Deletion asks for confirmation. Example templates are provided on first launch.

Keyboard shortcuts: Ctrl+Shift+C copies the final prompt; Ctrl+N creates a template. Text editors support standard undo, redo, copy, paste, and select-all shortcuts.

This app assembles text locally; it does not send prompts to an AI service. Saved inputs are plain text on your computer.
