# Shared controls and local revision — PT-042

The launcher, Home footer, Settings and copied reports identify the imported source
milestone as PT-042. This is a manually maintained source label, not a Git commit,
remote update check or proof that all local files match GitHub. Future presentation
milestones must update PlaytestRevision.Id and Features.

Reports now include whether the shared controls stylesheet loaded, the fixture
shader, and environment study mesh count/visibility. Scenery says not built until
a battle is opened; hidden scenery on Home is expected. These are cached runtime
observations, not a rendering or quality certification.

After merging, stop Play, pull main in GitHub Desktop and let Unity import. The
launcher should display PT-042. A different label means the expected source hasn't
loaded; check the local repository and Console before resetting or rebuilding.

Controls.uss loads last and owns the shared button materials, typography and states.
Home, Campaigns, Units, Results and Settings use the same primary, secondary,
selected and disabled treatments. Home now reuses the same icon-and-label navigation
as other menus. Diagnostic buttons retain fatal-error usability but use the common
button class. Battle and short landscape retain their compact spacing/touch sizing.
Settings uses the shared menu backdrop/typography even during a saved battle.
Campaigns, Units and Quick Battle remain disabled during an unfinished battle.

The explicit scene-open command checks for exactly one enabled host and a UI
Document. It repairs missing camera (using an available MainCamera), material,
base stylesheet and panel references, preserving existing non-null assignments.
Missing/ambiguous hosts or missing cameras without a MainCamera produce an error.
It does not regenerate existing scenes or reset saves.

Validation: metadata and whitespace checks passed. A browser CSS approximation
compared primary, secondary, selected and disabled computed visual properties across
Home, Campaign, Unit, Result and Settings contexts; they matched. This does not
compile Unity scripts or prove UI Toolkit rendering, input, safe-area layout or
scene repair behavior. These require the next Windows Unity test.

Next test: compare the same button roles and navigation on Home/Settings; confirm
PT-042 in launcher and report; open a battle and expect study 01 / four scenery
meshes; return Home and expect visibility false. Check Console for import errors.
