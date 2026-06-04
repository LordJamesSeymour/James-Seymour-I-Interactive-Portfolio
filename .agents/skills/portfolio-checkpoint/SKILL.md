---
name: portfolio-checkpoint
description: Create a connected Obsidian checkpoint for the Unity Web Portfolio project. Use when I type /portfolio-checkpoint after project changes.
argument-hint: "[short-topic]"
disable-model-invocation: true
---

Create a connected Obsidian checkpoint for the current Unity Web Portfolio project state.

Do not edit Unity source code unless the user separately asks for code changes.

Short topic argument:
$ARGUMENTS

If $ARGUMENTS is empty, choose a short filename-safe topic based on the current session.

Obsidian vault:
D:\ObsidianVaults\Unity\WebPortfolio

Use this command from the vault root:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File ".\00_System\Tools\New-PortfolioCheckpoint.ps1" "$ARGUMENTS"
```

Equivalent vault-root batch wrapper:

```bat
new-checkpoint "$ARGUMENTS"
```

The script writes checkpoints inside:
D:\ObsidianVaults\Unity\WebPortfolio\02_Checkpoints

The note must link to:

- [[00_Checkpoints_Index]]
- [[01_Current_Project_State]]
- [[02_AI_Agent_Operating_Rules]]
- [[00_Project_Overview]]
- [[00_Project_Rules]]
- [[01_Unity_Rules]]
- [[00_Architecture_Overview]]
- [[00_GitHub_Pages_Deployment]]
- [[01_Unity_Web_Template]]

Before creating the checkpoint:

- Confirm the correct vault path is `D:\ObsidianVaults\Unity\WebPortfolio`.
- Do not use or modify `D:\ObsidianVaults\C++\LLGP\LowLevel-Y3`.
- Do not delete or rename notes.

After creating the checkpoint:

- Report the created checkpoint path.
- If the checkpoint index should list this new checkpoint, add a link to `02_Checkpoints\00_Checkpoints_Index.md`.
- Preserve existing note content.

Final response format:

Done.
Changed:
- checkpoint note path
- checkpoint index note path, if updated

Test:
- Manual test needed: open Obsidian Graph View and confirm the checkpoint node connects to `00_Checkpoints_Index`.
