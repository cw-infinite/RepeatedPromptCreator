# Prompt Studio

A standalone Windows desktop app for reusable prompt templates. No account, API key, or internet connection is required.

Run `PromptStudio.exe` (or `Launch.cmd`). To rebuild from source, run `powershell -ExecutionPolicy Bypass -File build.ps1` on Windows with .NET Framework 4.x.

## How it works

1. Write a template using placeholders such as `{{topic}}`, `{{audience}}`, and `{{source_text}}`.
2. Fill the automatically generated input fields. Each field supports pasted, multiline text.
3. Review the live final prompt and copy it, or export it to a text file.

Use **+ Add variable** to insert a placeholder at the template cursor. Variable names accept letters, numbers, underscores, spaces, and hyphens. Repeated placeholders share the same input. Empty inputs remain visible as placeholders in the output.

Expand **Clean up edges** below an input to trim periods, commas, semicolons, colons, double quotes (straight or curly), or whitespace from its beginning and end. Select each character type separately. Enable whitespace too when punctuation is surrounded by spaces. Internal punctuation and spacing, your original input, and punctuation written in the template are preserved. Choices are saved per variable and included when duplicating a template. If cleanup removes an entire nonempty value, it contributes empty text to the output.

Templates, inputs, theme, and the current selection save automatically to `%LOCALAPPDATA%\PromptStudio\workspace.json`. **New**, **Duplicate**, and **Delete** manage your library. Deletion asks for confirmation. Example templates are provided on first launch.

Keyboard shortcuts: Ctrl+Shift+C copies the final prompt; Ctrl+N creates a template. Text editors support standard undo, redo, copy, paste, and select-all shortcuts.

This app assembles text locally; it does not send prompts to an AI service. Saved inputs are plain text on your computer.
