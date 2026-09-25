# Verification plan

No gameplay implementation exists yet. Add meaningful Core tests alongside state
and command code: action budgets, initiative, objective transitions, and reward
idempotency. Add Unity integration checks for asset imports, scene references and
save/resume when those systems exist. Device testing remains separate.

BuildTools/validate_repository.py checks repository integrity only. It does not
compile C#, resolve Unity packages or prove rendering/build compatibility.
