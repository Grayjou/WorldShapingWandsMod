# `Localization/_chunks/` — browser-agent translation workspace

This directory is the round-trip workspace for the browser-agent localization
pipeline (W-S9-1 / W-S2-2 / W-S3-2). It is **transient**: nothing here is
load-bearing once a chunk has been imported.

Per Cavendish design doc
`_session_outputs/2026-04-23_Session_9/DesignDoc_LocalizationPipelineWithBrowserAgents.md`
with S3 defaults updates from
`_session_outputs/2026-04-24_Session_3/Notes_LocalizationDefaultsUpdate.md`.

---

## Directory layout

```
Localization/_chunks/
  chunk_<locale>_<NNN>.prompt.txt    ← written by chunk_export.py;
                                       paste into a browser-agent tab
  chunk_<locale>_<NNN>.response.txt  ← YOU paste the agent's reply here
  _done/
    chunk_<locale>_<NNN>.prompt.txt    ← archived by chunk_import.py
    chunk_<locale>_<NNN>.response.txt  ← archived by chunk_import.py
```

The naming convention is strict because the importer matches by stem.
**Do not rename files.** If you need to redo a chunk, just delete the existing
`*.response.txt` and paste a fresh response with the same name.

---

## Active defaults (S3 2026-04-24)

| Parameter | Value | Source |
|-----------|-------|--------|
| Chunk size | **40** rows | S3 — was 20 in S9, bumped per GrayJou: *"LLMs can handle 40."* |
| Sentinels | `###CHUNK-START###` / `###CHUNK-END###` | S9 |
| Reference rows | 3 in-locale tone samples per chunk | S9 |
| Spanish workflow | `--references en-US` (one pass) | S9 |
| French workflow | `--references en-US,es-ES` (**two passes**) | S3 — Pass 2 reads completed Spanish as cognate signal |
| Other Romance targets (it-IT, pt-BR) | `--references en-US,es-ES` once Spanish exists | Generalised in `Notes_LocalizationDefaultsUpdate.md` §2 |
| Germanic targets (de-DE, nl, sv) | `--references en-US` (or `en-US,de-DE` for nl/sv) | Generalised |
| Slavic / CJK / Arabic | `--references en-US` only | Generalised |

---

## The happy path — one ES translation pass

```powershell
cd "$env:USERPROFILE\Documents\My Games\Terraria\tModLoader\ModSources\WorldShapingWandsMod"

# 1. Make sure pending TSV is fresh (no-op if you already exported recently).
python Scripts/Localization/locale_spreadsheet.py --export-pending --locale es-ES

# 2. Slice into chunks of 40.
python Scripts/Localization/chunk_export.py --locale es-ES

# 3. For each chunk in Localization/_chunks/chunk_es-ES_<NNN>.prompt.txt:
#      a. Open a browser-agent tab.
#      b. Paste the entire prompt file. Send.
#      c. Copy the agent's reply.
#      d. Paste it into a NEW file
#         Localization/_chunks/chunk_es-ES_<NNN>.response.txt
#         (same stem as the prompt; the importer matches by stem).
#    Run 4-6 tabs in parallel — that's the whole reason for the chunking.

# 4. As responses pile up, import them in batch.
python Scripts/Localization/chunk_import.py --locale es-ES --all

# 5. Once all chunks are imported, fold the populated pending TSV into the
#    master hjson via the existing script.
python Scripts/Localization/locale_spreadsheet.py --import
```

## The two-pass French workflow (S3 default)

After Spanish is COMPLETE — i.e. `_pending_es-ES.tsv` is fully translated AND
folded into `es-ES_Mods.WorldShapingWandsMod.hjson` via `--import`:

```powershell
# Pass 1 already happened (see ES happy path above). Now Pass 2:
python Scripts/Localization/locale_spreadsheet.py --export-pending --locale fr-FR
python Scripts/Localization/chunk_export.py --locale fr-FR --references en-US,es-ES
# ... paste / response loop as before ...
python Scripts/Localization/chunk_import.py --locale fr-FR --all
python Scripts/Localization/locale_spreadsheet.py --import
```

The fr-FR chunks will include a `es-ES` reference column with the completed
Spanish translation as a Romance-sister cognate signal. The agent treats it
as auxiliary context, not as the source of truth — `en-US` remains primary.

---

### §3.5 Review-before-import (the QA gate)

> Inserted in S4 2026-04-24 per Cavendish
> `_session_outputs/2026-04-24_Session_4/DesignDoc_LocalizationWorkflowPolish.md`
> §1.1, closing GrayJou's S4 verbatim concern *"how I edit them responses
> before moving them to the resposes."*

**Don't trust the agent.** Every chunk gets reviewed before merge.

1. After saving `chunk_<locale>_<NNN>.response.txt` (the empty stub is
   auto-created alongside the prompt by `chunk_export.py` — no need to
   create or name it yourself), run:
   ```powershell
   python Scripts/Localization/chunk_import.py --locale <locale> --chunk <NNN> --dry-run
   ```
   Output is a per-row merge preview: `KEY | en-US (source) | <locale> (proposed)`
   for every row that would land. Drops + warnings printed too.

2. **Where to edit**:
   - **For typos / single-word fixes**: edit the `.response.txt` directly.
     It's just a TSV after the sentinels — find the row, fix the cell,
     save.
   - **For wholesale rewrites of one row**: same; edit the `.response.txt`.
   - **For "I want to defer this row to the next pass"**: delete the row
     from `.response.txt`. The pending TSV will keep the row marked as
     un-translated; the next chunk export picks it up again.
   - **For style decisions affecting many rows**: amend the agent's prompt
     context once (open `chunk_<locale>_<NNN>.prompt.txt`, edit the
     locale-preamble hint or the in-locale tone-reference rows), then
     re-paste to a fresh agent tab.

3. Re-run without `--dry-run`:
   ```powershell
   python Scripts/Localization/chunk_import.py --locale <locale> --chunk <NNN>
   ```
   Merges into `_pending_<locale>.tsv`. Files move to `_chunks/_done/`.

4. Periodically (after every few chunks or at end of session):
   ```powershell
   python Scripts/Localization/locale_spreadsheet.py --import
   ```
   Folds the now-populated pending TSV into master hjson.

**Rule of thumb**: editing happens in the `.response.txt` file BEFORE the
import command, OR in `_pending_<locale>.tsv` AFTER the import command.
Editing the master hjson directly is fine for one-off polish but skips the
audit trail — prefer the TSV.

---

## Quality control

Before importing a chunk, glance at its `.response.txt`:

- Does the row count match the prompt? (Headers + sentinels counted out.)
- Are any cells obviously wrong? (English left in, gibberish, off-topic.)
- Does the tone match the reference rows in the prompt?

If anything's off, **delete the response file** and re-prompt the same chunk.
Cost: ~30 seconds.

After import, eyeball ~5 random rows in `_pending_<locale>.tsv` for sanity.
The pending TSV is the last gate before master-hjson commit; corruption
caught here costs nothing.

---

## Edge cases (importer is defensive)

| Case | Behaviour |
|------|-----------|
| Browser agent adds preamble/suffix | Sentinels strip them. |
| Browser agent reorders rows | Importer joins by Key column, ignores order. |
| Browser agent drops a row | Row stays blank in TSV; gets re-exported next pass. |
| Browser agent adds extra rows | Importer drops them with WARNING log. |
| Markdown code fences inside the sentinel block | Stripped. |
| Re-pasted response file (you redid the chunk) | Latest mtime wins. |
| Wrong response pasted into wrong file | Key mismatch → all rows drop with WARNINGs → 0 rows merged → re-paste correctly. |
| Translation cell already filled | Skipped (no clobber). |

---

## Cleanup

The `_done/` archive is for audit and to anchor `chunk_export.py` idempotency
(re-running export after import does NOT regenerate already-imported chunks).
Once you've shipped a locale to master and confirmed `--list-pending` is at
zero, you can delete `_done/<locale>_*` if you want — it's purely historical.

The `chunk_*.{prompt,response}.txt` files (top-level, not in `_done/`) are
never committed; they are scratch. Add them to your local `.gitignore` if
they aren't already filtered.
